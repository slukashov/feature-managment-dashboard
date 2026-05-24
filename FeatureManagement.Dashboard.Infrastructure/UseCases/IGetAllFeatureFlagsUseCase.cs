using FeatureManagement.Dashboard.Models;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases;

/// <summary>
/// Represents a use case for retrieving all feature flags.
/// </summary>
public interface IGetAllFeatureFlagsUseCase
{
  /// <summary>
  /// Executes the operation to retrieve all feature flags.
  /// </summary>
  /// <param name="cancellationToken">
  /// A <see cref="System.Threading.CancellationToken"/> used to receive a signal for cancellation.
  /// </param>
  /// <returns>
  /// A task representing the asynchronous operation, containing a list of <see cref="FeatureManagement.Dashboard.Models.FeatureFlag"/> objects.
  /// </returns>
  Task<List<FeatureFlag>> ExecuteAsync(CancellationToken cancellationToken = default);
}