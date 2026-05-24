namespace FeatureManagement.Dashboard.Infrastructure.Cache;

internal class FeatureFlagCacheState
{
  private long _version;

  internal long CurrentVersion => Interlocked.Read(ref _version);

  internal void Bump()
  {
    Interlocked.Increment(ref _version);
  }
}