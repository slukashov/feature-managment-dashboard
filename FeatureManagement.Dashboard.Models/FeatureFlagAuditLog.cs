namespace FeatureManagement.Dashboard.Models;

public enum FeatureFlagAuditAction
{
  Created = 1,
  Updated = 2,
  Deleted = 3,
  RolledBack = 4
}

public class FeatureFlagAuditLog
{
  public long Id { get; set; }
  public string FeatureFlagName { get; set; } = string.Empty;
  public FeatureFlagAuditAction Action { get; set; }
  public int SnapshotVersion { get; set; }
  public string SnapshotJson { get; set; } = string.Empty;
  public DateTime ChangedAtUtc { get; set; }
  public string ChangedBy { get; set; } = "system";
}

