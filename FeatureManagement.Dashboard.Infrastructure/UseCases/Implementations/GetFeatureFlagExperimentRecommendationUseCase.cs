using FeatureManagement.Dashboard.Infrastructure.Exceptions;
using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace FeatureManagement.Dashboard.Infrastructure.UseCases.Implementations;

internal sealed class GetFeatureFlagExperimentRecommendationUseCase(IFeatureManagementContext context)
  : IGetFeatureFlagExperimentRecommendationUseCase
{
  public async Task<FeatureFlagExperimentRecommendation> ExecuteAsync(string featureFlagName, CancellationToken cancellationToken = default)
  {
    var experiment = await context.FeatureFlagExperiments
      .AsNoTracking()
      .Include(entry => entry.VariantMetrics)
      .FirstOrDefaultAsync(entry => entry.FeatureFlagName == featureFlagName && entry.IsActive, cancellationToken);

    if (experiment is null)
      throw new FeatureFlagExperimentNotConfiguredException(featureFlagName);

    var baselineMetric = experiment.VariantMetrics.FirstOrDefault(entry =>
      string.Equals(entry.Variant, experiment.BaselineVariant, StringComparison.OrdinalIgnoreCase));
    var challengerMetric = experiment.VariantMetrics.FirstOrDefault(entry =>
      string.Equals(entry.Variant, experiment.ChallengerVariant, StringComparison.OrdinalIgnoreCase));

    var baseline = BuildSnapshot(experiment.BaselineVariant, baselineMetric);
    var challenger = BuildSnapshot(experiment.ChallengerVariant, challengerMetric);

    if (baseline.SampleSize == 0 && challenger.SampleSize == 0)
    {
      return new FeatureFlagExperimentRecommendation
      {
        Status = FeatureFlagExperimentRecommendationStatus.NoData,
        Reason = "No outcome samples were recorded yet.",
        Baseline = baseline,
        Challenger = challenger
      };
    }

    if (baseline.SampleSize < experiment.MinimumSampleSize || challenger.SampleSize < experiment.MinimumSampleSize)
    {
      return new FeatureFlagExperimentRecommendation
      {
        Status = FeatureFlagExperimentRecommendationStatus.Inconclusive,
        Reason = $"Minimum sample size ({experiment.MinimumSampleSize}) has not been reached for both variants.",
        Baseline = baseline,
        Challenger = challenger
      };
    }

    const double minimumScoreDelta = 0.01;
    var delta = challenger.Score - baseline.Score;
    if (Math.Abs(delta) < minimumScoreDelta)
    {
      return new FeatureFlagExperimentRecommendation
      {
        Status = FeatureFlagExperimentRecommendationStatus.Inconclusive,
        Reason = "Variant performance is too close to call.",
        Baseline = baseline,
        Challenger = challenger
      };
    }

    var recommendChallenger = delta > 0;
    return new FeatureFlagExperimentRecommendation
    {
      Status = recommendChallenger
        ? FeatureFlagExperimentRecommendationStatus.RecommendChallenger
        : FeatureFlagExperimentRecommendationStatus.RecommendBaseline,
      RecommendedVariant = recommendChallenger ? challenger.Variant : baseline.Variant,
      Reason = recommendChallenger
        ? $"Challenger outperformed baseline by score delta {delta:F4}."
        : $"Baseline outperformed challenger by score delta {Math.Abs(delta):F4}.",
      Baseline = baseline,
      Challenger = challenger
    };
  }

  private static FeatureFlagExperimentVariantSnapshot BuildSnapshot(string variant, FeatureFlagExperimentVariantMetric? metric)
  {
    var sampleSize = metric?.SampleSize ?? 0;
    var conversionCount = metric?.ConversionCount ?? 0;
    var errorCount = metric?.ErrorCount ?? 0;
    var totalLatencyMs = metric?.TotalLatencyMs ?? 0;

    var conversionRate = sampleSize == 0 ? 0 : (double)conversionCount / sampleSize;
    var errorRate = sampleSize == 0 ? 0 : (double)errorCount / sampleSize;
    var averageLatencyMs = sampleSize == 0 ? 0 : totalLatencyMs / sampleSize;

    return new FeatureFlagExperimentVariantSnapshot
    {
      Variant = variant,
      SampleSize = sampleSize,
      ConversionCount = conversionCount,
      ErrorCount = errorCount,
      ConversionRate = conversionRate,
      ErrorRate = errorRate,
      AverageLatencyMs = averageLatencyMs,
      Score = CalculateScore(conversionRate, errorRate, averageLatencyMs)
    };
  }

  private static double CalculateScore(double conversionRate, double errorRate, double averageLatencyMs)
    => conversionRate - (errorRate * 0.5) - (averageLatencyMs / 10000d);
}

