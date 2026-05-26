using FeatureManagement.Dashboard.Infrastructure.Cache;
using FeatureManagement.Dashboard.Infrastructure.Exceptions;
using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;

internal sealed class DeleteFeatureFlagUseCase(
  IFeatureManagementContext context,
  FeatureFlagCacheState cacheState,
  TimeProvider timeProvider) : IDeleteFeatureFlagUseCase
{
  public async Task ExecuteAsync(string name, CancellationToken cancellationToken = default)
  {
    var flag = await context.FeatureFlags
      .Include(featureFlag => featureFlag.EnabledFor)
      .FirstOrDefaultAsync(featureFlag => featureFlag.Name == name, cancellationToken);

    if (flag is null)
      throw new FeatureFlagNotFoundException(name);

    context.FeatureFlagAuditLogs.Add(new FeatureFlagAuditLog
    {
      FeatureFlagName = flag.Name,
      Action = FeatureFlagAuditAction.Deleted,
      SnapshotVersion = flag.Version,
      SnapshotJson = FeatureFlagAuditSnapshotSerializer.Serialize(flag),
      ChangedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
      ChangedBy = "system"
    });
    context.FeatureFlagActivityEntries.Add(new FeatureFlagActivityEntry
    {
      FeatureFlagName = flag.Name,
      ActivityType = "Deleted",
      Description = "Feature flag deleted.",
      ChangeType = "Deleted",
      ChangedAtUtc = timeProvider.GetUtcNow().UtcDateTime,
      ChangedBy = "system"
    });

    context.FeatureFlags.Remove(flag);
    await context.SaveChangesAsync(cancellationToken);
    cacheState.Bump();
  }
}