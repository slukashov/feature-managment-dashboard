using FeatureManagement.Dashboard.Infrastructure.Cache;
using FeatureManagement.Dashboard.Infrastructure.Exceptions;
using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Infrastructure.Providers;
using FeatureManagement.Dashboard.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;

internal sealed class UpdateFeatureFlagUseCase(
  IFeatureManagementContext context,
  FeatureFlagCacheState cacheState,
  IValidator<FeatureFlag> validator,
  ICurrentUserProvider currentUserProvider,
  TimeProvider timeProvider) : IUpdateFeatureFlagUseCase
{
  public async Task ExecuteAsync(string name, FeatureFlag updatedFlag, CancellationToken cancellationToken = default)
  {
    var changedBy = currentUserProvider.GetCurrentUserOrSystem();

    var validation = await validator.ValidateAsync(updatedFlag, cancellationToken);
    if (!validation.IsValid)
      throw new ValidationException(validation.Errors);

    var existing = await context.FeatureFlags
      .Include(flag => flag.EnabledFor)
      .FirstOrDefaultAsync(flag => flag.Name == name, cancellationToken);

    if (existing == null)
      throw new FeatureFlagNotFoundException(name);

    if (updatedFlag.Version != existing.Version)
      throw new FeatureFlagVersionConflictException(existing.Version);

    var normalizedTags = (updatedFlag.Tags ?? [])
      .Where(tag => !string.IsNullOrWhiteSpace(tag))
      .Select(tag => tag.Trim())
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToList();

    var previousOwner = existing.Owner;
    var previousTags = existing.Tags.ToList();
    var previousRequirementType = existing.RequirementType;
    var previousScheduledAtUtc = existing.ScheduledAtUtc;
    var previousRuleSignature = BuildRuleSignature(existing.EnabledFor);

    var updatedRuleSignature = BuildRuleSignature(updatedFlag.EnabledFor);
    var changedFields = GetChangedFields(
      previousOwner,
      updatedFlag.Owner,
      previousTags,
      normalizedTags,
      previousRequirementType,
      updatedFlag.RequirementType,
      previousRuleSignature,
      updatedRuleSignature,
      previousScheduledAtUtc,
      updatedFlag.ScheduledAtUtc);

    context.FeatureFilters.RemoveRange(existing.EnabledFor);
    existing.Owner = updatedFlag.Owner.Trim();
    existing.Tags = normalizedTags;
    existing.RequirementType = updatedFlag.RequirementType;
    existing.ScheduledAtUtc = updatedFlag.ScheduledAtUtc;
    existing.EnabledFor = updatedFlag.EnabledFor
      .Select(filter => new FeatureFilter
      {
        Name = filter.Name,
        ParametersJson = filter.ParametersJson,
        FeatureFlagName = existing.Name
      })
      .ToList();
    existing.Version++;
    existing.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

    context.FeatureFlagAuditLogs.Add(new FeatureFlagAuditLog
    {
      FeatureFlagName = existing.Name,
      Action = FeatureFlagAuditAction.Updated,
      SnapshotVersion = existing.Version,
      SnapshotJson = FeatureFlagAuditSnapshotSerializer.Serialize(existing),
      ChangedAtUtc = existing.UpdatedAtUtc,
      ChangedBy = changedBy
    });
    context.FeatureFlagActivityEntries.Add(new FeatureFlagActivityEntry
    {
      FeatureFlagName = existing.Name,
      ActivityType = "Updated",
      Description = BuildUpdateDescription(changedFields),
      ChangeType = changedFields.Count == 0 ? "General" : string.Join(",", changedFields),
      ChangedAtUtc = existing.UpdatedAtUtc,
      ChangedBy = changedBy
    });

    try
    {
      await context.SaveChangesAsync(cancellationToken);
    }
    catch (DbUpdateConcurrencyException)
    {
      var currentVersion = await context.FeatureFlags
        .Where(featureFlag => featureFlag.Name == name)
        .Select(featureFlag => featureFlag.Version)
        .FirstOrDefaultAsync(cancellationToken);

      throw new FeatureFlagVersionConflictException(currentVersion);
    }

    cacheState.Bump();
  }

  private static List<string> BuildRuleSignature(IEnumerable<FeatureFilter> filters)
    => filters
      .Select(filter => $"{filter.Name}:{filter.ParametersJson}")
      .OrderBy(signature => signature, StringComparer.Ordinal)
      .ToList();

  private static List<string> GetChangedFields(
    string previousOwner,
    string nextOwner,
    List<string> previousTags,
    List<string> nextTags,
    RequirementType previousRequirementType,
    RequirementType nextRequirementType,
    List<string> previousRuleSignature,
    List<string> nextRuleSignature,
    DateTime? previousScheduledAtUtc,
    DateTime? nextScheduledAtUtc)
  {
    var changedFields = new List<string>();

    if (!string.Equals(previousOwner.Trim(), nextOwner.Trim(), StringComparison.Ordinal))
      changedFields.Add("Owner");

    if (!previousTags.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
          .SequenceEqual(nextTags.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase))
      changedFields.Add("Tags");

    if (previousRequirementType != nextRequirementType)
      changedFields.Add("RequirementType");

    if (!previousRuleSignature.SequenceEqual(nextRuleSignature, StringComparer.Ordinal))
      changedFields.Add("Rules");

    if (previousScheduledAtUtc != nextScheduledAtUtc)
      changedFields.Add("ScheduledAtUtc");

    return changedFields;
  }

  private static string BuildUpdateDescription(List<string> changedFields)
    => changedFields.Count == 0
      ? "Feature flag updated."
      : $"Updated {string.Join(", ", changedFields)}.";
}