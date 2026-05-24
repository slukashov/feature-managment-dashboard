using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FeatureManagement.Dashboard.Persistence.Database.Configurations;

internal sealed class FeatureFilterEntityConfiguration : IEntityTypeConfiguration<FeatureFilter>
{
  public void Configure(EntityTypeBuilder<FeatureFilter> builder)
  {
    builder.HasKey(filter => filter.Id);

    builder.Property(filter => filter.Name)
      .IsRequired();

    builder.Property(filter => filter.FeatureFlagName)
      .IsRequired();

    builder.Property(filter => filter.ParametersJson)
      .IsRequired();
  }
}

