using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Models;
using Microsoft.Data.Sqlite;

namespace FeatureManagement.Dashboard.Tests;

public class FeatureManagementRelationalInitializationTests
{
  [Fact]
  public async Task Initialize_feature_management_store_creates_schema_for_relational_provider()
  {
    await using var host = await SqliteRelationalTestHost.CreateAsync();

    var tableNames = new HashSet<string>(StringComparer.Ordinal);

    await using (var command = host.Connection.CreateCommand())
    {
      command.CommandText =
        "SELECT name FROM sqlite_master WHERE type = 'table' AND name IN ('FeatureFlags', 'FeatureFilters', 'FeatureFlagAuditLogs');";

      await using var reader = await command.ExecuteReaderAsync();
      while (await reader.ReadAsync())
      {
        tableNames.Add(reader.GetString(0));
      }
    }

    Assert.Contains("FeatureFlags", tableNames);
    Assert.Contains("FeatureFilters", tableNames);
    Assert.Contains("FeatureFlagAuditLogs", tableNames);
  }

  [Fact]
  public async Task Delete_feature_flag_removes_related_filters_with_relational_constraints()
  {
    await using var host = await SqliteRelationalTestHost.CreateAsync();

    await using (var scope = host.App.Services.CreateAsyncScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<IFeatureManagementContext>();
      db.FeatureFlags.Add(new FeatureFlag
      {
        Name = "beta-dashboard",
        RequirementType = RequirementType.All,
        UpdatedAtUtc = DateTime.UtcNow,
        EnabledFor =
        [
          new FeatureFilter { Name = "Microsoft.TimeWindow", ParametersJson = "{}", FeatureFlagName = "beta-dashboard" },
          new FeatureFilter { Name = "Microsoft.Percentage", ParametersJson = "{\"Value\":25}", FeatureFlagName = "beta-dashboard" }
        ]
      });

      await db.SaveChangesAsync();
    }

    await using (var scope = host.App.Services.CreateAsyncScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<IFeatureManagementContext>();
      var featureFlag = await db.FeatureFlags.SingleAsync(flag => flag.Name == "beta-dashboard");
      db.FeatureFlags.Remove(featureFlag);
      await db.SaveChangesAsync();
    }

    await using (var scope = host.App.Services.CreateAsyncScope())
    {
      var db = scope.ServiceProvider.GetRequiredService<IFeatureManagementContext>();
      Assert.Equal(0, await db.FeatureFlags.CountAsync());
      Assert.Equal(0, await db.FeatureFilters.CountAsync());
    }
  }

  [Fact]
  public async Task Initialize_feature_management_store_applies_sql_script_for_relational_provider()
  {
    await using var host = await SqliteRelationalTestHost.CreateAsync(options =>
    {
      options.SqlScriptProvider = FeatureManagementSqlScriptProvider.Sqlite;
    });

    await using var tablesCommand = host.Connection.CreateCommand();
    tablesCommand.CommandText =
      "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name IN ('FeatureFlags', 'FeatureFilters', 'FeatureFlagAuditLogs');";
    var tableCount = (long)(await tablesCommand.ExecuteScalarAsync() ?? 0L);
    Assert.Equal(3L, tableCount);

    await using var historyCommand = host.Connection.CreateCommand();
    historyCommand.CommandText =
      "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsHistory';";
    var historyTableExists = (long)(await historyCommand.ExecuteScalarAsync() ?? 0L);
    Assert.Equal(0L, historyTableExists);
  }

  [Fact]
  public async Task Initialize_feature_management_store_applies_pending_migration_scripts_for_existing_schema()
  {
    await using var host = await SqliteRelationalTestHost.CreateAsync(
      options => options.SqlScriptProvider = FeatureManagementSqlScriptProvider.Sqlite,
      async connection =>
      {
        await using var seedCommand = connection.CreateCommand();
        seedCommand.CommandText =
          """
          BEGIN TRANSACTION;

          CREATE TABLE IF NOT EXISTS "FeatureFlags"
          (
            "Name" TEXT PRIMARY KEY,
            "RequirementType" INTEGER NOT NULL,
            "Version" INTEGER NOT NULL DEFAULT 1,
            "UpdatedAtUtc" TEXT NOT NULL
          );

          CREATE TABLE IF NOT EXISTS "FeatureFilters"
          (
            "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
            "Name" TEXT NOT NULL,
            "FeatureFlagName" TEXT NOT NULL,
            "ParametersJson" TEXT NOT NULL DEFAULT '{}',
            CONSTRAINT "FK_FeatureFilters_FeatureFlags_FeatureFlagName"
              FOREIGN KEY ("FeatureFlagName") REFERENCES "FeatureFlags"("Name") ON DELETE CASCADE
          );

          CREATE TABLE IF NOT EXISTS "FeatureFlagAuditLogs"
          (
            "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
            "FeatureFlagName" TEXT NOT NULL,
            "Action" INTEGER NOT NULL,
            "SnapshotVersion" INTEGER NOT NULL,
            "SnapshotJson" TEXT NOT NULL,
            "ChangedAtUtc" TEXT NOT NULL,
            "ChangedBy" TEXT NOT NULL
          );

          COMMIT;
          """;
        await seedCommand.ExecuteNonQueryAsync();
      });

    await using var columnCommand = host.Connection.CreateCommand();
    columnCommand.CommandText =
      "SELECT COUNT(1) FROM pragma_table_info('FeatureFlags') WHERE name = 'ScheduledAtUtc';";
    var scheduledColumnExists = (long)(await columnCommand.ExecuteScalarAsync() ?? 0L);
    Assert.Equal(1L, scheduledColumnExists);

    await using var ownerColumnCommand = host.Connection.CreateCommand();
    ownerColumnCommand.CommandText =
      "SELECT COUNT(1) FROM pragma_table_info('FeatureFlags') WHERE name = 'Owner';";
    var ownerColumnExists = (long)(await ownerColumnCommand.ExecuteScalarAsync() ?? 0L);
    Assert.Equal(1L, ownerColumnExists);

    await using var tagsColumnCommand = host.Connection.CreateCommand();
    tagsColumnCommand.CommandText =
      "SELECT COUNT(1) FROM pragma_table_info('FeatureFlags') WHERE name = 'TagsJson';";
    var tagsColumnExists = (long)(await tagsColumnCommand.ExecuteScalarAsync() ?? 0L);
    Assert.Equal(1L, tagsColumnExists);

    await using var tableCommand = host.Connection.CreateCommand();
    tableCommand.CommandText =
      "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = 'FeatureFlagActivityEntries';";
    var activityTableExists = (long)(await tableCommand.ExecuteScalarAsync() ?? 0L);
    Assert.Equal(1L, activityTableExists);

    await using var migrationHistoryCommand = host.Connection.CreateCommand();
    migrationHistoryCommand.CommandText =
      "SELECT COUNT(1) FROM \"FeatureManagementSchemaMigrations\" WHERE \"ScriptName\" = 'migrate_sqlite_add_scheduled_and_activity.sql';";
    var migrationApplied = (long)(await migrationHistoryCommand.ExecuteScalarAsync() ?? 0L);
    Assert.Equal(1L, migrationApplied);
  }

