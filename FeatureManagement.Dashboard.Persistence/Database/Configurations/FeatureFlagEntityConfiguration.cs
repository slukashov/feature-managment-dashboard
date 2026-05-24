using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FeatureManagement.Dashboard.Persistence.Database.Configurations;

internal sealed class FeatureFlagEntityConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
  public void Configure(EntityTypeBuilder<FeatureFlag> builder)
  {
    builder.HasKey(featureFlag => featureFlag.Name);

    builder.HasMany(featureFlag => featureFlag.EnabledFor)
      .WithOne()
      .HasForeignKey(filter => filter.FeatureFlagName)
      .OnDelete(DeleteBehavior.Cascade);
  }
}

