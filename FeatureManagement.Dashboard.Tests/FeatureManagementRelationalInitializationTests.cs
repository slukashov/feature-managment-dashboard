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
        "SELECT name FROM sqlite_master WHERE type = 'table' AND name IN ('FeatureFlags', 'FeatureFilters');";

      await using var reader = await command.ExecuteReaderAsync();
      while (await reader.ReadAsync())
      {
        tableNames.Add(reader.GetString(0));
      }
    }

    Assert.Contains("FeatureFlags", tableNames);
    Assert.Contains("FeatureFilters", tableNames);
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
      "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name IN ('FeatureFlags', 'FeatureFilters');";
    var tableCount = (long)(await tablesCommand.ExecuteScalarAsync() ?? 0L);
    Assert.Equal(2L, tableCount);

    await using var historyCommand = host.Connection.CreateCommand();
    historyCommand.CommandText =
      "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = '__EFMigrationsHistory';";
    var historyTableExists = (long)(await historyCommand.ExecuteScalarAsync() ?? 0L);
    Assert.Equal(0L, historyTableExists);
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
      Action<FeatureManagementSchemaOptions>? schemaOptionsAction = null)
    {
      var connection = new SqliteConnection("Data Source=:memory:");
      await connection.OpenAsync();

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


