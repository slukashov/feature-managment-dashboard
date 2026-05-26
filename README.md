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
- Rule-based targeting (`AlwaysOn`, `Microsoft.Percentage`, `Microsoft.TimeWindow`, custom JSON)
- Feature definition provider backed by database + memory cache
- Version-aware updates for optimistic concurrency
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

Available script files:

- `create_feature_management_tables.postgres.sql`
- `create_feature_management_tables.mysql.sql`
- `create_feature_management_tables.sqlite.sql`

You can force a specific script:

```csharp
builder.Services.AddFeatureManagementUi(
  options => options.UseNpgsql(builder.Configuration.GetConnectionString("FeatureFlagsDb")!),
  TimeProvider.System,
  schema => schema.SqlScriptProvider = FeatureManagementSqlScriptProvider.Postgres);
```

## API Endpoints

Default route prefix: `/api/feature-flags`

- `GET /api/feature-flags` - list feature flags
- `POST /api/feature-flags` - create feature flag
- `PUT /api/feature-flags/{name}` - update feature flag
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
