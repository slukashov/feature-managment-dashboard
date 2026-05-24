using FeatureManagement.Dashboard.Infrastructure.Persistence;
using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace FeatureManagement.Dashboard.Persistence.Database;

internal class FeatureManagementContext(DbContextOptions<FeatureManagementContext> options) : DbContext(options),
  IFeatureManagementContext
{
  public DbSet<FeatureFlag> FeatureFlags => Set<FeatureFlag>();
  public DbSet<FeatureFilter> FeatureFilters => Set<FeatureFilter>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(FeatureManagementContext).Assembly);
    base.OnModelCreating(modelBuilder);
  }
}