  [Fact]
  public async Task Initialize_feature_management_store_marks_migration_as_applied_when_schema_already_contains_changes()
  {
    await using var host = await SqliteRelationalTestHost.CreateAsync(
      options => options.SqlScriptProvider = FeatureManagementSqlScriptProvider.Sqlite,
      async connection =>
      {
        await using var seedCommand = connection.CreateCommand();
        seedCommand.CommandText =
          """
          BEGIN TRANSACTION;

          CREATE TABLE IF NOT EXISTS "FeatureFlags"
          (
            "Name" TEXT PRIMARY KEY,
            "Owner" TEXT NOT NULL DEFAULT '',
            "TagsJson" TEXT NOT NULL DEFAULT '[]',
            "RequirementType" INTEGER NOT NULL,
            "Version" INTEGER NOT NULL DEFAULT 1,
            "UpdatedAtUtc" TEXT NOT NULL,
            "ScheduledAtUtc" TEXT
          );

          CREATE TABLE IF NOT EXISTS "FeatureFilters"
          (
            "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
            "Name" TEXT NOT NULL,
            "FeatureFlagName" TEXT NOT NULL,
            "ParametersJson" TEXT NOT NULL DEFAULT '{}',
            CONSTRAINT "FK_FeatureFilters_FeatureFlags_FeatureFlagName"
              FOREIGN KEY ("FeatureFlagName") REFERENCES "FeatureFlags"("Name") ON DELETE CASCADE
          );

          CREATE TABLE IF NOT EXISTS "FeatureFlagAuditLogs"
          (
            "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
            "FeatureFlagName" TEXT NOT NULL,
            "Action" INTEGER NOT NULL,
            "SnapshotVersion" INTEGER NOT NULL,
            "SnapshotJson" TEXT NOT NULL,
            "ChangedAtUtc" TEXT NOT NULL,
            "ChangedBy" TEXT NOT NULL
          );

          CREATE TABLE IF NOT EXISTS "FeatureFlagActivityEntries"
          (
            "Id" INTEGER PRIMARY KEY AUTOINCREMENT,
            "FeatureFlagName" TEXT NOT NULL,
            "ActivityType" TEXT NOT NULL,
            "Description" TEXT NOT NULL,
            "ChangeType" TEXT,
            "ChangedAtUtc" TEXT NOT NULL,
            "ChangedBy" TEXT NOT NULL
          );

          COMMIT;
          """;
        await seedCommand.ExecuteNonQueryAsync();
      });

    await using var migrationHistoryCommand = host.Connection.CreateCommand();
    migrationHistoryCommand.CommandText =
      "SELECT COUNT(1) FROM \"FeatureManagementSchemaMigrations\" WHERE \"ScriptName\" = 'migrate_sqlite_add_scheduled_and_activity.sql';";
    var migrationApplied = (long)(await migrationHistoryCommand.ExecuteScalarAsync() ?? 0L);
    Assert.Equal(1L, migrationApplied);
  }

  private sealed class SqliteRelationalTestHost : IAsyncDisposable
  {
    private SqliteRelationalTestHost(WebApplication app, SqliteConnection connection)
    {
      App = app;
      Connection = connection;
    }

    public WebApplication App { get; }
    public SqliteConnection Connection { get; }

    public static async Task<SqliteRelationalTestHost> CreateAsync(
      Action<FeatureManagementSchemaOptions>? schemaOptionsAction = null,
      Func<SqliteConnection, Task>? preInitializeAsync = null)
    {
      var connection = new SqliteConnection("Data Source=:memory:");
      await connection.OpenAsync();

      if (preInitializeAsync is not null)
      {
        await preInitializeAsync(connection);
      }

      var builder = WebApplication.CreateBuilder(new WebApplicationOptions
      {
        EnvironmentName = "Development"
      });

      builder.WebHost.UseTestServer();
      builder.Services.AddFeatureManagementUi(
        options => options.UseSqlite(connection),
        TimeProvider.System,
        schemaOptionsAction);

      var app = builder.Build();
      await app.StartAsync();
      return new SqliteRelationalTestHost(app, connection);
    }

    public async ValueTask DisposeAsync()
    {
      await App.DisposeAsync();
      await Connection.DisposeAsync();
    }
  }
}


