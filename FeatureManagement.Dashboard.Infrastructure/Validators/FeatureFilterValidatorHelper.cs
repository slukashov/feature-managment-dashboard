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

  internal static bool HasValidTargeting(FeatureFilter filter)
  {
    if (!TryParseParameters(filter.ParametersJson, out var parameters))
      return false;

    if (!parameters.TryGetValue(Constants.Audience, out var audienceElement) ||
        audienceElement.ValueKind != JsonValueKind.Object)
    {
      return false;
    }

    var hasUsers = HasValidUsers(audienceElement);
    var hasGroups = HasValidGroups(audienceElement);
    var hasDefaultRollout = HasValidDefaultRollout(audienceElement, out _);
    var hasRoles = HasValidRoles(audienceElement);
    var hasIpRanges = HasValidIpRanges(audienceElement);
    var hasCustomAttributes = HasValidCustomAttributes(audienceElement);

    return hasUsers || hasGroups || hasDefaultRollout || hasRoles || hasIpRanges || hasCustomAttributes;
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

  private static bool HasValidUsers(JsonElement audienceElement)
  {
    if (!TryGetPropertyCaseInsensitive(audienceElement, Constants.Users, out var usersElement) ||
        usersElement.ValueKind != JsonValueKind.Array)
    {
      return false;
    }

    var anyUsers = false;
    foreach (var user in usersElement.EnumerateArray())
    {
      if (user.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(user.GetString()))
        return false;

      anyUsers = true;
    }

    return anyUsers;
  }

  private static bool HasValidGroups(JsonElement audienceElement)
  {
    if (!TryGetPropertyCaseInsensitive(audienceElement, Constants.Groups, out var groupsElement) ||
        groupsElement.ValueKind != JsonValueKind.Array)
    {
      return false;
    }

    var anyGroups = false;
    foreach (var group in groupsElement.EnumerateArray())
    {
      if (group.ValueKind != JsonValueKind.Object)
        return false;

      if (!TryGetPropertyCaseInsensitive(group, Constants.Name, out var nameElement) ||
          nameElement.ValueKind != JsonValueKind.String ||
          string.IsNullOrWhiteSpace(nameElement.GetString()))
      {
        return false;
      }

      if (!TryGetPropertyCaseInsensitive(group, Constants.RolloutPercentage, out var rolloutElement) ||
          !TryGetInt(rolloutElement, out var rolloutPercentage) ||
          rolloutPercentage is < 0 or > 100)
      {
        return false;
      }

      anyGroups = true;
    }

    return anyGroups;
  }

  private static bool HasValidDefaultRollout(JsonElement audienceElement, out int value)
  {
    value = 0;
    if (!TryGetPropertyCaseInsensitive(audienceElement, Constants.DefaultRolloutPercentage, out var defaultRolloutElement))
      return false;

    return TryGetInt(defaultRolloutElement, out value) && value is >= 0 and <= 100;
  }

  private static bool HasValidRoles(JsonElement audienceElement)
  {
    if (!TryGetPropertyCaseInsensitive(audienceElement, Constants.Roles, out var rolesElement) ||
        rolesElement.ValueKind != JsonValueKind.Array)
    {
      return false;
    }

    var anyRoles = false;
    foreach (var role in rolesElement.EnumerateArray())
    {
      if (role.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(role.GetString()))
        return false;

      anyRoles = true;
    }

    return anyRoles;
  }

  private static bool HasValidIpRanges(JsonElement audienceElement)
  {
    if (!TryGetPropertyCaseInsensitive(audienceElement, Constants.IpRanges, out var ipElement) ||
        ipElement.ValueKind != JsonValueKind.Array)
    {
      return false;
    }

    var anyIps = false;
    foreach (var ip in ipElement.EnumerateArray())
    {
      if (ip.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(ip.GetString()))
        return false;

      anyIps = true;
    }

    return anyIps;
  }

  private static bool HasValidCustomAttributes(JsonElement audienceElement)
  {
    if (!TryGetPropertyCaseInsensitive(audienceElement, Constants.CustomAttributes, out var attrsElement) ||
        attrsElement.ValueKind != JsonValueKind.Object)
    {
      return false;
    }

    var hasAnyAttribute = false;
    foreach (var property in attrsElement.EnumerateObject())
    {
      if (string.IsNullOrWhiteSpace(property.Name))
        return false;

      if (property.Value.ValueKind is not (JsonValueKind.String or JsonValueKind.Array))
        return false;

      hasAnyAttribute = true;
    }

    return hasAnyAttribute;
  }

  private static bool TryGetPropertyCaseInsensitive(JsonElement obj, string key, out JsonElement value)
  {
    foreach (var property in obj.EnumerateObject())
    {
      if (string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase))
      {
        value = property.Value;
        return true;
      }
    }

    value = default;
    return false;
  }

  private static bool TryGetInt(JsonElement element, out int value)
  {
    value = 0;

    return element.ValueKind switch
    {
      JsonValueKind.Number => element.TryGetInt32(out value),
      JsonValueKind.String => int.TryParse(element.GetString(), out value),
      _ => false
    };
  }
}