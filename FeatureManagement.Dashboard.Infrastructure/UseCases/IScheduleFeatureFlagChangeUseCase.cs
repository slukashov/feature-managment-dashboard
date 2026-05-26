using FeatureManagement.Dashboard.Models;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases;

/// <summary>
/// Use case for scheduling a feature flag state change at a future date/time.
/// Stores the scheduled timestamp; actual execution requires a background service.
/// </summary>
public interface IScheduleFeatureFlagChangeUseCase
{
  Task<FeatureFlag> ExecuteAsync(string name, FeatureFlag flag, DateTime scheduledAtUtc, CancellationToken cancellationToken = default);
}

