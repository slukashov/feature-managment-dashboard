# FeatureManagement.Dashboard

`FeatureManagement.Dashboard` provides a database-backed feature flag dashboard for ASP.NET Core with an embedded React UI, minimal API endpoints, and integration with `Microsoft.FeatureManagement`.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)

## Overview

The solution is organized into layered projects:

- `FeatureManagement.Dashboard.Models` - domain models and shared constants
- `FeatureManagement.Dashboard.Infrastructure` - validators, use cases, cache state, feature definition provider
- `FeatureManagement.Dashboard.Persistence` - EF Core context and entity configurations
- `FeatureManagement.Dashboard` - public package surface (extensions, endpoint wiring, UI hosting)
- `FeatureManagement.Dashboard.Tests` - unit and integration tests
- `FeatureManagement.Dashboard/client-app` - React dashboard UI

Authorization is host-owned via ASP.NET Core `IAuthorizationRequirement`.

## Open Source Project Info

- License: [MIT](./LICENSE)
- Changelog: [CHANGELOG.md](./CHANGELOG.md)
- Contributing guide: [CONTRIBUTING.md](./CONTRIBUTING.md)
- Security policy: [SECURITY.md](./SECURITY.md)
- Community standards: [CODE_OF_CONDUCT.md](./CODE_OF_CONDUCT.md)

## Key Features

- CRUD operations for feature flags
- Rule-based targeting (`AlwaysOn`, `Microsoft.Percentage`, `Microsoft.TimeWindow`, `Microsoft.Targeting`, custom JSON)
- Advanced targeting inside `Microsoft.Targeting` (`Users`, `Groups`, `Roles`, `IpRanges`, `CustomAttributes`, `DefaultRolloutPercentage`)
- Owner and tags metadata on each feature flag
- Search and filtering by name, owner, and tag
- Feature definition provider backed by database + memory cache
- Version-aware updates for optimistic concurrency
- Audit log for create/update/delete/rollback operations
- Activity feed with human-readable change entries
- Rollback to any previously audited version
- Scheduled rollouts for future-dated feature changes
- Embedded UI served from the host application

## Install

```bash
dotnet add package FeatureManagement.Dashboard
```

## Requirements

- .NET 10
- ASP.NET Core host app
- `Microsoft.FeatureManagement.AspNetCore`
- Node.js 20+ (only when developing/building the embedded client locally)

## Quick Start

### 1) Register services

```csharp
using FeatureManagement.Dashboard.Extensions;
using Microsoft.EntityFrameworkCore;

builder.Services.AddFeatureManagementUi(
  options => options.UseNpgsql(builder.Configuration.GetConnectionString("FeatureFlagsDb")!),
  TimeProvider.System,
  schema =>
  {
    // Auto maps provider -> embedded SQL script.
    schema.SqlScriptProvider = FeatureManagementSqlScriptProvider.Auto;
  });
```

### 2) Configure authorization requirement

```csharp
using Microsoft.AspNetCore.Authorization;

public sealed class DashboardAccessRequirement : IAuthorizationRequirement;

public sealed class DashboardAccessHandler : AuthorizationHandler<DashboardAccessRequirement>
{
  protected override Task HandleRequirementAsync(
    AuthorizationHandlerContext context,
    DashboardAccessRequirement requirement)
  {
    context.Succeed(requirement);
    return Task.CompletedTask;
  }
}

builder.Services.AddSingleton<IAuthorizationHandler, DashboardAccessHandler>();
builder.Services.AddSingleton<DashboardAccessRequirement>();
```

### 3) Map endpoints and UI

```csharp
var app = builder.Build();
var accessRequirement = app.Services.GetRequiredService<DashboardAccessRequirement>();

app.MapFeatureManagementEndpoints(accessRequirement); // /api/feature-flags
app.UseFeatureManagementUi();                        // /feature-flags

await app.RunAsync();
```

## Schema Initialization

- Schema initialization runs automatically at startup through an internal hosted service.
- Relational providers use embedded SQL scripts only.
- Non-relational providers use `EnsureCreated`.
- For relational providers, startup applies the base `create_*` script and then runs pending `migrate_*` scripts automatically.
- Applied migrations are tracked in `FeatureManagementSchemaMigrations` to ensure each script runs only once.

Available script files:

- `create_feature_management_tables.postgres.sql`
- `create_feature_management_tables.mysql.sql`
- `create_feature_management_tables.sqlite.sql`
- `migrate_postgres_*.sql`
- `migrate_mysql_*.sql`
- `migrate_sqlite_*.sql`

You can force a specific script:

```csharp
builder.Services.AddFeatureManagementUi(
  options => options.UseNpgsql(builder.Configuration.GetConnectionString("FeatureFlagsDb")!),
  TimeProvider.System,
  schema => schema.SqlScriptProvider = FeatureManagementSqlScriptProvider.Postgres);
```

## API Endpoints

Default route prefix: `/api/feature-flags`

- `GET /api/feature-flags` - list feature flags (supports `search`, `owner`, `tag` query params)
- `GET /api/feature-flags/{name}` - get a single feature flag (returns `ETag: "v{version}"`)
- `GET /api/feature-flags/{name}/audit` - read audit history for a flag
- `GET /api/feature-flags/{name}/activity` - read activity feed for a flag
- `POST /api/feature-flags` - create feature flag
- `PUT /api/feature-flags/{name}` - update feature flag (supports `If-Match: "v{version}"`)
- `POST /api/feature-flags/{name}/rollback/{targetVersion}` - rollback to an audited version
- `POST /api/feature-flags/{name}/schedule` - schedule a future feature change (`{ flag, scheduledAtUtc }` payload)
- `DELETE /api/feature-flags/{name}` - delete feature flag

Default UI route: `/feature-flags`

## Cache Behavior

The feature definition provider uses versioned cache keys in memory:

- current key pattern: `feature-flags:all:v{version}`
- on writes (`POST`/`PUT`/`DELETE`), version is bumped
- after reload, previous version cache key is removed to avoid stale accumulation

## Client App (embedded UI)

Client path: `FeatureManagement.Dashboard/client-app`

```bash
cd FeatureManagement.Dashboard/client-app
npm install
npm start
```

Useful scripts:

```bash
npm run build
npm test
npm run test:run
npm run test:coverage
```

## Notes for Production

- Use a persistent provider with available scripts (PostgreSQL / MySQL / SQLite), not in-memory.
- Protect routes with your host authentication and authorization middleware.
- Ensure route prefixes are aligned with reverse proxy/base-path settings.
