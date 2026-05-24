namespace FeatureManagement.Dashboard.Models;

public record FeatureFilter
{
  public int Id { get; init; }
  public string Name { get; init; } = string.Empty;
  public string FeatureFlagName { get; set; } = string.Empty;
  public string ParametersJson { get; set; } = Constants.DefaultParameters; 
}