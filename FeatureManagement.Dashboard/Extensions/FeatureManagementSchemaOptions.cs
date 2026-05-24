namespace FeatureManagement.Dashboard.Extensions;

public enum FeatureManagementSqlScriptProvider
{
  Auto,
  Postgres,
  MySql,
  Sqlite
}

public sealed class FeatureManagementSchemaOptions
{
  public FeatureManagementSqlScriptProvider SqlScriptProvider { get; set; } =
    FeatureManagementSqlScriptProvider.Auto;
}



