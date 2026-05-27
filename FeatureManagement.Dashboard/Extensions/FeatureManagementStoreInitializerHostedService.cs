using FeatureManagement.Dashboard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data.Common;

namespace FeatureManagement.Dashboard.Extensions;

internal sealed class FeatureManagementStoreInitializerHostedService(
  IServiceProvider services,
  FeatureManagementSchemaOptions schemaOptions) : IHostedService
{
  private const string MigrationHistoryTableName = "FeatureManagementSchemaMigrations";

  private static readonly Dictionary<FeatureManagementSqlScriptProvider, string> ScriptFileNames = new()
  {
    [FeatureManagementSqlScriptProvider.Postgres] = "create_feature_management_tables.postgres.sql",
    [FeatureManagementSqlScriptProvider.MySql] = "create_feature_management_tables.mysql.sql",
    [FeatureManagementSqlScriptProvider.Sqlite] = "create_feature_management_tables.sqlite.sql"
  };

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    await using var scope = services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<IFeatureManagementContext>();

    if (!db.Database.IsRelational())
    {
      await db.Database.EnsureCreatedAsync(cancellationToken);
      return;
    }

    await ExecuteSqlScriptAsync(db, cancellationToken);
  }

  public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

  private async Task ExecuteSqlScriptAsync(IFeatureManagementContext db, CancellationToken cancellationToken)
  {
    var provider = ResolveScriptProvider(schemaOptions.SqlScriptProvider, db.Database.ProviderName);
    var scriptFileName = ScriptFileNames[provider];
    var sql = ReadEmbeddedScript(scriptFileName);

    var connection = db.Database.GetDbConnection();
    if (connection.State != System.Data.ConnectionState.Open)
    {
      await connection.OpenAsync(cancellationToken);
    }

    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    await command.ExecuteNonQueryAsync(cancellationToken);

    await EnsureMigrationHistoryTableAsync(connection, provider, cancellationToken);
    await ApplyPendingMigrationsAsync(connection, provider, cancellationToken);
  }

  private static async Task EnsureMigrationHistoryTableAsync(
    DbConnection connection,
    FeatureManagementSqlScriptProvider provider,
    CancellationToken cancellationToken)
  {
    await using var command = connection.CreateCommand();
    command.CommandText = provider switch
    {
      FeatureManagementSqlScriptProvider.Postgres =>
        $"CREATE TABLE IF NOT EXISTS \"{MigrationHistoryTableName}\" (\"ScriptName\" text PRIMARY KEY, \"AppliedAtUtc\" timestamp with time zone NOT NULL)",
      FeatureManagementSqlScriptProvider.MySql =>
        $"CREATE TABLE IF NOT EXISTS `{MigrationHistoryTableName}` (`ScriptName` varchar(255) NOT NULL PRIMARY KEY, `AppliedAtUtc` datetime(6) NOT NULL) ENGINE=InnoDB",
      _ =>
        $"CREATE TABLE IF NOT EXISTS \"{MigrationHistoryTableName}\" (\"ScriptName\" TEXT PRIMARY KEY, \"AppliedAtUtc\" TEXT NOT NULL)"
    };

    await command.ExecuteNonQueryAsync(cancellationToken);
  }

  private static async Task ApplyPendingMigrationsAsync(
    DbConnection connection,
    FeatureManagementSqlScriptProvider provider,
    CancellationToken cancellationToken)
  {
    var migrationScriptNames = GetMigrationScriptNames(provider);
    foreach (var scriptName in migrationScriptNames)
    {
      if (await IsMigrationAppliedAsync(connection, provider, scriptName, cancellationToken))
      {
        continue;
      }

      if (scriptName == "migrate_sqlite_add_scheduled_and_activity.sql" ||
          scriptName == "migrate_postgres_add_scheduled_and_activity.sql" ||
          scriptName == "migrate_mysql_add_scheduled_and_activity.sql")
      {
        var hasScheduledColumn = await HasColumnAsync(connection, provider, "FeatureFlags", "ScheduledAtUtc", cancellationToken);
        var hasActivityTable = await HasTableAsync(connection, provider, "FeatureFlagActivityEntries", cancellationToken);
        if (hasScheduledColumn && hasActivityTable)
        {
          await MarkMigrationAppliedAsync(connection, provider, scriptName, cancellationToken);
          continue;
        }
      }

      if (provider == FeatureManagementSqlScriptProvider.Sqlite &&
          scriptName == "migrate_sqlite_add_scheduled_and_activity.sql")
      {
        await EnsureSqliteScheduledAndActivityColumnsAsync(connection, cancellationToken);
      }

      var sql = ReadEmbeddedScript(scriptName);
      await using (var command = connection.CreateCommand())
      {
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
      }

      await MarkMigrationAppliedAsync(connection, provider, scriptName, cancellationToken);
    }
  }

  private static async Task EnsureSqliteScheduledAndActivityColumnsAsync(
    DbConnection connection,
    CancellationToken cancellationToken)
  {
    if (!await HasColumnAsync(connection, FeatureManagementSqlScriptProvider.Sqlite, "FeatureFlags", "ScheduledAtUtc", cancellationToken))
    {
      await ExecuteNonQueryAsync(connection, "ALTER TABLE \"FeatureFlags\" ADD COLUMN \"ScheduledAtUtc\" TEXT;", cancellationToken);
    }

    if (!await HasColumnAsync(connection, FeatureManagementSqlScriptProvider.Sqlite, "FeatureFlags", "Owner", cancellationToken))
    {
      await ExecuteNonQueryAsync(connection, "ALTER TABLE \"FeatureFlags\" ADD COLUMN \"Owner\" TEXT NOT NULL DEFAULT '';", cancellationToken);
    }

    if (!await HasColumnAsync(connection, FeatureManagementSqlScriptProvider.Sqlite, "FeatureFlags", "TagsJson", cancellationToken))
    {
      await ExecuteNonQueryAsync(connection, "ALTER TABLE \"FeatureFlags\" ADD COLUMN \"TagsJson\" TEXT NOT NULL DEFAULT '[]';", cancellationToken);
    }
  }

  private static IReadOnlyList<string> GetMigrationScriptNames(FeatureManagementSqlScriptProvider provider)
  {
    var prefix = provider switch
    {
      FeatureManagementSqlScriptProvider.Postgres => "migrate_postgres_",
      FeatureManagementSqlScriptProvider.MySql => "migrate_mysql_",
      _ => "migrate_sqlite_"
    };

    var assembly = typeof(FeatureManagementStoreInitializerHostedService).Assembly;
    return assembly
      .GetManifestResourceNames()
      .Where(name => name.Contains(prefix, StringComparison.OrdinalIgnoreCase) && name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
      .Select(name =>
      {
        var token = "Database.Scripts.";
        var index = name.LastIndexOf(token, StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? name[(index + token.Length)..] : name;
      })
      .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
      .ToList();
  }

  private static async Task<bool> IsMigrationAppliedAsync(
    DbConnection connection,
    FeatureManagementSqlScriptProvider provider,
    string scriptName,
    CancellationToken cancellationToken)
  {
    await using var command = connection.CreateCommand();
    command.CommandText = provider switch
    {
      FeatureManagementSqlScriptProvider.Postgres =>
        $"SELECT COUNT(1) FROM \"{MigrationHistoryTableName}\" WHERE \"ScriptName\" = @script",
      FeatureManagementSqlScriptProvider.MySql =>
        $"SELECT COUNT(1) FROM `{MigrationHistoryTableName}` WHERE `ScriptName` = @script",
      _ =>
        $"SELECT COUNT(1) FROM \"{MigrationHistoryTableName}\" WHERE \"ScriptName\" = @script"
    };

    var parameter = command.CreateParameter();
    parameter.ParameterName = "@script";
    parameter.Value = scriptName;
    command.Parameters.Add(parameter);

    var count = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
    return count > 0;
  }

  private static async Task MarkMigrationAppliedAsync(
    DbConnection connection,
    FeatureManagementSqlScriptProvider provider,
    string scriptName,
    CancellationToken cancellationToken)
  {
    await using var command = connection.CreateCommand();
    command.CommandText = provider switch
    {
      FeatureManagementSqlScriptProvider.Postgres =>
        $"INSERT INTO \"{MigrationHistoryTableName}\" (\"ScriptName\", \"AppliedAtUtc\") VALUES (@script, @appliedAt)",
      FeatureManagementSqlScriptProvider.MySql =>
        $"INSERT INTO `{MigrationHistoryTableName}` (`ScriptName`, `AppliedAtUtc`) VALUES (@script, @appliedAt)",
      _ =>
        $"INSERT INTO \"{MigrationHistoryTableName}\" (\"ScriptName\", \"AppliedAtUtc\") VALUES (@script, @appliedAt)"
    };

    var scriptParameter = command.CreateParameter();
    scriptParameter.ParameterName = "@script";
    scriptParameter.Value = scriptName;
    command.Parameters.Add(scriptParameter);

    var appliedAtParameter = command.CreateParameter();
    appliedAtParameter.ParameterName = "@appliedAt";
    appliedAtParameter.Value = provider == FeatureManagementSqlScriptProvider.Sqlite
      ? DateTime.UtcNow.ToString("O")
      : DateTime.UtcNow;
    command.Parameters.Add(appliedAtParameter);

    await command.ExecuteNonQueryAsync(cancellationToken);
  }

  private static async Task ExecuteNonQueryAsync(
    DbConnection connection,
    string sql,
    CancellationToken cancellationToken)
  {
    await using var command = connection.CreateCommand();
    command.CommandText = sql;
    await command.ExecuteNonQueryAsync(cancellationToken);
  }

  private static async Task<bool> HasTableAsync(
    DbConnection connection,
    FeatureManagementSqlScriptProvider provider,
    string tableName,
    CancellationToken cancellationToken)
  {
    await using var command = connection.CreateCommand();
    command.CommandText = provider switch
    {
      FeatureManagementSqlScriptProvider.Postgres =>
        "SELECT COUNT(1) FROM information_schema.tables WHERE table_name = @table",
      FeatureManagementSqlScriptProvider.MySql =>
        "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = @table",
      _ =>
        "SELECT COUNT(1) FROM sqlite_master WHERE type = 'table' AND name = @table"
    };

    var parameter = command.CreateParameter();
    parameter.ParameterName = "@table";
    parameter.Value = tableName;
    command.Parameters.Add(parameter);

    var count = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
    return count > 0;
  }

  private static async Task<bool> HasColumnAsync(
    DbConnection connection,
    FeatureManagementSqlScriptProvider provider,
    string tableName,
    string columnName,
    CancellationToken cancellationToken)
  {
    await using var command = connection.CreateCommand();
    command.CommandText = provider switch
    {
      FeatureManagementSqlScriptProvider.Postgres =>
        "SELECT COUNT(1) FROM information_schema.columns WHERE table_name = @table AND column_name = @column",
      FeatureManagementSqlScriptProvider.MySql =>
        "SELECT COUNT(1) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = @table AND column_name = @column",
      _ =>
        $"SELECT COUNT(1) FROM pragma_table_info('{tableName}') WHERE name = @column"
    };

    if (provider != FeatureManagementSqlScriptProvider.Sqlite)
    {
      var tableParameter = command.CreateParameter();
      tableParameter.ParameterName = "@table";
      tableParameter.Value = tableName;
      command.Parameters.Add(tableParameter);
    }

    var columnParameter = command.CreateParameter();
    columnParameter.ParameterName = "@column";
    columnParameter.Value = columnName;
    command.Parameters.Add(columnParameter);

    var count = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken) ?? 0L);
    return count > 0;
  }

  private static FeatureManagementSqlScriptProvider ResolveScriptProvider(
    FeatureManagementSqlScriptProvider requestedProvider,
    string? databaseProviderName)
  {
    if (requestedProvider != FeatureManagementSqlScriptProvider.Auto)
    {
      return requestedProvider;
    }

    return databaseProviderName switch
    {
      "Npgsql.EntityFrameworkCore.PostgreSQL" => FeatureManagementSqlScriptProvider.Postgres,
      "Pomelo.EntityFrameworkCore.MySql" => FeatureManagementSqlScriptProvider.MySql,
      "MySql.EntityFrameworkCore" => FeatureManagementSqlScriptProvider.MySql,
      "Microsoft.EntityFrameworkCore.Sqlite" => FeatureManagementSqlScriptProvider.Sqlite,
      _ => throw new NotSupportedException(
        $"No SQL script mapping configured for provider '{databaseProviderName ?? "<null>"}'.")
    };
  }

  private static string ReadEmbeddedScript(string scriptFileName)
  {
    var assembly = typeof(FeatureManagementStoreInitializerHostedService).Assembly;
    var resourceName = assembly
      .GetManifestResourceNames()
      .FirstOrDefault(name => name.EndsWith(scriptFileName, StringComparison.OrdinalIgnoreCase));

    if (resourceName is null)
    {
      throw new InvalidOperationException($"Embedded SQL script '{scriptFileName}' was not found in assembly resources.");
    }

    using var stream = assembly.GetManifestResourceStream(resourceName)
      ?? throw new InvalidOperationException($"Unable to open embedded SQL script resource '{resourceName}'.");
    using var reader = new StreamReader(stream);
    return reader.ReadToEnd();
  }
}

