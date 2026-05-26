using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FeatureManagement.Dashboard.Persistence.Database.Configurations;

internal sealed class FeatureFlagAuditLogEntityConfiguration : IEntityTypeConfiguration<FeatureFlagAuditLog>
{
  public void Configure(EntityTypeBuilder<FeatureFlagAuditLog> builder)
  {
    builder.HasKey(entry => entry.Id);

    builder.Property(entry => entry.FeatureFlagName)
      .IsRequired();

    builder.Property(entry => entry.SnapshotJson)
      .IsRequired();

    builder.Property(entry => entry.ChangedBy)
      .IsRequired();

    builder.HasIndex(entry => new { entry.FeatureFlagName, entry.Id });
  }
}

