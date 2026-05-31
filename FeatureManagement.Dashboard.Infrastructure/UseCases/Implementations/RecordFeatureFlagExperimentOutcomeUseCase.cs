using FeatureManagement.Dashboard.Infrastructure.Exceptions;
using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Infrastructure.Providers;
using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;

internal sealed class RecordFeatureFlagExperimentOutcomeUseCase(
  IFeatureManagementContext context,
  ICurrentUserProvider currentUserProvider,
  TimeProvider timeProvider) : IRecordFeatureFlagExperimentOutcomeUseCase
{
  public async Task ExecuteAsync(
    string featureFlagName,
    FeatureFlagExperimentOutcome outcome,
    CancellationToken cancellationToken = default)
  {
    if (string.IsNullOrWhiteSpace(outcome.Variant))
      throw new ArgumentException("Variant is required.", nameof(outcome));

    if (outcome.LatencyMs < 0)
      throw new ArgumentException("Latency cannot be negative.", nameof(outcome));

    var experiment = await context.FeatureFlagExperiments
      .Include(entry => entry.VariantMetrics)
      .FirstOrDefaultAsync(entry => entry.FeatureFlagName == featureFlagName && entry.IsActive, cancellationToken);

    if (experiment is null)
      throw new FeatureFlagExperimentNotConfiguredException(featureFlagName);

    var metric = experiment.VariantMetrics.FirstOrDefault(entry =>
      string.Equals(entry.Variant, outcome.Variant.Trim(), StringComparison.OrdinalIgnoreCase));

    if (metric is null)
      throw new FeatureFlagExperimentInvalidVariantException(outcome.Variant);

    metric.SampleSize++;
    if (outcome.Converted)
      metric.ConversionCount++;
    if (outcome.HasError)
      metric.ErrorCount++;
    metric.TotalLatencyMs += outcome.LatencyMs;

    experiment.UpdatedAtUtc = timeProvider.GetUtcNow().UtcDateTime;

    context.FeatureFlagActivityEntries.Add(new FeatureFlagActivityEntry
    {
      FeatureFlagName = featureFlagName,
      ActivityType = "ExperimentOutcomeRecorded",
      Description = $"Recorded outcome for variant '{metric.Variant}'.",
      ChangeType = "ExperimentMetrics",
      ChangedAtUtc = experiment.UpdatedAtUtc,
      ChangedBy = currentUserProvider.GetCurrentUserOrSystem()
    });

    await context.SaveChangesAsync(cancellationToken);
  }
}

