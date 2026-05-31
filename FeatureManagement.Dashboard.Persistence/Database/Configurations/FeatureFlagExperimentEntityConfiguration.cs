using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FeatureManagement.Dashboard.Persistence.Database.Configurations;

internal sealed class FeatureFlagExperimentEntityConfiguration : IEntityTypeConfiguration<FeatureFlagExperiment>
{
  public void Configure(EntityTypeBuilder<FeatureFlagExperiment> builder)
  {
    builder.HasKey(experiment => experiment.Id);

    builder.Property(experiment => experiment.FeatureFlagName)
      .IsRequired();

    builder.Property(experiment => experiment.BaselineVariant)
      .IsRequired();

    builder.Property(experiment => experiment.ChallengerVariant)
      .IsRequired();

    builder.Property(experiment => experiment.ConversionMetricName)
      .IsRequired();

    builder.Property(experiment => experiment.LatencyMetricName)
      .IsRequired();

    builder.HasIndex(experiment => experiment.FeatureFlagName)
      .IsUnique();

    builder.HasMany(experiment => experiment.VariantMetrics)
      .WithOne()
      .HasForeignKey(metric => metric.FeatureFlagExperimentId)
      .OnDelete(DeleteBehavior.Cascade);
  }
}

