using System.Text.Json;
using FeatureManagement.Dashboard.Models;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;

internal static class FeatureFlagAuditSnapshotSerializer
{
  private static readonly JsonSerializerOptions SerializerOptions = new()
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
  };

  internal static string Serialize(FeatureFlag flag)
  {
    var snapshot = new FeatureFlag
    {
      Name = flag.Name,
      RequirementType = flag.RequirementType,
      Version = flag.Version,
      UpdatedAtUtc = flag.UpdatedAtUtc,
      EnabledFor = flag.EnabledFor
        .Select(filter => new FeatureFilter
        {
          Name = filter.Name,
          ParametersJson = filter.ParametersJson,
          FeatureFlagName = flag.Name
        })
        .ToList()
    };

    return JsonSerializer.Serialize(snapshot, SerializerOptions);
  }

  internal static FeatureFlag Deserialize(string snapshotJson)
  {
    var snapshot = JsonSerializer.Deserialize<FeatureFlag>(snapshotJson, SerializerOptions);
    if (snapshot is null)
      throw new InvalidOperationException("Audit snapshot payload could not be deserialized.");

    snapshot.EnabledFor ??= [];
    foreach (var filter in snapshot.EnabledFor)
    {
      filter.FeatureFlagName = snapshot.Name;
    }

    return snapshot;
  }
}

