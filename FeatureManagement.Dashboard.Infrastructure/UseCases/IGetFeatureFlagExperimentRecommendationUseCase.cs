using FeatureManagement.Dashboard.Models;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases;

public interface IGetFeatureFlagExperimentRecommendationUseCase
{
  Task<FeatureFlagExperimentRecommendation> ExecuteAsync(string featureFlagName, CancellationToken cancellationToken = default);
}

