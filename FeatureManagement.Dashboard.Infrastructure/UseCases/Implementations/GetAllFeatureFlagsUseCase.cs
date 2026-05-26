using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;

internal sealed class GetAllFeatureFlagsUseCase(IFeatureManagementContext context) : IGetAllFeatureFlagsUseCase
{
  public async Task<List<FeatureFlag>> ExecuteAsync(
    string? search = null,
    string? owner = null,
    string? tag = null,
    CancellationToken cancellationToken = default)
  {
    var query = context.FeatureFlags
      .Include(featureFlag => featureFlag.EnabledFor)
      .AsQueryable();

    if (!string.IsNullOrWhiteSpace(search))
    {
      var searchTerm = search.Trim().ToLowerInvariant();
      query = query.Where(featureFlag =>
        featureFlag.Name.ToLower().Contains(searchTerm) ||
        featureFlag.Owner.ToLower().Contains(searchTerm));
    }

    if (!string.IsNullOrWhiteSpace(owner))
    {
      var ownerFilter = owner.Trim().ToLowerInvariant();
      query = query.Where(featureFlag => featureFlag.Owner.ToLower() == ownerFilter);
    }

    var flags = await query.ToListAsync(cancellationToken);

    if (!string.IsNullOrWhiteSpace(tag))
    {
      var tagFilter = tag.Trim();
      flags = flags
        .Where(featureFlag => featureFlag.Tags.Any(value => string.Equals(value, tagFilter, StringComparison.OrdinalIgnoreCase)))
        .ToList();
    }

    return flags;
  }
}