using FeatureManagement.Dashboard.Models;
using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FluentValidation;

namespace FeatureManagement.Dashboard.Tests;

public sealed class FeatureManagementServiceRegistrationTests
{
  [Fact]
  public void AddDbFeatureManagementUi_registers_expected_services_and_custom_time_provider()
  {
    var services = new ServiceCollection();
    var customTimeProvider = new FakeTimeProvider(new DateTimeOffset(2026, 5, 23, 12, 0, 0, TimeSpan.Zero));

    services.AddFeatureManagementUi(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()), customTimeProvider);

    using var provider = services.BuildServiceProvider();
    using var scope = provider.CreateScope();

    var resolvedTimeProvider = scope.ServiceProvider.GetRequiredService<TimeProvider>();
    var validator = scope.ServiceProvider.GetRequiredService<IValidator<FeatureFlag>>();
    var featureDefinitionProvider = scope.ServiceProvider.GetRequiredService<IFeatureDefinitionProvider>();

    Assert.Same(customTimeProvider, resolvedTimeProvider);
    Assert.NotNull(validator);
    Assert.NotNull(featureDefinitionProvider);
  }

  [Fact]
  public void AddDbFeatureManagementUi_uses_system_time_provider_by_default()
  {
    var services = new ServiceCollection();
    services.AddFeatureManagementUi(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

    using var provider = services.BuildServiceProvider();
    var resolvedTimeProvider = provider.GetRequiredService<TimeProvider>();

    Assert.Same(TimeProvider.System, resolvedTimeProvider);
  }

  [Fact]
  public void Feature_flag_validator_is_scoped_per_request_scope()
  {
    var services = new ServiceCollection();
    services.AddFeatureManagementUi(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

    using var provider = services.BuildServiceProvider();
    using var firstScope = provider.CreateScope();
    using var secondScope = provider.CreateScope();

    var firstValidator = firstScope.ServiceProvider.GetRequiredService<IValidator<FeatureFlag>>();
    var secondValidator = secondScope.ServiceProvider.GetRequiredService<IValidator<FeatureFlag>>();

    Assert.NotSame(firstValidator, secondValidator);
  }

  [Fact]
  public void AddDbFeatureManagementUi_registers_single_store_initializer_hosted_service()
  {
    var services = new ServiceCollection();
    services.AddFeatureManagementUi(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
    services.AddFeatureManagementUi(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

    var initializers = services
      .Where(service => service.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
      .Where(service => service.ImplementationType == typeof(FeatureManagementStoreInitializerHostedService))
      .ToList();

    Assert.Single(initializers);
  }

  [Fact]
  public void AddDbFeatureManagementUi_applies_schema_options()
  {
    var services = new ServiceCollection();
    services.AddFeatureManagementUi(
      options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()),
      TimeProvider.System,
      schemaOptions =>
      {
        schemaOptions.SqlScriptProvider = FeatureManagementSqlScriptProvider.Sqlite;
      });

    using var provider = services.BuildServiceProvider();
    var schemaOptions = provider.GetRequiredService<FeatureManagementSchemaOptions>();

    Assert.Equal(FeatureManagementSqlScriptProvider.Sqlite, schemaOptions.SqlScriptProvider);
  }

  [Fact]
  public void Feature_flag_version_is_configured_as_concurrency_token()
  {
    var services = new ServiceCollection();
    services.AddFeatureManagementUi(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

    using var provider = services.BuildServiceProvider();
    using var scope = provider.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<IFeatureManagementContext>();
    var dbContext = (DbContext)db;

    var versionProperty = dbContext.Model.FindEntityType(typeof(FeatureFlag))?.FindProperty(nameof(FeatureFlag.Version));

    Assert.NotNull(versionProperty);
    Assert.True(versionProperty!.IsConcurrencyToken);
  }

  private sealed class FakeTimeProvider(DateTimeOffset utcNow) : TimeProvider
  {
    public override DateTimeOffset GetUtcNow() => utcNow;
  }
}