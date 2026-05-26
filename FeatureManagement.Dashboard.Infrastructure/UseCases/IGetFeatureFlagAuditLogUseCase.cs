using FeatureManagement.Dashboard.Models;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases;

public interface IGetFeatureFlagAuditLogUseCase
{
  Task<List<FeatureFlagAuditLog>> ExecuteAsync(string name, CancellationToken cancellationToken = default);
}
