using FeatureManagement.Dashboard.Infrastructure.Exceptions;
using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;

internal sealed class GetFeatureFlagAuditLogUseCase(IFeatureManagementContext context) : IGetFeatureFlagAuditLogUseCase
{
  public async Task<List<FeatureFlagAuditLog>> ExecuteAsync(string name, CancellationToken cancellationToken = default)
  {
    var exists = await context.FeatureFlags.AnyAsync(featureFlag => featureFlag.Name == name, cancellationToken);
    if (!exists)
      throw new FeatureFlagNotFoundException(name);

    return await context.FeatureFlagAuditLogs
      .AsNoTracking()
      .Where(entry => entry.FeatureFlagName == name)
      .OrderByDescending(entry => entry.Id)
      .ToListAsync(cancellationToken);
  }
}

