using FeatureManagement.Dashboard.Models;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases;

public interface IConfigureFeatureFlagExperimentUseCase
{
  Task<FeatureFlagExperiment> ExecuteAsync(string featureFlagName, FeatureFlagExperimentConfiguration configuration,
    CancellationToken cancellationToken = default);
}

