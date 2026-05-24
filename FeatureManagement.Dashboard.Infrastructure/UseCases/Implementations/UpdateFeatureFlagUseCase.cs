using FeatureManagement.Dashboard.Infrastructure.Cache;
using FeatureManagement.Dashboard.Infrastructure.Exceptions;
using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;

internal sealed class UpdateFeatureFlagUseCase(
  IFeatureManagementContext context,
  FeatureFlagCacheState cacheState,
  IValidator<FeatureFlag> validator,
  TimeProvider timeProvider) : IUpdateFeatureFlagUseCase
{
  public async Task ExecuteAsync(string name, FeatureFlag updatedFlag, CancellationToken cancellationToken = default)
  {
    var validation = await validator.ValidateAsync(updatedFlag, cancellationToken);
    if (!validation.IsValid)
      throw new ValidationException(validation.Errors);

    var existing = await context.FeatureFlags
      .Include(flag => flag.EnabledFor)
      .FirstOrDefaultAsync(flag => flag.Name == name, cancellationToken);

    if (existing == null)
      throw new FeatureFlagNotFoundException(name);

    if (updatedFlag.Version != existing.Version)
      throw new FeatureFlagVersionConflictException(existing.Version);

    context.FeatureFilters.RemoveRange(existing.EnabledFor);
    existing.RequirementType = updatedFlag.RequirementType;
    existing.EnabledFor = updatedFlag.EnabledFor
      .Select(filter => new FeatureFilter
      {
        Name = filter.Name,
        ParametersJson = filter.ParametersJson,
        FeatureFlagName = existing.Name
      })
      .ToList();
    existing.Version++;
    existing.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

    await context.SaveChangesAsync(cancellationToken);
    cacheState.Bump();
  }
}