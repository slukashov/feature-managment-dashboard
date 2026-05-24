using System.Reflection;
using FeatureManagement.Dashboard.Infrastructure;
using FeatureManagement.Dashboard.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace FeatureManagement.Dashboard.Extensions;

public static class FeatureManagementUiExtensions
{
  /// <param name="services">
  /// The <see cref="IServiceCollection"/> to which the services are added.
  /// </param>
  extension(IServiceCollection services)
  {
    /// <summary>
    /// Adds database-backed feature management UI services to the dependency injection container.
    /// Configures the feature management system to use a database for storing feature flags
    /// and integrates UI components for feature flag management.
    /// </summary>
    /// <param name="optionsAction">
    /// A delegate to configure the <see cref="DbContextOptionsBuilder"/> for the feature management database.
    /// </param>
    /// <returns>
    /// The updated <see cref="IServiceCollection"/> with the feature management UI services added.
    /// </returns>
    public IServiceCollection AddFeatureManagementUi(Action<DbContextOptionsBuilder> optionsAction)
      => AddFeatureManagementUi(services, optionsAction, null, null);

    /// <summary>
    /// Configures and registers database-backed feature management services, including
    /// a caching mechanism, feature flag validation, custom time provider, and feature
    /// definition services, in the dependency injection container.
    /// </summary>
    /// <param name="optionsAction">
    /// A delegate to configure the <see cref="DbContextOptionsBuilder"/> for the feature management database.
    /// </param>
    /// <param name="timeProvider">
    /// An optional custom <see cref="TimeProvider"/> to manage time-related functionality within the feature management system.
    /// If null, the default system time provider is used.
    /// </param>
    /// <returns>
    /// The updated <see cref="IServiceCollection"/> with all services required for
    /// database-backed feature management added.
    /// </returns>
    public IServiceCollection AddFeatureManagementUi(Action<DbContextOptionsBuilder> optionsAction,
      TimeProvider? timeProvider)
      => AddFeatureManagementUi(services, optionsAction, timeProvider, null);

    /// <summary>
    /// Configures and registers database-backed feature management services with explicit schema initialization settings.
    /// </summary>
    /// <param name="optionsAction">
    /// A delegate to configure the <see cref="DbContextOptionsBuilder"/> for the feature management database.
    /// </param>
    /// <param name="timeProvider">
    /// An optional custom <see cref="TimeProvider"/> to manage time-related functionality within the feature management system.
    /// </param>
    /// <param name="schemaOptionsAction">
    /// A delegate used to configure schema initialization behavior, including migration mode and SQL script provider.
    /// </param>
    /// <returns>
    /// The updated <see cref="IServiceCollection"/> with all services required for
    /// database-backed feature management added.
    /// </returns>
    public IServiceCollection AddFeatureManagementUi(Action<DbContextOptionsBuilder> optionsAction,
      TimeProvider? timeProvider,
      Action<FeatureManagementSchemaOptions>? schemaOptionsAction)
    {
      var schemaOptions = new FeatureManagementSchemaOptions();
      schemaOptionsAction?.Invoke(schemaOptions);

      services.TryAddSingleton(timeProvider ?? TimeProvider.System);
      services.AddAuthorization();
      services.AddPersistence(optionsAction);
      services.AddInfrastructure();
      services.Replace(ServiceDescriptor.Singleton(schemaOptions));
      services.TryAddEnumerable(
        ServiceDescriptor.Singleton<IHostedService, FeatureManagementStoreInitializerHostedService>());
      return services;
    }
  }

  /// <param name="app">
  /// The <see cref="IApplicationBuilder"/> instance used to configure the application's request pipeline.
  /// </param>
  extension(IApplicationBuilder app)
  {
    /// <summary>
    /// Configures the application to serve the database-backed feature management UI.
    /// Includes static file hosting for the React-based UI and configures routing for the feature flag management interface.
    /// </summary>
    /// <param name="routePrefix">
    /// The base path under which the feature management UI is accessible. Defaults to "/feature-flags".
    /// </param>
    /// <returns>
    /// The <see cref="IApplicationBuilder"/> instance with the feature management UI middleware configured.
    /// </returns>
    public IApplicationBuilder UseFeatureManagementUi(string routePrefix = "/feature-flags")
    {
      var assembly = typeof(FeatureManagementUiExtensions).GetTypeInfo().Assembly;

      ManifestEmbeddedFileProvider? embeddedFileProvider = null;
      try
      {
        embeddedFileProvider = new ManifestEmbeddedFileProvider(assembly, "client-app/dist");
      }
      catch (InvalidOperationException)
      {
        // In test/CI runs the embedded SPA payload may be intentionally absent.
      }

      if (embeddedFileProvider is null)
      {
        app.Map(routePrefix, builder =>
        {
          builder.Run(async context =>
          {
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync("<html><body><h1>Feature Management UI is not embedded in this build.</h1></body></html>");
          });
        });

        return app;
      }

      app.UseStaticFiles(new StaticFileOptions
      {
        RequestPath = routePrefix,
        FileProvider = embeddedFileProvider
      });

      app.Map(routePrefix, builder =>
      {
        builder.Run(async context =>
        {
          context.Response.ContentType = "text/html";
          var fileInfo = embeddedFileProvider.GetFileInfo("index.html");
          await using var stream = fileInfo.CreateReadStream();
          using var reader = new StreamReader(stream);
          await context.Response.WriteAsync(await reader.ReadToEndAsync());
        });
      });

      return app;
    }
  }
  
  /// <summary>
  /// Maps feature management endpoints to the specified <see cref="IEndpointRouteBuilder"/>.
  /// Configures HTTP routes for retrieving, creating, updating, and deleting feature flags,
  /// protected by the provided access requirement.
  /// </summary>
  /// <param name="endpoints">
  /// The <see cref="IEndpointRouteBuilder"/> to which the feature management endpoints will be mapped.
  /// </param>
  /// <param name="accessRequirement">
  /// The <see cref="IAuthorizationRequirement"/> used to authorize access to feature management endpoints.
  /// </param>
  /// <param name="routePrefix">
  /// The base route prefix used for the feature management endpoints. Defaults to "/api/feature-flags".
  /// </param>
  /// <returns>
  /// The updated <see cref="IEndpointRouteBuilder"/> with the feature management endpoints added.
  /// </returns>
  public static IEndpointRouteBuilder MapFeatureManagementEndpoints(this IEndpointRouteBuilder endpoints,
    IAuthorizationRequirement accessRequirement, string routePrefix = "/api/feature-flags")
  {
    var group = endpoints.MapGroup(routePrefix);
    group.MapGet("/", FeatureFlagEndpointHandlers.GetAllAsync).RequireFeatureManagement(accessRequirement);
    group.MapPost("/", FeatureFlagEndpointHandlers.CreateAsync).RequireFeatureManagement(accessRequirement);
    group.MapPut("/{name}", FeatureFlagEndpointHandlers.UpdateAsync).RequireFeatureManagement(accessRequirement);
    group.MapDelete("/{name}", FeatureFlagEndpointHandlers.DeleteAsync).RequireFeatureManagement(accessRequirement);
    return endpoints;
  }
}