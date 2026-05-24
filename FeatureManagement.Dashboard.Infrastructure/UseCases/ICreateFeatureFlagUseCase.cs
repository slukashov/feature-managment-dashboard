using FeatureManagement.Dashboard.Models;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases;

/// <summary>
/// Defines the contract for creating a new feature flag in the feature management system.
/// </summary>
public interface ICreateFeatureFlagUseCase
{
  /// <summary>
  /// Executes the process of creating a new feature flag, validates the input, persists it to the data store,
  /// and updates the cache state appropriately.
  /// </summary>
  /// <param name="flag">The feature flag object to be created.</param>
  /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
  /// <returns>A task that represents the asynchronous operation, producing the created <see cref="FeatureFlag"/> object.</returns>
  Task<FeatureFlag> ExecuteAsync(FeatureFlag flag, CancellationToken cancellationToken = default);
}