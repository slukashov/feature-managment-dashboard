using FeatureManagement.Dashboard.Infrastructure.Cache;
using FeatureManagement.Dashboard.Infrastructure.Exceptions;
using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;

internal sealed class CreateFeatureFlagUseCase(
  IFeatureManagementContext context,
  FeatureFlagCacheState cacheState,
  IValidator<FeatureFlag> validator,
  TimeProvider timeProvider) : ICreateFeatureFlagUseCase
{
  public async Task<FeatureFlag> ExecuteAsync(FeatureFlag flag, CancellationToken cancellationToken = default)
  {
    var validation = await validator.ValidateAsync(flag, cancellationToken);
    if (!validation.IsValid)
      throw new ValidationException(validation.Errors);

    if (await context.FeatureFlags.AnyAsync(featureFlag => featureFlag.Name == flag.Name, cancellationToken))
      throw new FeatureFlagAlreadyExistsException(flag.Name);

    flag.Version = Models.Constants.DefaultVersion;
    flag.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;
    foreach (var filter in flag.EnabledFor)
      filter.FeatureFlagName = flag.Name;

    context.FeatureFlags.Add(flag);
    await context.SaveChangesAsync(cancellationToken);
    cacheState.Bump();
    return flag;
  }
}