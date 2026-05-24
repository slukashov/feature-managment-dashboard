using System.Text;
using FeatureManagement.Dashboard.Infrastructure.Cache;
using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace FeatureManagement.Dashboard.Infrastructure.Providers;

internal class DatabaseFeatureDefinitionProvider(
  IServiceScopeFactory scopeFactory,
  IMemoryCache cache,
  FeatureFlagCacheState cacheState)
  : IFeatureDefinitionProvider
{
  private const string CacheKeyPrefix = "feature-flags:all:v";

  public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
  {
    var flags = await GetAllFlagsCachedAsync();

    foreach (var flag in flags)
    {
      yield return MapToDefinition(flag);
    }
  }

  public async Task<FeatureDefinition?> GetFeatureDefinitionAsync(string featureName)
  {
    var flags = await GetAllFlagsCachedAsync();
    var flag = flags.FirstOrDefault(featureFlag => featureFlag.Name == featureName);

    return flag == null ? null : MapToDefinition(flag);
  }

  private async Task<List<FeatureFlag>> GetAllFlagsCachedAsync()
  {
    var currentVersion = cacheState.CurrentVersion;
    var cacheKey = BuildCacheKey(currentVersion);
    if (cache.TryGetValue(cacheKey, out List<FeatureFlag>? cachedFlags) && cachedFlags is not null)
    {
      RemovePreviousVersionCacheEntry(currentVersion);
      return cachedFlags;
    }

    await using var scope = scopeFactory.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<IFeatureManagementContext>();

    var flags = await dbContext.FeatureFlags
      .Include(featureFlag => featureFlag.EnabledFor)
      .AsNoTracking()
      .ToListAsync();

    cache.Set(cacheKey, flags);
    RemovePreviousVersionCacheEntry(currentVersion);
    return flags;
  }

  private static string BuildCacheKey(long version)
    => $"{CacheKeyPrefix}{version}";

  private void RemovePreviousVersionCacheEntry(long currentVersion)
  {
    if (currentVersion <= 0)
      return;

    cache.Remove(BuildCacheKey(currentVersion - 1));
  }

  private static FeatureDefinition MapToDefinition(FeatureFlag flag)
  {
    var enabledForList = new List<FeatureFilterConfiguration>();

    foreach (var filter in flag.EnabledFor)
    {
      IConfiguration parameters = new ConfigurationBuilder().Build();
        
      if (!string.IsNullOrWhiteSpace(filter.ParametersJson) && filter.ParametersJson != "{}")
      {
        var bytes = Encoding.UTF8.GetBytes(filter.ParametersJson);
        parameters = new ConfigurationBuilder().AddJsonStream(new MemoryStream(bytes)).Build();
      }

      enabledForList.Add(new FeatureFilterConfiguration
      {
        Name = filter.Name,
        Parameters = parameters
      });
    }

    return new FeatureDefinition
    {
      Name = flag.Name,
      RequirementType = flag.RequirementType,
      EnabledFor = enabledForList
    };
  }
}