namespace FeatureManagement.Dashboard.Extensions;

internal static class FeatureFlagEtagHelper
{
  internal static string Format(int version) => $"\"v{version}\"";

  internal static bool TryParseIfMatch(string? ifMatchHeader, out int version)
  {
    version = 0;

    if (string.IsNullOrWhiteSpace(ifMatchHeader))
      return false;

    var value = ifMatchHeader.Trim();
    if (value.StartsWith("W/", StringComparison.OrdinalIgnoreCase))
    {
      value = value[2..].Trim();
    }

    if (value.Length < 4 || value[0] != '"' || value[^1] != '"')
      return false;

    var token = value[1..^1];
    if (!token.StartsWith("v", StringComparison.OrdinalIgnoreCase))
      return false;

    return int.TryParse(token[1..], out version);
  }
}

