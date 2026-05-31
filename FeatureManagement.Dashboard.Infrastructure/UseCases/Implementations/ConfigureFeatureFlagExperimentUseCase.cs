using FeatureManagement.Dashboard.Infrastructure.Cache;
using FeatureManagement.Dashboard.Infrastructure.Exceptions;
using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Infrastructure.Providers;
using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;

internal sealed class ConfigureFeatureFlagExperimentUseCase(
  IFeatureManagementContext context,
  FeatureFlagCacheState cacheState,
  ICurrentUserProvider currentUserProvider,
  TimeProvider timeProvider) : IConfigureFeatureFlagExperimentUseCase
{
  public async Task<FeatureFlagExperiment> ExecuteAsync(
    string featureFlagName,
    FeatureFlagExperimentConfiguration configuration,
    CancellationToken cancellationToken = default)
  {
    ValidateConfiguration(configuration);

    var featureFlagExists = await context.FeatureFlags
      .AnyAsync(flag => flag.Name == featureFlagName, cancellationToken);
    if (!featureFlagExists)
      throw new FeatureFlagNotFoundException(featureFlagName);

    var now = timeProvider.GetUtcNow().UtcDateTime;
    var changedBy = currentUserProvider.GetCurrentUserOrSystem();

    var experiment = await context.FeatureFlagExperiments
      .Include(entry => entry.VariantMetrics)
      .FirstOrDefaultAsync(entry => entry.FeatureFlagName == featureFlagName, cancellationToken);

    if (experiment is null)
    {
      experiment = new FeatureFlagExperiment
      {
        FeatureFlagName = featureFlagName,
        CreatedAtUtc = now
      };
      context.FeatureFlagExperiments.Add(experiment);
    }

    var variantsChanged = !string.Equals(experiment.BaselineVariant, configuration.BaselineVariant, StringComparison.Ordinal) ||
                         !string.Equals(experiment.ChallengerVariant, configuration.ChallengerVariant, StringComparison.Ordinal);

    experiment.BaselineVariant = configuration.BaselineVariant.Trim();
    experiment.ChallengerVariant = configuration.ChallengerVariant.Trim();
    experiment.BaselineTrafficPercentage = configuration.BaselineTrafficPercentage;
    experiment.ChallengerTrafficPercentage = configuration.ChallengerTrafficPercentage;
    experiment.ConversionMetricName = configuration.ConversionMetricName.Trim();
    experiment.LatencyMetricName = configuration.LatencyMetricName.Trim();
    experiment.MinimumSampleSize = configuration.MinimumSampleSize;
    experiment.IsActive = configuration.IsActive;
    experiment.UpdatedAtUtc = now;

    if (variantsChanged)
    {
      context.FeatureFlagExperimentVariantMetrics.RemoveRange(experiment.VariantMetrics);
      experiment.VariantMetrics.Clear();
    }

    EnsureMetricRow(experiment, experiment.BaselineVariant);
    EnsureMetricRow(experiment, experiment.ChallengerVariant);

    context.FeatureFlagActivityEntries.Add(new FeatureFlagActivityEntry
    {
      FeatureFlagName = featureFlagName,
      ActivityType = "ExperimentConfigured",
      Description = $"Configured experiment '{experiment.BaselineVariant}' vs '{experiment.ChallengerVariant}' ({experiment.BaselineTrafficPercentage}/{experiment.ChallengerTrafficPercentage}).",
      ChangeType = "Experiment",
      ChangedAtUtc = now,
      ChangedBy = changedBy
    });

    await context.SaveChangesAsync(cancellationToken);
    cacheState.Bump();
    return experiment;
  }

  private static void ValidateConfiguration(FeatureFlagExperimentConfiguration configuration)
  {
    if (string.IsNullOrWhiteSpace(configuration.BaselineVariant))
      throw new ArgumentException("Baseline variant is required.", nameof(configuration));

    if (string.IsNullOrWhiteSpace(configuration.ChallengerVariant))
      throw new ArgumentException("Challenger variant is required.", nameof(configuration));

    if (string.Equals(configuration.BaselineVariant.Trim(), configuration.ChallengerVariant.Trim(), StringComparison.OrdinalIgnoreCase))
      throw new ArgumentException("Baseline and challenger variants must be different.", nameof(configuration));

    if (configuration.BaselineTrafficPercentage < 0 || configuration.ChallengerTrafficPercentage < 0)
      throw new ArgumentException("Traffic percentages cannot be negative.", nameof(configuration));

    if (configuration.BaselineTrafficPercentage + configuration.ChallengerTrafficPercentage != 100)
      throw new ArgumentException("Traffic percentages must total 100.", nameof(configuration));

    if (string.IsNullOrWhiteSpace(configuration.ConversionMetricName) || string.IsNullOrWhiteSpace(configuration.LatencyMetricName))
      throw new ArgumentException("Conversion and latency metric names are required.", nameof(configuration));

    if (configuration.MinimumSampleSize <= 0)
      throw new ArgumentException("Minimum sample size must be positive.", nameof(configuration));
  }

  private static void EnsureMetricRow(FeatureFlagExperiment experiment, string variant)
  {
    if (experiment.VariantMetrics.Any(entry => string.Equals(entry.Variant, variant, StringComparison.OrdinalIgnoreCase)))
      return;

    experiment.VariantMetrics.Add(new FeatureFlagExperimentVariantMetric
    {
      Variant = variant,
      SampleSize = 0,
      ConversionCount = 0,
      ErrorCount = 0,
      TotalLatencyMs = 0
    });
  }
}

