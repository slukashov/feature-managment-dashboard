using FeatureManagement.Dashboard.Infrastructure.Cache;
using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Infrastructure.Providers;
using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;

internal sealed class ScheduleFeatureFlagChangeUseCase(
  IFeatureManagementContext context,
  FeatureFlagCacheState cacheState,
  ICurrentUserProvider currentUserProvider,
  TimeProvider timeProvider) : IScheduleFeatureFlagChangeUseCase
{
  public async Task<FeatureFlag> ExecuteAsync(string name, FeatureFlag flag, DateTime scheduledAtUtc, CancellationToken cancellationToken = default)
  {
    var changedBy = currentUserProvider.GetCurrentUserOrSystem();

    var existing = await context.FeatureFlags
      .Include(f => f.EnabledFor)
      .FirstOrDefaultAsync(f => f.Name == name, cancellationToken);

    if (existing is null)
    {
      throw new InvalidOperationException($"Feature flag '{name}' not found.");
    }

    var now = timeProvider.GetUtcNow().UtcDateTime;
    if (scheduledAtUtc <= now)
    {
      throw new InvalidOperationException("Scheduled time must be in the future.");
    }

    // Update the flag with scheduled timestamp
    existing.ScheduledAtUtc = scheduledAtUtc;
    existing.Version++;
    existing.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

    // Replace filters
    context.FeatureFilters.RemoveRange(existing.EnabledFor);
    existing.EnabledFor = flag.EnabledFor
      .Select(f => new FeatureFilter
      {
        Name = f.Name,
        ParametersJson = f.ParametersJson,
        FeatureFlagName = name
      })
      .ToList();

    existing.RequirementType = flag.RequirementType;

    // Create activity entry
    var activityEntry = new FeatureFlagActivityEntry
    {
      FeatureFlagName = name,
      ActivityType = "Scheduled",
      Description = $"Scheduled rollout for {scheduledAtUtc:u}",
      ChangeType = "ScheduledAtUtc",
      ChangedAtUtc = existing.UpdatedAtUtc,
      ChangedBy = changedBy
    };

    context.FeatureFlagActivityEntries.Add(activityEntry);
    await context.SaveChangesAsync(cancellationToken);
    cacheState.Bump();

    return existing;
  }
}

