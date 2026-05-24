namespace FeatureManagement.Dashboard.Infrastructure;

internal static class Constants
{
  internal const string PercentageFilterName = "Microsoft.Percentage";
  internal const string TimeWindowFilterName = "Microsoft.TimeWindow";
  internal const string Start = "Start";
  internal const string End = "End";
  internal const string Value = "Value";

  internal static class ErrorMessages
  {
    internal const string RequiredFeatureName = "Feature name is required.";
    internal const string RequiredFilterName = "Filter name is required.";
    internal const string InvalidTimeWindow = $"{TimeWindowFilterName} filter requires valid Start and End where Start is before End.";
    internal const string InvalidPercentage = $"{PercentageFilterName} filter requires Value between 0 and 100.";
    internal const string InvalidJson = "Filter parameters must be valid JSON.";
  }
}