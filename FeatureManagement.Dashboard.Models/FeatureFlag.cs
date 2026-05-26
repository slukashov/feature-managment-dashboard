using Microsoft.FeatureManagement;

namespace FeatureManagement.Dashboard.Models;

public class FeatureFlag
{
  public required string Name { get; set; }
  public string Owner { get; set; } = string.Empty;
  public List<string> Tags { get; set; } = [];
  public required RequirementType RequirementType { get; set; }
  public required List<FeatureFilter> EnabledFor { get; set; } 
  public  int Version { get; set; } = Constants.DefaultVersion;
  public DateTime UpdatedAtUtc { get; set; }
  public DateTime? ScheduledAtUtc { get; set; }
}