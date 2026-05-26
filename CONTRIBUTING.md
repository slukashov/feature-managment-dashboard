# Contributing

Thanks for your interest in contributing to FeatureManagement.Dashboard.

## Development setup

1. Install .NET 10 SDK.
2. Install Node.js 20+.
3. Clone the repository.
4. Restore dependencies and run tests.

## Build and test

```bash
dotnet restore
dotnet test
cd FeatureManagement.Dashboard/client-app
npm ci
npm run test:run
```

## Pull request guidelines

- Create a feature branch from `main`.
- Keep PRs focused and small.
- Add or update tests for behavior changes.
- Update `README.md` and `CHANGELOG.md` if user-facing behavior changes.
- Ensure CI passes before requesting review.

## Coding standards

- Keep nullable reference types enabled.
- Prefer explicit names over abbreviations.
- Add comments only for non-obvious logic.

## Commit messages

Use clear messages describing user-visible behavior, for example:
- Add SQL script selection by provider
- Fix embedded UI path on Linux CI