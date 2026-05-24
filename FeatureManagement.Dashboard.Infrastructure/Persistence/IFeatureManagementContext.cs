using FeatureManagement.Dashboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace FeatureManagement.Dashboard.Infrastructure.Persistence;

/// <summary>
/// Represents the interface for the feature management context, providing access to
/// feature flags and their associated filters. It defines methods for querying
/// and managing the state of feature management data.
/// </summary>
public interface IFeatureManagementContext
{
  /// <summary>
  /// Represents a collection of feature flag entities stored in the database.
  /// </summary>
  /// <remarks>
  /// This property is used to access and manage feature flags within the persistence layer.
  /// Feature flags are used to control the availability of specific features in an application,
  /// typically toggled on or off for a subset of users or scenarios.
  /// </remarks>
  DbSet<FeatureFlag> FeatureFlags { get; }

  /// <summary>
  /// Represents a collection of feature filter entities stored in the database.
  /// </summary>
  /// <remarks>
  /// This property provides access to and management of feature filters that define
  /// specific conditions or parameters associated with feature flags. Feature filters
  /// are used to determine the conditions under which a feature flag is enabled
  /// for users or scenarios.
  /// </remarks>
  DbSet<FeatureFilter> FeatureFilters { get; }

  /// <summary>
  /// Provides access to database-specific functionality for the feature management context.
  /// </summary>
  /// <remarks>
  /// This property is used to perform operations directly related to the underlying database connection,
  /// such as ensuring the database schema is created or performing database migrations.
  /// </remarks>
  DatabaseFacade Database { get; }

  /// <summary>
  /// Asynchronously saves all changes made in the context to the database.
  /// </summary>
  /// <param name="cancellationToken">
  /// A cancellation token to observe while waiting for the task to complete. The default value is None.
  /// </param>
  /// <returns>
  /// A task that represents the asynchronous save operation. The task result contains the number of state entries written to the database.
  /// </returns>
  Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}