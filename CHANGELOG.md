# Changelog

## 1.1.0-rc

### Pillaro.Dataverse.PluginFramework

- Added code-first plugin registration metadata API through `Register(IPluginRegistration registration)`.
- Added deployment CLI support for Dataverse plugin assembly deployment and plugin step/image synchronization.
- Added generated deployment tooling for consuming plugin projects, including `PillaroSettings.json`, deployment wrappers, and deployment documentation.
- Fixed deployment registration upsert so image create/update changes are applied even when the parent step is unchanged.
- Changed `SecureConfig` and `UnsecureConfig` handling to expose raw string values intentionally instead of automatic JSON parsing.
- Added unsecure configuration details to logs and masked secure configuration values by logging only that secure configuration is registered.
- Added logging when `Register` method is empty or not overridden, indicating no steps were registered via registration API.
- Clarified documentation for Create and Update filtering attributes, generated deployment tooling, SDK-style early-bound source inclusion, and `MinimalSeverityLevel`.
- Aligned package dependency metadata and deployment documentation with the current build and packaging behavior.
- Fixed issue #25, where the NuGet package build had incorrect changelog content.

### Pillaro.Dataverse.PluginFramework.Testing

- Aligned package dependencies and package metadata for configuration, environment variable, memory cache, and Dataverse testing support.
- Fixed issue #25, where the NuGet package build had incorrect changelog content.

### 1.0.2-rc

### Pillaro.Dataverse.PluginFramework
- Improved release readiness before the production version.
- Added CI/CD documentation for testing, packaging, and release workflow.
- Updated contributing guidelines with branching strategy and pull request rules.
- Added documentation for the model-driven Pillaro Plugin Framework application.
- Documented recommended C# language version setup for plugin projects.

### Pillaro.Dataverse.PluginFramework.Testing
- Added nightly automated testing against a live Dataverse environment.
- Added test execution to the package build pipeline.
- Added publishing of test results and code coverage.
- Updated testing documentation with continuous testing information.

## 1.0.1-rc

### Pillaro.Dataverse.PluginFramework
- Release candidate for the next framework release.
- Core plugin documentation has been completed and aligned with the current framework structure.
- Framework behavior is being validated before final release.

### Pillaro.Dataverse.PluginFramework.Testing
- Release candidate for the next testing package release.
- Core testing documentation structure has been prepared and aligned with the current testing architecture.
- Testing behavior is being validated before final release.

## 1.0.1-ci

### Pillaro.Dataverse.PluginFramework
- Finalizing framework logic and validating it before production release.
- Ongoing testing of the stabilized API.
- Documentation is being completed and refined for the final release.
- Provides a structured, task-based foundation for Microsoft Dataverse plugins.

### Pillaro.Dataverse.PluginFramework.Testing
- Finalizing testing layer logic and validating real-world scenarios.
- Ongoing testing of integration with the core framework.
- Documentation is being completed and refined for the final release.
- Enables effective testing of plugins built on top of the framework.

## 1.0.0-ci

### Pillaro.Dataverse.PluginFramework
- Continuous integration build of the core plugin framework.
- Intended for internal testing and validation only.
- May contain incomplete or unstable changes.

### Pillaro.Dataverse.PluginFramework.Testing
- Continuous integration build of the testing package.
- Intended for internal testing of plugin scenarios.
- May contain incomplete or unstable changes.
