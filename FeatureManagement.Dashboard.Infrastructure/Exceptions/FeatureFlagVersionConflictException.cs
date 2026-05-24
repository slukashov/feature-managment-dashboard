namespace FeatureManagement.Dashboard.Infrastructure.Exceptions;

/// <summary>
/// Represents an exception that is thrown when there is a version conflict
/// while updating a feature flag. This occurs when the version of the
/// feature flag being updated does not match the current version
/// in the system, indicating that the feature flag was modified
/// by another request.
/// </summary>
public sealed class FeatureFlagVersionConflictException(int currentVersion)
  : Exception("The feature flag was modified by another request.")
{
  /// <summary>
  /// Gets the current version of the feature flag in the system.
  /// This property represents the version of the feature flag at the time the
  /// exception was thrown, indicating a conflict due to concurrent updates.
  /// </summary>
  public int CurrentVersion { get; } = currentVersion;
}