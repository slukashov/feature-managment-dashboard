namespace FeatureManagement.Dashboard.Infrastructure;

internal static class Constants
{
  internal const string PercentageFilterName = "Microsoft.Percentage";
  internal const string TimeWindowFilterName = "Microsoft.TimeWindow";
  internal const string TargetingFilterName = "Microsoft.Targeting";
  internal const string Start = "Start";
  internal const string End = "End";
  internal const string Value = "Value";
  internal const string Audience = "Audience";
  internal const string Users = "Users";
  internal const string Groups = "Groups";
  internal const string Name = "Name";
  internal const string RolloutPercentage = "RolloutPercentage";
  internal const string DefaultRolloutPercentage = "DefaultRolloutPercentage";
  internal const string Roles = "Roles";
  internal const string IpRanges = "IpRanges";
  internal const string CustomAttributes = "CustomAttributes";

  internal static class ErrorMessages
  {
    internal const string RequiredFeatureName = "Feature name is required.";
    internal const string InvalidOwner = "Owner must be 200 characters or fewer.";
    internal const string InvalidTag = "Each tag must be non-empty and up to 64 characters.";
    internal const string RequiredFilterName = "Filter name is required.";
    internal const string InvalidTimeWindow = $"{TimeWindowFilterName} filter requires valid Start and End where Start is before End.";
    internal const string InvalidPercentage = $"{PercentageFilterName} filter requires Value between 0 and 100.";
    internal const string InvalidTargeting =
      $"{TargetingFilterName} filter requires a valid Audience with Users, Groups, or DefaultRolloutPercentage between 0 and 100.";
    internal const string InvalidJson = "Filter parameters must be valid JSON.";
  }
}