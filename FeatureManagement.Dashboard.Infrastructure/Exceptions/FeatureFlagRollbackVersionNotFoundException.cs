namespace FeatureManagement.Dashboard.Infrastructure.Exceptions;

public sealed class FeatureFlagRollbackVersionNotFoundException(string featureName, int targetVersion)
  : Exception($"Rollback target version '{targetVersion}' was not found for feature '{featureName}'.")
{
}

