using FeatureManagement.Dashboard.Models;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases;

public interface IRollbackFeatureFlagUseCase
{
  Task<FeatureFlag> ExecuteAsync(string name, int targetVersion, CancellationToken cancellationToken = default);
}

