using FeatureManagement.Dashboard.Infrastructure.Exceptions;
using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;

internal sealed class GetFeatureFlagByNameUseCase(IFeatureManagementContext context) : IGetFeatureFlagByNameUseCase
{
  public async Task<FeatureFlag> ExecuteAsync(string name, CancellationToken cancellationToken = default)
  {
    var featureFlag = await context.FeatureFlags
      .Include(flag => flag.EnabledFor)
      .AsNoTracking()
      .FirstOrDefaultAsync(flag => flag.Name == name, cancellationToken);

    return featureFlag ?? throw new FeatureFlagNotFoundException(name);
  }
}

