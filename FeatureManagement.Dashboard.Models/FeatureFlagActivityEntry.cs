namespace FeatureManagement.Dashboard.Models;

/// <summary>
/// Represents a human-readable activity log entry for a feature flag.
/// Unlike audit logs which store snapshots for rollback, activity entries show what changed.
/// </summary>
public class FeatureFlagActivityEntry
{
  public long Id { get; set; }
  public string FeatureFlagName { get; set; } = string.Empty;
  public string ActivityType { get; set; } = string.Empty; // "Created", "Updated", "Deleted", "Scheduled", etc.
  public string Description { get; set; } = string.Empty; // Human-readable description
  public string? ChangeType { get; set; } // "EnabledFor", "Owner", "Tags", etc.
  public DateTime ChangedAtUtc { get; set; }
  public string ChangedBy { get; set; } = "system";
}

