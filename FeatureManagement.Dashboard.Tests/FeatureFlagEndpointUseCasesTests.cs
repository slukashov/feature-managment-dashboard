using FeatureManagement.Dashboard.Infrastructure.Exceptions;
using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Infrastructure.UseCases;
using FeatureManagement.Dashboard.Models;
using FluentValidation;

namespace FeatureManagement.Dashboard.Tests;

public sealed class FeatureFlagEndpointUseCasesTests
{
  [Fact]
  public async Task GetAllFeatureFlagsUseCase_returns_all_flags_with_filters()
  {
    using var provider = CreateProvider();
    await using var scope = provider.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<IFeatureManagementContext>();
    var useCase = scope.ServiceProvider.GetRequiredService<IGetAllFeatureFlagsUseCase>();

    db.FeatureFlags.Add(new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.All,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = "{\"Value\":25}",
          FeatureFlagName = "beta-dashboard"
        }
      ]
    });
    await db.SaveChangesAsync();

    var flags = await useCase.ExecuteAsync();

    var flag = Assert.Single(flags);
    Assert.Equal("beta-dashboard", flag.Name);
    Assert.Single(flag.EnabledFor);
  }

  [Fact]
  public async Task CreateFeatureFlagUseCase_throws_validation_exception_for_invalid_payload()
  {
    using var provider = CreateProvider();
    await using var scope = provider.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<IFeatureManagementContext>();
    var useCase = scope.ServiceProvider.GetRequiredService<ICreateFeatureFlagUseCase>();

    await Assert.ThrowsAsync<ValidationException>(() => useCase.ExecuteAsync(new FeatureFlag
    {
      Name = string.Empty,
      RequirementType = RequirementType.Any,
      EnabledFor = []
    }));

    Assert.Empty(await db.FeatureFlags.ToListAsync());
  }

  [Fact]
  public async Task CreateFeatureFlagUseCase_throws_conflict_exception_for_duplicate_name()
  {
    using var provider = CreateProvider();
    await using var scope = provider.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<IFeatureManagementContext>();
    var useCase = scope.ServiceProvider.GetRequiredService<ICreateFeatureFlagUseCase>();

    var existingFlag = BuildValidFlag("beta-dashboard", RequirementType.Any, "{\"Value\":10}");
    existingFlag.EnabledFor[0].FeatureFlagName = existingFlag.Name;
    db.FeatureFlags.Add(existingFlag);
    await db.SaveChangesAsync();

    await Assert.ThrowsAsync<FeatureFlagAlreadyExistsException>(() =>
      useCase.ExecuteAsync(BuildValidFlag("beta-dashboard", RequirementType.All, "{\"Value\":70}")));
  }

  [Fact]
  public async Task CreateFeatureFlagUseCase_creates_flag_sets_metadata_and_bumps_cache()
  {
    using var provider = CreateProvider(new FixedTimeProvider(new DateTimeOffset(2026, 5, 24, 10, 0, 0, TimeSpan.Zero)));
    await using var scope = provider.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<IFeatureManagementContext>();
    var useCase = scope.ServiceProvider.GetRequiredService<ICreateFeatureFlagUseCase>();

    var now = new DateTimeOffset(2026, 5, 24, 10, 0, 0, TimeSpan.Zero);

    var created = await useCase.ExecuteAsync(BuildValidFlag("beta-dashboard", RequirementType.Any, "{\"Value\":35}"));

    Assert.Equal("beta-dashboard", created.Name);

    var stored = await db.FeatureFlags.Include(flag => flag.EnabledFor).SingleAsync();
    Assert.Equal(1, stored.Version);
    Assert.Equal(now.UtcDateTime, stored.UpdatedAtUtc);
    Assert.Equal(stored.Name, Assert.Single(stored.EnabledFor).FeatureFlagName);
  }

  [Fact]
  public async Task UpdateFeatureFlagUseCase_throws_not_found_when_flag_does_not_exist()
  {
    using var provider = CreateProvider();
    await using var scope = provider.CreateAsyncScope();
    var useCase = scope.ServiceProvider.GetRequiredService<IUpdateFeatureFlagUseCase>();

    await Assert.ThrowsAsync<FeatureFlagNotFoundException>(() =>
      useCase.ExecuteAsync("missing", BuildValidFlag("missing", RequirementType.All, "{\"Value\":20}")));
  }

  [Fact]
  public async Task UpdateFeatureFlagUseCase_throws_conflict_for_version_mismatch()
  {
    using var provider = CreateProvider();
    await using var scope = provider.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<IFeatureManagementContext>();
    var useCase = scope.ServiceProvider.GetRequiredService<IUpdateFeatureFlagUseCase>();

    var existingFlag = BuildValidFlag("beta-dashboard", RequirementType.All, "{\"Value\":20}");
    existingFlag.Version = 3;
    existingFlag.EnabledFor[0].FeatureFlagName = existingFlag.Name;
    db.FeatureFlags.Add(existingFlag);
    await db.SaveChangesAsync();

    var update = BuildValidFlag("beta-dashboard", RequirementType.Any, "{\"Value\":60}");
    update.Version = 1;

    var exception = await Assert.ThrowsAsync<FeatureFlagVersionConflictException>(() =>
      useCase.ExecuteAsync("beta-dashboard", update));

    Assert.Equal(3, exception.CurrentVersion);

    var stored = await db.FeatureFlags.SingleAsync();
    Assert.Equal(3, stored.Version);
  }

  [Fact]
  public async Task UpdateFeatureFlagUseCase_updates_existing_flag_and_bumps_cache()
  {
    using var provider = CreateProvider(new FixedTimeProvider(new DateTimeOffset(2026, 5, 24, 11, 0, 0, TimeSpan.Zero)));
    await using var scope = provider.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<IFeatureManagementContext>();
    var useCase = scope.ServiceProvider.GetRequiredService<IUpdateFeatureFlagUseCase>();

    var existingFlag = BuildValidFlag("beta-dashboard", RequirementType.All, "{\"Value\":20}");
    existingFlag.EnabledFor[0].FeatureFlagName = existingFlag.Name;
    db.FeatureFlags.Add(existingFlag);
    await db.SaveChangesAsync();

    var now = new DateTimeOffset(2026, 5, 24, 11, 0, 0, TimeSpan.Zero);

    var update = new FeatureFlag
    {
      Name = "beta-dashboard",
      RequirementType = RequirementType.Any,
      Version = 1,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.TimeWindow",
          ParametersJson = "{\"Start\":\"2026-05-24T09:00:00Z\",\"End\":\"2026-05-24T12:00:00Z\"}"
        }
      ]
    };

    await useCase.ExecuteAsync("beta-dashboard", update);

    var stored = await db.FeatureFlags.Include(flag => flag.EnabledFor).SingleAsync();
    Assert.Equal(RequirementType.Any, stored.RequirementType);
    Assert.Equal(2, stored.Version);
    Assert.Equal(now.UtcDateTime, stored.UpdatedAtUtc);
    var filter = Assert.Single(stored.EnabledFor);
    Assert.Equal("Microsoft.TimeWindow", filter.Name);
    Assert.Equal("beta-dashboard", filter.FeatureFlagName);
  }

  [Fact]
  public async Task DeleteFeatureFlagUseCase_throws_not_found_for_missing_flag()
  {
    using var provider = CreateProvider();
    await using var scope = provider.CreateAsyncScope();
    var useCase = scope.ServiceProvider.GetRequiredService<IDeleteFeatureFlagUseCase>();

    await Assert.ThrowsAsync<FeatureFlagNotFoundException>(() => useCase.ExecuteAsync("missing"));
  }

  [Fact]
  public async Task DeleteFeatureFlagUseCase_deletes_flag_and_bumps_cache()
  {
    using var provider = CreateProvider();
    await using var scope = provider.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<IFeatureManagementContext>();
    var useCase = scope.ServiceProvider.GetRequiredService<IDeleteFeatureFlagUseCase>();

    var flag = BuildValidFlag("beta-dashboard", RequirementType.All, "{\"Value\":25}");
    flag.EnabledFor[0].FeatureFlagName = flag.Name;
    db.FeatureFlags.Add(flag);
    await db.SaveChangesAsync();

    await useCase.ExecuteAsync("beta-dashboard");
    Assert.Empty(await db.FeatureFlags.ToListAsync());
  }

  private static ServiceProvider CreateProvider(TimeProvider? timeProvider = null)
  {
    var services = new ServiceCollection();
    services.AddFeatureManagementUi(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()), timeProvider);
    return services.BuildServiceProvider();
  }


  private static FeatureFlag BuildValidFlag(string name, RequirementType requirementType, string parametersJson)
    => new()
    {
      Name = name,
      RequirementType = requirementType,
      Version = 1,
      EnabledFor =
      [
        new FeatureFilter
        {
          Name = "Microsoft.Percentage",
          ParametersJson = parametersJson
        }
      ]
    };

  private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
  {
    public override DateTimeOffset GetUtcNow() => utcNow;
  }
}

