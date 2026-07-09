# CI/CD Pipelines

This document describes the automated pipelines used to ensure code quality, testing, and package distribution.

---

## Overview

The repository uses Azure DevOps pipelines for:

- automated testing
- package building and versioning
- quality assurance

All pipelines are defined in YAML files at the repository root.

---

## Nightly Test Pipeline

**File**: `Nightly – Tests Only.yml`

### Nightly Purpose

Validates all test projects against a real Dataverse environment to detect integration issues and regressions early.

### Schedule

- **Frequency**: Daily
- **Time**: 2:00 AM UTC (4:00 AM UTC+2)
- **Branches**: `main` and `develop`
- **Trigger**: Scheduled (runs even if no changes were made)

### Nightly Execution Flow

1. **Checkout**: Fetches latest code (shallow clone, depth 1)
2. **SDK Installation**: Installs .NET SDK 8.x
3. **Restore**: Restores all NuGet packages for test projects
4. **Build**: Builds all test projects in Release configuration
5. **Test Execution**: Runs all tests with code coverage collection
6. **Results Publishing**: Publishes test results and coverage reports

### Test Projects Included

~~~
tests/**/*.Tests.csproj
examples/**/*.Tests.csproj
~~~

This includes:

- `Pillaro.Dataverse.PluginFramework.Tests`
- `Pillaro.Dataverse.PluginFramework.Examples.Tests`

### Test Environment

Tests are executed against a live Dataverse environment using a secured connection string stored in Azure DevOps variable group `dataverse-test-secrets`.

Environment variable:

~~~
ConnectionStrings__Dataverse
~~~

### Output

- **Test Results**: Published in VSTest format (`.trx` files)
- **Code Coverage**: XPlat Code Coverage in Cobertura format
- **Test Run Title**: `Nightly Tests`

### Failure Behavior

The pipeline **fails** if:

- any test fails (`failTaskOnFailedTests: true`)
- build errors occur
- restore fails

### Why Nightly?

Running tests on a schedule (rather than on every commit) provides:

- **Non-blocking development**: Contributors are not blocked by long-running integration tests
- **Early detection**: Issues are identified within 24 hours
- **Cost efficiency**: Reduces Dataverse API usage and pipeline runtime
- **Stability validation**: Confirms that code in `main` and `develop` remains stable

---

## Package Build Pipeline

**File**: `Packages – Build & Package.yml`

### Package Purpose

Builds and packages NuGet packages for distribution.

### Trigger

- **Manual only**: No automatic triggers
- **On-demand**: Executed when a new package version is needed

### Parameters

#### Base Version

Format: `Major.Minor.Patch` (e.g., `1.0.0`)

Defines the base semantic version for the package.

#### Package Type

Determines version suffix and target audience:

| Type | Version Format | Purpose |
|------|---------------|---------|
| `ci` | `1.0.0-ci.{buildId}` | Continuous integration builds |
| `preview` | `1.0.0-preview.{buildId}` | Preview releases for early testing |
| `rc` | `1.0.0-rc.{buildId}` | Release candidate builds |
| `release` | `1.0.0` | Stable production release |

### Package Execution Flow

1. **Checkout**: Fetches full repository history (`fetchDepth: 0`)
2. **SDK Installation**: Installs .NET SDK 8.x
3. **Version Calculation**: Determines package and assembly versions
4. **Build**: Builds framework and testing projects
5. **Pack**: Creates NuGet packages (`.nupkg` files)
6. **Publish**: Uploads packages as pipeline artifacts

### Packages Produced

- **Pillaro.Dataverse.PluginFramework**: Core framework package
- **Pillaro.Dataverse.PluginFramework.Testing**: Testing infrastructure package

### Version Strategy

#### Assembly Versioning

- **AssemblyVersion**: `Major.Minor.Patch.0` (stable for binding)
- **FileVersion**: `Major.Minor.Patch.BuildId` (unique per build)
- **InformationalVersion**: Full package version with suffix

#### Release Notes

NuGet package metadata links to the central [CHANGELOG.md](../CHANGELOG.md) file through the source branch used by the package build.

This keeps package metadata simple while the detailed release history stays in one maintained place. It also allows release-branch documentation and changelog corrections without rebuilding the NuGet package.

The packaging pipeline stamps both framework package nuspec files with:

```text
https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/<source-branch>/CHANGELOG.md
```

Package verification rejects release notes that point to the exact source commit instead of the source branch.

### Environment Variables

Stored in Azure DevOps variable group `dataverse-test-secrets`:

- **DataverseConnectionString**: Connection string for integration tests (if tests are executed during packaging)

---

## Template Artifact Pipeline

**File**: `Templates - Build Template Artifacts.yml`

### Template Purpose

Builds both official template deliveries in one Azure DevOps run:

- the Visual Studio template ZIP and VSIX package
- the CLI-oriented `dotnet new` NuGet template package

This pipeline prepares two separate Azure DevOps artifacts so both template formats can be published or downloaded together.

### Trigger

- **Manual only**: No automatic triggers
- **On-demand**: Executed when either template artifact set is needed

When you queue the pipeline manually, Azure DevOps prompts for `baseVersion` and `packageType` in the same style as the framework package pipeline.

### Parameters

| Parameter | Purpose |
|-----------|---------|
| `baseVersion` | Base version entered at queue time for both template packages, in `Major.Minor.Patch` format |
| `packageType` | Determines whether the NuGet template version becomes `ci`, `preview`, `rc`, or `release` |
| `visualStudioArtifactName` | Name of the Azure DevOps artifact containing the Visual Studio template outputs |
| `dotnetNewArtifactName` | Name of the Azure DevOps artifact containing the `dotnet new` package |

### Template Execution Flow

1. **Checkout**: Fetches the repository with full history
2. **SDK Installation**: Installs .NET SDK 8.x
3. **Version Calculation**: Determines the shared base version, then derives the VSIX and NuGet package versions
4. **Manifest stamping**: Writes the computed version into `templates/Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix/source.extension.vsixmanifest`
5. **VSIX build**: Builds `templates/Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix/Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix.csproj`
6. **VSIX validation**: Confirms the ZIP and VSIX outputs are complete and smoke-buildable
7. **NuGet restore**: Restores the `dotnet new` template project
8. **NuGet pack**: Creates the `Pillaro.Dataverse.PluginTemplate.DotNetNew` template package
9. **NuGet validation**: Confirms the package contains the expected template metadata and smoke-generates the template successfully
10. **Artifact publishing**: Uploads the Visual Studio outputs and the `.nupkg` as two separate pipeline artifacts

### Artifacts Produced

- Visual Studio template ZIP and VSIX
- `Pillaro.Dataverse.PluginTemplate.DotNetNew.<version>.nupkg`

### Version Strategy

The VSIX package uses the supplied base semantic version and appends the Azure DevOps build ID, for example `1.0.20.12345`.

The NuGet template package uses the same versioning model as the framework package pipeline, including support for `ci`, `preview`, `rc`, and `release` package types. For tag builds, the tag version is used directly, while `AssemblyVersion` and `FileVersion` remain aligned to the `Major.Minor.Patch.0` scheme.

---

## ➡️ Related documents

- [Contributing Guidelines](CONTRIBUTING.md) — Contribution workflow and testing requirements
- [Testing Overview](tests/testing.md) — Framework testing infrastructure
- [Deployment Plugins](plugins/deployment-plugins.md) — Deploy plugin assemblies and registration metadata
- [Versioning Strategy](versioning.md) — Release and version management

