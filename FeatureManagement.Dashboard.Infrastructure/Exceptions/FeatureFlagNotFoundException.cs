namespace FeatureManagement.Dashboard.Infrastructure.Exceptions;

/// <summary>
/// This exception is thrown when a feature flag is not found in the system.
/// </summary>
public sealed class FeatureFlagNotFoundException(string name)
  : Exception($"Feature flag '{name}' was not found.");