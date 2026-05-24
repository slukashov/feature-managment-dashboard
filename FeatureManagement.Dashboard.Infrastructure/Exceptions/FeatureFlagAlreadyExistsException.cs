namespace FeatureManagement.Dashboard.Infrastructure.Exceptions;

/// <summary>
/// Represents an exception that is thrown when attempting to create a feature flag with a name
/// that already exists within the system.
/// </summary>
public sealed class FeatureFlagAlreadyExistsException(string name)
  : Exception($"A feature flag with name '{name}' already exists.");