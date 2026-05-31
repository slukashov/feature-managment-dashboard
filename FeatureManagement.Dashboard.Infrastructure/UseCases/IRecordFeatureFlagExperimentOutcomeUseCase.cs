using FeatureManagement.Dashboard.Models;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases;

public interface IRecordFeatureFlagExperimentOutcomeUseCase
{
  Task ExecuteAsync(string featureFlagName, FeatureFlagExperimentOutcome outcome,
    CancellationToken cancellationToken = default);
}

