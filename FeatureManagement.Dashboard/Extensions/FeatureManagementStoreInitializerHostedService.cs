using FeatureManagement.Dashboard.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FeatureManagement.Dashboard.Extensions;

internal sealed class FeatureManagementStoreInitializerHostedService(
  IServiceProvider services,
  FeatureManagementSchemaOptions schemaOptions) : IHostedService
{
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

