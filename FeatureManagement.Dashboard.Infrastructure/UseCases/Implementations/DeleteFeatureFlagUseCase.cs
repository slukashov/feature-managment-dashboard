using FeatureManagement.Dashboard.Infrastructure.Cache;
using FeatureManagement.Dashboard.Infrastructure.Exceptions;
using FeatureManagement.Dashboard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;

internal sealed class DeleteFeatureFlagUseCase(
  IFeatureManagementContext context,
  FeatureFlagCacheState cacheState) : IDeleteFeatureFlagUseCase
{
  public async Task ExecuteAsync(string name, CancellationToken cancellationToken = default)
  {
    var flag = await context.FeatureFlags
      .Include(featureFlag => featureFlag.EnabledFor)
      .FirstOrDefaultAsync(featureFlag => featureFlag.Name == name, cancellationToken);

    if (flag is null)
      throw new FeatureFlagNotFoundException(name);

    context.FeatureFlags.Remove(flag);
    await context.SaveChangesAsync(cancellationToken);
    cacheState.Bump();
  }
}