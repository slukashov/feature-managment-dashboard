using System.Text.Json;
using FeatureManagement.Dashboard.Models;

namespace FeatureManagement.Dashboard.Infrastructure.Validators;

internal static class FeatureFilterValidatorHelper
{
  internal static bool TryParseParameters(string? parametersJson)
  {
    if (string.IsNullOrWhiteSpace(parametersJson) || parametersJson == Models.Constants.DefaultParameters)
      return true;

    try
    {
      _ = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(parametersJson);
      return true;
    }
    catch (JsonException)
    {
      return false;
    }
  }

  internal static bool HasValidPercentage(FeatureFilter filter)
  {
    if (!TryParseParameters(filter.ParametersJson, out var parameters))
      return false;

    return TryGetInt(parameters, Constants.Value, out var percentage) && percentage is >= 0 and <= 100;
  }

  internal static bool HasValidTimeWindow(FeatureFilter filter)
  {
    if (!TryParseParameters(filter.ParametersJson, out var parameters))
      return false;

    return TryGetString(parameters, Constants.Start, out var startRaw) &&
           TryGetString(parameters, Constants.End, out var endRaw) &&
           DateTime.TryParse(startRaw, out var start) &&
           DateTime.TryParse(endRaw, out var end) &&
           start < end;
  }

  private static bool TryParseParameters(string? parametersJson, out Dictionary<string, JsonElement> parameters)
  {
    parameters = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);

    if (string.IsNullOrWhiteSpace(parametersJson) || parametersJson == Models.Constants.DefaultParameters)
      return true;

    try
    {
      parameters = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(parametersJson) ??
                   new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
      return true;
    }
    catch (JsonException)
    {
      return false;
    }
  }

  private static bool TryGetInt(Dictionary<string, JsonElement> parameters, string key, out int value)
  {
    value = 0;
    if (!parameters.TryGetValue(key, out var element)) 
      return false;

    return element.ValueKind switch
    {
      JsonValueKind.Number => element.TryGetInt32(out value),
      JsonValueKind.String => int.TryParse(element.GetString(), out value),
      _ => false
    };
  }

  private static bool TryGetString(Dictionary<string, JsonElement> parameters, string key, out string value)
  {
    value = string.Empty;
    if (!parameters.TryGetValue(key, out var element)) 
      return false;

    if (element.ValueKind != JsonValueKind.String)
    {
      return false;
    }

    value = element.GetString() ?? string.Empty;
    return !string.IsNullOrWhiteSpace(value);
  }
}