using FeatureManagement.Dashboard.Models;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases;

/// <summary>
/// Use case for retrieving human-readable activity feed for a feature flag.
/// Shows who changed what and when, separate from audit logs used for rollback.
/// </summary>
public interface IGetFeatureFlagActivityFeedUseCase
{
  Task<List<FeatureFlagActivityEntry>> ExecuteAsync(string name, CancellationToken cancellationToken = default);
}

