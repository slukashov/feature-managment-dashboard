using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;

internal sealed class GetFeatureFlagActivityFeedUseCase(IFeatureManagementContext context) : IGetFeatureFlagActivityFeedUseCase
{
  public async Task<List<FeatureFlagActivityEntry>> ExecuteAsync(string name, CancellationToken cancellationToken = default)
  {
    return await context.FeatureFlagActivityEntries
      .AsNoTracking()
      .Where(entry => entry.FeatureFlagName == name)
      .OrderByDescending(entry => entry.ChangedAtUtc)
      .ThenByDescending(entry => entry.Id)
      .ToListAsync(cancellationToken);
  }
}

