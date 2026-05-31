namespace FeatureManagement.Dashboard.Infrastructure.Exceptions;

public sealed class FeatureFlagExperimentInvalidVariantException(string variant)
  : Exception($"Variant '{variant}' is not part of the configured experiment.");

