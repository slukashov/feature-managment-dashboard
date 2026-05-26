using FeatureManagement.Dashboard.Infrastructure.Cache;
using FeatureManagement.Dashboard.Infrastructure.Providers;
using FeatureManagement.Dashboard.Infrastructure.UseCases;
using FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;
using FeatureManagement.Dashboard.Infrastructure.Validators;
using FeatureManagement.Dashboard.Models;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace FeatureManagement.Dashboard.Infrastructure;

/// <summary>
/// Provides configuration methods for registering and setting up infrastructure components
/// required by the feature management dashboard. This class is designed to configure dependencies
/// such as caching, database providers, use cases, and validators in the service container.
/// </summary>
public static class Configuration
{
  /// <summary>
  /// Configures and registers infrastructure services required for the feature management dashboard.
  /// </summary>
  /// <param name="services">The service collection to which the infrastructure services will be added.</param>
  /// <returns>The updated service collection with the registered infrastructure services.</returns>
  public static IServiceCollection AddInfrastructure(this IServiceCollection services)
  {
    services.AddFeatureManagement();
    services.AddMemoryCache();
    services.AddSingleton<FeatureFlagCacheState>();
    services.AddSingleton<IFeatureDefinitionProvider, DatabaseFeatureDefinitionProvider>();
    services.AddScoped<IValidator<FeatureFlag>, FeatureFlagValidator>();
    services.AddScoped<IGetAllFeatureFlagsUseCase, GetAllFeatureFlagsUseCase>();
    services.AddScoped<IGetFeatureFlagByNameUseCase, GetFeatureFlagByNameUseCase>();
    services.AddScoped<IGetFeatureFlagAuditLogUseCase, GetFeatureFlagAuditLogUseCase>();
    services.AddScoped<ICreateFeatureFlagUseCase, CreateFeatureFlagUseCase>();
    services.AddScoped<IUpdateFeatureFlagUseCase, UpdateFeatureFlagUseCase>();
    services.AddScoped<IRollbackFeatureFlagUseCase, RollbackFeatureFlagUseCase>();
    services.AddScoped<IDeleteFeatureFlagUseCase, DeleteFeatureFlagUseCase>();
    services.AddScoped<IGetFeatureFlagActivityFeedUseCase, GetFeatureFlagActivityFeedUseCase>();
    return services.AddScoped<IScheduleFeatureFlagChangeUseCase, ScheduleFeatureFlagChangeUseCase>();
  }
}