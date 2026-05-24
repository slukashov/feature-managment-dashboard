using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;

internal sealed class GetAllFeatureFlagsUseCase(IFeatureManagementContext context) : IGetAllFeatureFlagsUseCase
{
  public Task<List<FeatureFlag>> ExecuteAsync(CancellationToken cancellationToken = default)
    => context.FeatureFlags
      .Include(featureFlag => featureFlag.EnabledFor)
      .ToListAsync(cancellationToken);
}