namespace FeatureManagement.Dashboard.Models;

public class FeatureFlagExperiment
{
  public long Id { get; set; }
  public string FeatureFlagName { get; set; } = string.Empty;
  public string BaselineVariant { get; set; } = "A";
  public string ChallengerVariant { get; set; } = "B";
  public int BaselineTrafficPercentage { get; set; } = 50;
  public int ChallengerTrafficPercentage { get; set; } = 50;
  public string ConversionMetricName { get; set; } = "conversion";
  public string LatencyMetricName { get; set; } = "latency_ms";
  public int MinimumSampleSize { get; set; } = 100;
  public bool IsActive { get; set; } = true;
  public DateTime CreatedAtUtc { get; set; }
  public DateTime UpdatedAtUtc { get; set; }
  public List<FeatureFlagExperimentVariantMetric> VariantMetrics { get; set; } = [];
}

public class FeatureFlagExperimentVariantMetric
{
  public long Id { get; set; }
  public long FeatureFlagExperimentId { get; set; }
  public string Variant { get; set; } = string.Empty;
  public long SampleSize { get; set; }
  public long ConversionCount { get; set; }
  public long ErrorCount { get; set; }
  public double TotalLatencyMs { get; set; }
}

public class FeatureFlagExperimentConfiguration
{
  public string BaselineVariant { get; set; } = "A";
  public string ChallengerVariant { get; set; } = "B";
  public int BaselineTrafficPercentage { get; set; } = 50;
  public int ChallengerTrafficPercentage { get; set; } = 50;
  public string ConversionMetricName { get; set; } = "conversion";
  public string LatencyMetricName { get; set; } = "latency_ms";
  public int MinimumSampleSize { get; set; } = 100;
  public bool IsActive { get; set; } = true;
}

public class FeatureFlagExperimentOutcome
{
  public string Variant { get; set; } = string.Empty;
  public bool Converted { get; set; }
  public bool HasError { get; set; }
  public double LatencyMs { get; set; }
}

public class FeatureFlagExperimentVariantAssignment
{
  public string Variant { get; set; } = string.Empty;
  public int Bucket { get; set; }
}

public enum FeatureFlagExperimentRecommendationStatus
{
  NoData = 0,
  Inconclusive = 1,
  RecommendBaseline = 2,
  RecommendChallenger = 3
}

public class FeatureFlagExperimentVariantSnapshot
{
  public string Variant { get; set; } = string.Empty;
  public long SampleSize { get; set; }
  public long ConversionCount { get; set; }
  public long ErrorCount { get; set; }
  public double ConversionRate { get; set; }
  public double ErrorRate { get; set; }
  public double AverageLatencyMs { get; set; }
  public double Score { get; set; }
}

public class FeatureFlagExperimentRecommendation
{
  public FeatureFlagExperimentRecommendationStatus Status { get; set; }
  public string? RecommendedVariant { get; set; }
  public string Reason { get; set; } = string.Empty;
  public FeatureFlagExperimentVariantSnapshot Baseline { get; set; } = new();
  public FeatureFlagExperimentVariantSnapshot Challenger { get; set; } = new();
}

