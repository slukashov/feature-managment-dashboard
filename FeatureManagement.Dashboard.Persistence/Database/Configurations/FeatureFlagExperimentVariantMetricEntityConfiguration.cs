using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FeatureManagement.Dashboard.Persistence.Database.Configurations;

internal sealed class FeatureFlagExperimentVariantMetricEntityConfiguration : IEntityTypeConfiguration<FeatureFlagExperimentVariantMetric>
{
  public void Configure(EntityTypeBuilder<FeatureFlagExperimentVariantMetric> builder)
  {
    builder.HasKey(metric => metric.Id);

    builder.Property(metric => metric.Variant)
      .IsRequired();

    builder.HasIndex(metric => new { metric.FeatureFlagExperimentId, metric.Variant })
      .IsUnique();
  }
}

