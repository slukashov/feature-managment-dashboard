using FeatureManagement.Dashboard.Infrastructure.Cache;
using FeatureManagement.Dashboard.Infrastructure.Exceptions;
using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Infrastructure.Providers;
using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;

internal sealed class RollbackFeatureFlagUseCase(
  IFeatureManagementContext context,
  FeatureFlagCacheState cacheState,
  ICurrentUserProvider currentUserProvider,
  TimeProvider timeProvider) : IRollbackFeatureFlagUseCase
{
  public async Task<FeatureFlag> ExecuteAsync(string name, int targetVersion, CancellationToken cancellationToken = default)
  {
    var changedBy = currentUserProvider.GetCurrentUserOrSystem();

    var rollbackSource = await context.FeatureFlagAuditLogs
      .AsNoTracking()
      .Where(entry => entry.FeatureFlagName == name && entry.SnapshotVersion == targetVersion)
      .OrderByDescending(entry => entry.Id)
      .FirstOrDefaultAsync(cancellationToken);

    if (rollbackSource is null)
      throw new FeatureFlagRollbackVersionNotFoundException(name, targetVersion);

    var snapshot = FeatureFlagAuditSnapshotSerializer.Deserialize(rollbackSource.SnapshotJson);

    var existing = await context.FeatureFlags
      .Include(flag => flag.EnabledFor)
      .FirstOrDefaultAsync(flag => flag.Name == name, cancellationToken);

    var currentVersion = existing?.Version ?? 0;
    var nextVersion = Math.Max(currentVersion, targetVersion) + 1;
    var now = timeProvider.GetUtcNow().UtcDateTime;

    if (existing is null)
    {
      var restored = new FeatureFlag
      {
        Name = name,
        RequirementType = snapshot.RequirementType,
        Version = nextVersion,
        UpdatedAtUtc = now,
        EnabledFor = snapshot.EnabledFor
          .Select(filter => new FeatureFilter
          {
            Name = filter.Name,
            ParametersJson = filter.ParametersJson,
            FeatureFlagName = name
          })
          .ToList()
      };

      context.FeatureFlags.Add(restored);
      context.FeatureFlagAuditLogs.Add(CreateAuditLog(restored, FeatureFlagAuditAction.RolledBack, now, changedBy));
      context.FeatureFlagActivityEntries.Add(CreateActivityLog(name, targetVersion, now, changedBy));
      await context.SaveChangesAsync(cancellationToken);
      cacheState.Bump();
      return restored;
    }

    context.FeatureFilters.RemoveRange(existing.EnabledFor);
    existing.RequirementType = snapshot.RequirementType;
    existing.EnabledFor = snapshot.EnabledFor
      .Select(filter => new FeatureFilter
      {
        Name = filter.Name,
        ParametersJson = filter.ParametersJson,
        FeatureFlagName = name
      })
      .ToList();
    existing.Version = nextVersion;
    existing.UpdatedAtUtc = now;

    context.FeatureFlagAuditLogs.Add(CreateAuditLog(existing, FeatureFlagAuditAction.RolledBack, now, changedBy));
    context.FeatureFlagActivityEntries.Add(CreateActivityLog(name, targetVersion, now, changedBy));
    await context.SaveChangesAsync(cancellationToken);
    cacheState.Bump();
    return existing;
  }

  private static FeatureFlagAuditLog CreateAuditLog(FeatureFlag flag, FeatureFlagAuditAction action, DateTime changedAtUtc, string changedBy)
    => new()
    {
      FeatureFlagName = flag.Name,
      Action = action,
      SnapshotVersion = flag.Version,
      SnapshotJson = FeatureFlagAuditSnapshotSerializer.Serialize(flag),
      ChangedAtUtc = changedAtUtc,
      ChangedBy = changedBy
    };

  private static FeatureFlagActivityEntry CreateActivityLog(string featureFlagName, int targetVersion, DateTime changedAtUtc, string changedBy)
    => new()
    {
      FeatureFlagName = featureFlagName,
      ActivityType = "RolledBack",
      Description = $"Feature flag rolled back to version {targetVersion}.",
      ChangeType = "Version",
      ChangedAtUtc = changedAtUtc,
      ChangedBy = changedBy
    };
}

