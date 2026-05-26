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

    var auditEntry = await db.FeatureFlagAuditLogs.SingleAsync();
    Assert.Equal("beta-dashboard", auditEntry.FeatureFlagName);
    Assert.Equal(FeatureFlagAuditAction.Created, auditEntry.Action);
    Assert.Equal(1, auditEntry.SnapshotVersion);

    var activityEntry = await db.FeatureFlagActivityEntries.SingleAsync();
    Assert.Equal("beta-dashboard", activityEntry.FeatureFlagName);
    Assert.Equal("Created", activityEntry.ActivityType);
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

    var auditEntries = await db.FeatureFlagAuditLogs.OrderBy(entry => entry.Id).ToListAsync();
    Assert.Single(auditEntries);
    Assert.Equal(FeatureFlagAuditAction.Updated, auditEntries[0].Action);
    Assert.Equal(2, auditEntries[0].SnapshotVersion);

    var activityEntries = await db.FeatureFlagActivityEntries.OrderBy(entry => entry.Id).ToListAsync();
    Assert.Single(activityEntries);
    Assert.Equal("Updated", activityEntries[0].ActivityType);
    Assert.Equal("RequirementType,Rules", activityEntries[0].ChangeType);
  }

  [Fact]
  public async Task UpdateFeatureFlagUseCase_throws_conflict_when_stale_tracked_entity_hits_db_concurrency_check()
  {
    var services = new ServiceCollection();
    var sharedDatabaseName = Guid.NewGuid().ToString();
    var sharedRoot = new InMemoryDatabaseRoot();
    services.AddFeatureManagementUi(options => options.UseInMemoryDatabase(sharedDatabaseName, sharedRoot));
    using var provider = services.BuildServiceProvider();

    await using (var seedScope = provider.CreateAsyncScope())
    {
      var seedDb = seedScope.ServiceProvider.GetRequiredService<IFeatureManagementContext>();
      var seededFlag = BuildValidFlag("beta-dashboard", RequirementType.Any, "{\"Value\":20}");
      seededFlag.EnabledFor[0].FeatureFlagName = seededFlag.Name;
      seedDb.FeatureFlags.Add(seededFlag);
      await seedDb.SaveChangesAsync();
    }

    await using var staleScope = provider.CreateAsyncScope();
    var staleDb = staleScope.ServiceProvider.GetRequiredService<IFeatureManagementContext>();
    var staleUseCase = staleScope.ServiceProvider.GetRequiredService<IUpdateFeatureFlagUseCase>();

    // Force a tracked stale snapshot in this context before the competing update commits.
    _ = await staleDb.FeatureFlags.Include(flag => flag.EnabledFor)
      .SingleAsync(flag => flag.Name == "beta-dashboard");

    await using (var concurrentScope = provider.CreateAsyncScope())
    {
      var concurrentUseCase = concurrentScope.ServiceProvider.GetRequiredService<IUpdateFeatureFlagUseCase>();
      await concurrentUseCase.ExecuteAsync("beta-dashboard", BuildValidFlag("beta-dashboard", RequirementType.All, "{\"Value\":70}"));
    }

    var staleUpdate = BuildValidFlag("beta-dashboard", RequirementType.Any, "{\"Value\":40}");
    staleUpdate.Version = 1;

    var conflict = await Assert.ThrowsAsync<FeatureFlagVersionConflictException>(() =>
      staleUseCase.ExecuteAsync("beta-dashboard", staleUpdate));

    Assert.Equal(2, conflict.CurrentVersion);
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

    var auditEntry = await db.FeatureFlagAuditLogs.SingleAsync();
    Assert.Equal(FeatureFlagAuditAction.Deleted, auditEntry.Action);
    Assert.Equal(1, auditEntry.SnapshotVersion);

    var activityEntry = await db.FeatureFlagActivityEntries.SingleAsync();
    Assert.Equal("Deleted", activityEntry.ActivityType);
  }

  [Fact]
  public async Task RollbackFeatureFlagUseCase_restores_previous_snapshot_and_writes_audit_event()
  {
    using var provider = CreateProvider(new FixedTimeProvider(new DateTimeOffset(2026, 5, 24, 12, 0, 0, TimeSpan.Zero)));
    await using var scope = provider.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<IFeatureManagementContext>();
    var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateFeatureFlagUseCase>();
    var updateUseCase = scope.ServiceProvider.GetRequiredService<IUpdateFeatureFlagUseCase>();
    var rollbackUseCase = scope.ServiceProvider.GetRequiredService<IRollbackFeatureFlagUseCase>();

    await createUseCase.ExecuteAsync(BuildValidFlag("beta-dashboard", RequirementType.Any, "{\"Value\":10}"));

    var update = BuildValidFlag("beta-dashboard", RequirementType.Any, "{\"Value\":75}");
    update.Version = 1;
    await updateUseCase.ExecuteAsync("beta-dashboard", update);

    var rolledBack = await rollbackUseCase.ExecuteAsync("beta-dashboard", 1);

    Assert.Equal(3, rolledBack.Version);
    Assert.Equal(RequirementType.Any, rolledBack.RequirementType);
    Assert.Equal("{\"Value\":10}", Assert.Single(rolledBack.EnabledFor).ParametersJson);

    var stored = await db.FeatureFlags.Include(flag => flag.EnabledFor).SingleAsync(flag => flag.Name == "beta-dashboard");
    Assert.Equal(3, stored.Version);
    Assert.Equal("{\"Value\":10}", Assert.Single(stored.EnabledFor).ParametersJson);

    var latestAudit = await db.FeatureFlagAuditLogs.OrderByDescending(entry => entry.Id).FirstAsync();
    Assert.Equal(FeatureFlagAuditAction.RolledBack, latestAudit.Action);
    Assert.Equal(3, latestAudit.SnapshotVersion);

    var latestActivity = await db.FeatureFlagActivityEntries.OrderByDescending(entry => entry.Id).FirstAsync();
    Assert.Equal("RolledBack", latestActivity.ActivityType);
  }

  [Fact]
  public async Task GetFeatureFlagAuditLogUseCase_returns_latest_entries_first()
  {
    using var provider = CreateProvider();
    await using var scope = provider.CreateAsyncScope();
    var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateFeatureFlagUseCase>();
    var updateUseCase = scope.ServiceProvider.GetRequiredService<IUpdateFeatureFlagUseCase>();
    var auditUseCase = scope.ServiceProvider.GetRequiredService<IGetFeatureFlagAuditLogUseCase>();

    await createUseCase.ExecuteAsync(BuildValidFlag("beta-dashboard", RequirementType.Any, "{\"Value\":10}"));
    var update = BuildValidFlag("beta-dashboard", RequirementType.All, "{\"Value\":20}");
    update.Version = 1;
    await updateUseCase.ExecuteAsync("beta-dashboard", update);

    var entries = await auditUseCase.ExecuteAsync("beta-dashboard");

    Assert.Equal(2, entries.Count);
    Assert.Equal(FeatureFlagAuditAction.Updated, entries[0].Action);
    Assert.Equal(FeatureFlagAuditAction.Created, entries[1].Action);
  }

  [Fact]
  public async Task GetFeatureFlagActivityFeedUseCase_returns_latest_entries_first()
  {
    using var provider = CreateProvider();
    await using var scope = provider.CreateAsyncScope();
    var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateFeatureFlagUseCase>();
    var updateUseCase = scope.ServiceProvider.GetRequiredService<IUpdateFeatureFlagUseCase>();
    var activityUseCase = scope.ServiceProvider.GetRequiredService<IGetFeatureFlagActivityFeedUseCase>();

    await createUseCase.ExecuteAsync(BuildValidFlag("beta-dashboard", RequirementType.Any, "{\"Value\":10}"));
    var update = BuildValidFlag("beta-dashboard", RequirementType.All, "{\"Value\":20}");
    update.Version = 1;
    await updateUseCase.ExecuteAsync("beta-dashboard", update);

    var entries = await activityUseCase.ExecuteAsync("beta-dashboard");

    Assert.Equal(2, entries.Count);
    Assert.Equal("Updated", entries[0].ActivityType);
    Assert.Equal("Created", entries[1].ActivityType);
  }

  [Fact]
  public async Task ScheduleFeatureFlagChangeUseCase_schedules_change_and_writes_activity_entry()
  {
    using var provider = CreateProvider(new FixedTimeProvider(new DateTimeOffset(2026, 5, 26, 10, 0, 0, TimeSpan.Zero)));
    await using var scope = provider.CreateAsyncScope();
    var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateFeatureFlagUseCase>();
    var scheduleUseCase = scope.ServiceProvider.GetRequiredService<IScheduleFeatureFlagChangeUseCase>();
    var db = scope.ServiceProvider.GetRequiredService<IFeatureManagementContext>();

    await createUseCase.ExecuteAsync(BuildValidFlag("beta-dashboard", RequirementType.Any, "{\"Value\":10}"));

    var scheduleTarget = BuildValidFlag("beta-dashboard", RequirementType.All, "{\"Value\":80}");
    var scheduledAt = new DateTime(2026, 5, 27, 12, 0, 0, DateTimeKind.Utc);

    var updated = await scheduleUseCase.ExecuteAsync("beta-dashboard", scheduleTarget, scheduledAt);

    Assert.Equal(2, updated.Version);
    Assert.Equal(scheduledAt, updated.ScheduledAtUtc);
    Assert.Equal(RequirementType.All, updated.RequirementType);
    Assert.Equal("{\"Value\":80}", Assert.Single(updated.EnabledFor).ParametersJson);

    var activityEntry = await db.FeatureFlagActivityEntries.OrderByDescending(entry => entry.Id).FirstAsync();
    Assert.Equal("Scheduled", activityEntry.ActivityType);
    Assert.Equal("ScheduledAtUtc", activityEntry.ChangeType);
  }

  [Fact]
  public async Task ScheduleFeatureFlagChangeUseCase_throws_for_missing_flag_or_past_schedule()
  {
    using var provider = CreateProvider(new FixedTimeProvider(new DateTimeOffset(2026, 5, 26, 10, 0, 0, TimeSpan.Zero)));
    await using var scope = provider.CreateAsyncScope();
    var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateFeatureFlagUseCase>();
    var scheduleUseCase = scope.ServiceProvider.GetRequiredService<IScheduleFeatureFlagChangeUseCase>();

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
      scheduleUseCase.ExecuteAsync("missing", BuildValidFlag("missing", RequirementType.Any, "{\"Value\":10}"),
        new DateTime(2026, 5, 27, 12, 0, 0, DateTimeKind.Utc)));

    await createUseCase.ExecuteAsync(BuildValidFlag("beta-dashboard", RequirementType.Any, "{\"Value\":10}"));

    await Assert.ThrowsAsync<InvalidOperationException>(() =>
      scheduleUseCase.ExecuteAsync("beta-dashboard", BuildValidFlag("beta-dashboard", RequirementType.Any, "{\"Value\":30}"),
        new DateTime(2026, 5, 26, 9, 0, 0, DateTimeKind.Utc)));
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

