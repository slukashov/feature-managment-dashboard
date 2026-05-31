namespace FeatureManagement.Dashboard.Infrastructure.Exceptions;

public sealed class FeatureFlagExperimentNotConfiguredException(string featureFlagName)
  : Exception($"Feature flag '{featureFlagName}' does not have an active experiment configured.");

