using FeatureManagement.Dashboard.Models;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases;

/// <summary>
/// Represents a use case for updating an existing feature flag.
/// </summary>
public interface IUpdateFeatureFlagUseCase
{
  /// <summary>
  /// Updates the specified feature flag with the provided updated flag data.
  /// </summary>
  /// <param name="name">The name of the feature flag to update.</param>
  /// <param name="updatedFlag">The updated feature flag data to apply.</param>
  /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  Task ExecuteAsync(string name, FeatureFlag updatedFlag, CancellationToken cancellationToken = default);
}