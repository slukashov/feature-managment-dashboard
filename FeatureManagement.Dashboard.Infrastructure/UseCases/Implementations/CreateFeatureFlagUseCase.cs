using FeatureManagement.Dashboard.Infrastructure.Cache;
using FeatureManagement.Dashboard.Infrastructure.Exceptions;
using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Infrastructure.Providers;
using FeatureManagement.Dashboard.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;

internal sealed class CreateFeatureFlagUseCase(
  IFeatureManagementContext context,
  FeatureFlagCacheState cacheState,
  IValidator<FeatureFlag> validator,
  ICurrentUserProvider currentUserProvider,
  TimeProvider timeProvider) : ICreateFeatureFlagUseCase
{
  public async Task<FeatureFlag> ExecuteAsync(FeatureFlag flag, CancellationToken cancellationToken = default)
  {
    var changedBy = currentUserProvider.GetCurrentUserOrSystem();

    var validation = await validator.ValidateAsync(flag, cancellationToken);
    if (!validation.IsValid)
      throw new ValidationException(validation.Errors);

    if (await context.FeatureFlags.AnyAsync(featureFlag => featureFlag.Name == flag.Name, cancellationToken))
      throw new FeatureFlagAlreadyExistsException(flag.Name);

    flag.Owner = flag.Owner.Trim();
    flag.Tags = (flag.Tags ?? [])
      .Where(tag => !string.IsNullOrWhiteSpace(tag))
      .Select(tag => tag.Trim())
      .Distinct(StringComparer.OrdinalIgnoreCase)
      .ToList();

    flag.Version = Models.Constants.DefaultVersion;
    flag.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
    foreach (var filter in flag.EnabledFor)
      filter.FeatureFlagName = flag.Name;

    context.FeatureFlags.Add(flag);
    context.FeatureFlagAuditLogs.Add(new FeatureFlagAuditLog
    {
      FeatureFlagName = flag.Name,
      Action = FeatureFlagAuditAction.Created,
      SnapshotVersion = flag.Version,
      SnapshotJson = FeatureFlagAuditSnapshotSerializer.Serialize(flag),
      ChangedAtUtc = flag.UpdatedAtUtc,
      ChangedBy = changedBy
    });
    context.FeatureFlagActivityEntries.Add(new FeatureFlagActivityEntry
    {
      FeatureFlagName = flag.Name,
      ActivityType = "Created",
      Description = "Feature flag created.",
      ChangeType = null,
      ChangedAtUtc = flag.UpdatedAtUtc,
      ChangedBy = changedBy
    });

    await context.SaveChangesAsync(cancellationToken);
    cacheState.Bump();
    return flag;
  }
}