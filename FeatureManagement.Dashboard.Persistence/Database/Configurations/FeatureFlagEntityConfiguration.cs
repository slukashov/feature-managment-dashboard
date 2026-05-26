using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace FeatureManagement.Dashboard.Persistence.Database.Configurations;

internal sealed class FeatureFlagEntityConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
  public void Configure(EntityTypeBuilder<FeatureFlag> builder)
  {
    var tagsConverter = new ValueConverter<List<string>, string>(
      tags => JsonSerializer.Serialize(tags ?? new List<string>(), (JsonSerializerOptions?)null),
      json => string.IsNullOrWhiteSpace(json)
        ? new List<string>()
        : JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>());

    var tagsComparer = new ValueComparer<List<string>>(
      (left, right) => (left ?? new List<string>()).SequenceEqual(right ?? new List<string>()),
      tags => tags == null ? 0 : tags.Aggregate(0, (hash, value) => HashCode.Combine(hash, value.GetHashCode(StringComparison.Ordinal))),
      tags => tags == null ? new List<string>() : tags.ToList());

    builder.HasKey(featureFlag => featureFlag.Name);

    builder.Property(featureFlag => featureFlag.Owner)
      .IsRequired()
      .HasDefaultValue(string.Empty);

    builder.Property(featureFlag => featureFlag.Tags)
      .HasConversion(tagsConverter)
      .HasColumnName("TagsJson")
      .Metadata.SetValueComparer(tagsComparer);

    builder.Property(featureFlag => featureFlag.Version)
      .IsConcurrencyToken();

    builder.HasMany(featureFlag => featureFlag.EnabledFor)
      .WithOne()
      .HasForeignKey(filter => filter.FeatureFlagName)
      .OnDelete(DeleteBehavior.Cascade);
  }
}

