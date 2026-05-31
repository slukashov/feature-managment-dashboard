using FeatureManagement.Dashboard.Models;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases;

public interface IAssignFeatureFlagExperimentVariantUseCase
{
  Task<FeatureFlagExperimentVariantAssignment> ExecuteAsync(string featureFlagName, string subjectKey,
    CancellationToken cancellationToken = default);
}

