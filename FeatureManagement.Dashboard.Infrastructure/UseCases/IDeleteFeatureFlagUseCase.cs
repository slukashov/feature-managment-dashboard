namespace FeatureManagement.Dashboard.Infrastructure.UseCases;

/// <summary>
/// Defines the contract for deleting an existing feature flag within the system.
/// </summary>
public interface IDeleteFeatureFlagUseCase
{
  /// <summary>
  /// Executes the deletion of an existing feature flag by its name.
  /// </summary>
  /// <param name="name">The name of the feature flag to be deleted.</param>
  /// <param name="cancellationToken">A cancellation token to observe while awaiting the operation.</param>
  /// <returns>A task that represents the asynchronous operation.</returns>
  Task ExecuteAsync(string name, CancellationToken cancellationToken = default);
}