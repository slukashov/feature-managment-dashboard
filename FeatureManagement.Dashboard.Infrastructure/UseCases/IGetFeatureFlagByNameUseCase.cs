using FeatureManagement.Dashboard.Models;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases;

public interface IGetFeatureFlagByNameUseCase
{
  Task<FeatureFlag> ExecuteAsync(string name, CancellationToken cancellationToken = default);
}

