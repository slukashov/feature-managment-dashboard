using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Persistence.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FeatureManagement.Dashboard.Persistence;

/// <summary>
/// Provides functionality for configuring persistence support in the feature management
/// dashboard. This class is responsible for registering the feature management data context
/// and integrating it with the application using dependency injection.
/// </summary>
public static class Configuration
{
  /// <summary>
  /// Adds persistence support to the service collection using the specified
  /// database context options action. This method registers the feature management
  /// data context and configures it with the provided options.
  /// </summary>
  /// <param name="services">
  /// The service collection to which the persistence components will be added.
  /// </param>
  /// <param name="optionsAction">
  /// A delegate to configure the database context options, such as connection strings
  /// or provider-specific settings.
  /// </param>
  /// <returns>
  /// The updated service collection with persistence components registered.
  /// </returns>
  public static IServiceCollection AddPersistence(this IServiceCollection services,
    Action<DbContextOptionsBuilder> optionsAction)
  {
    return services
      .AddDbContext<IFeatureManagementContext, FeatureManagementContext>(optionsAction);
  }
}