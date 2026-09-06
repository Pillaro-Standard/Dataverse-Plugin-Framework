# Changelog

## 1.2.0-rc

### Pillaro.Dataverse.PluginFramework

- Fixed `TaskBase<TEntity>` so pre-images and post-images are initialized for every message (#61). They used to be loaded only for messages that also carry an `Entity` target (`Create`, `Update`), so a task registered on `Delete` got `null` in `PreImage` even though the image was registered on the step. `ContextEntity` initialization is unchanged.
- Added `GetPreImageName()` and `GetPostImageName()` to `TaskBase<TEntity>`, so a task whose step registers images under a name other than `image` can have them loaded into `PreImage` and `PostImage`.
- `HasPreImage(...)` and `HasPostImage(...)` validation now also fails when the image is registered on the step but carries no data, instead of reporting a valid step for an image the task would read as `null`.
- Entity-typed registration is now available for every message, not only `Update`. `OnCreate<TEntity>(...)`, `OnDelete<TEntity>(...)` and `OnMessage<TEntity>(...)` return an entity-typed builder, so typed filtering attributes (`WhenChanged(c => c.FirstName)`, `WithFilteringAttributes(c => c.FirstName)`) and typed images (`WithPreImage(..., c => c.FirstName)`) work for all of them. `WhenChanged(...)` is also available on the string-based builders.
- Added `WithBothImage(...)` and `PluginImageType.Both`, exposing the Dataverse `Both` image type (value 2) that the deployer could already write but no registration could produce.
- Added `WithImage(PluginImageOptions)` for the image combinations the shorthands cannot express: a distinct `EntityAlias` and an explicit `MessagePropertyName` (for example `Merge` with `SubordinateId`).
- Image `EntityAlias` is now registered from the registration instead of always being forced to the image name.
- Image `MessagePropertyName` derivation now handles `Send` per entity (`FaxId` for `fax`, `TemplateId` for `template`, otherwise `EmailId`), refining the derivation added in 1.1.3-rc. The derived value can also be overridden per image with `WithImage(PluginImageOptions)`.
- Fixed manifest validation, which rejected every image on a PreValidation step. Pre-images are valid in PreValidation, PreOperation and PostOperation; the rule that was missing is that post-images (and `Both`) are available only in PostOperation, and that is now enforced instead.
- Image uniqueness within a step is now checked per image collection using the entity alias, so a pre-image and a post-image may share a key while duplicates within one collection are rejected.
- The deployment diff now compares image `EntityAlias` and `MessagePropertyName`, so drift in either is detected.

## 1.1.3-rc

### Pillaro.Dataverse.PluginFramework

- Fixed step image registration so `MessagePropertyName` is derived from the step message instead of always sending `Target` (#57). Post-images on `Create` steps now register with `Id`; `SetState`/`SetStateDynamicEntity` use `EntityMoniker` and `Send`/`DeliverIncoming`/`DeliverPromote` use `EmailId`.
- Added support for Custom API MainOperation handlers in the deployment manifest (#56). A step registered with `OnMessage(...).MainOperation()` keeps the plugin type in the manifest so the assembly and plugin type are deployed, but no `SdkMessageProcessingStep` is created, updated, or deleted for it; the diff output marks it as `[TYPE-ONLY]`. Stage-30 steps auto-created by Dataverse for Custom APIs are never touched by step synchronization.
- Manifest validation now rejects MainOperation registrations that define images or target the platform messages `Create`, `Update`, or `Delete`.
- Deployment now re-enables steps that were manually disabled in Dataverse. A disabled step in the manifest is reported in the diff output (`State: step was disabled in Dataverse and will be re-enabled by deployment.`) and updated back to enabled, so deployed registrations always end up active.
- Deployment output now colors status labels: `CREATE` green, `UPDATE` yellow, `CHANGE`/`WARN` orange, `DELETE`/`ERROR` red, `TYPE-ONLY` cyan, and `OK` dimmed gray so changes stand out. Colors can be disabled with the standard `NO_COLOR` environment variable.

## 1.1.2

### Pillaro.Dataverse.PluginFramework

- Relicensed from the Pillaro Community License (PCL) v1.0 to the Apache License, Version 2.0. NuGet package metadata now uses the `Apache-2.0` SPDX license expression, and a `NOTICE` file was added to the repository.

### Pillaro.Dataverse.PluginFramework.Testing

- Relicensed from the Pillaro Community License (PCL) v1.0 to the Apache License, Version 2.0. NuGet package metadata now uses the `Apache-2.0` SPDX license expression.

### Templates

- Relicensed the template license file (`dotnet new` package and Visual Studio VSIX) to the Apache License, Version 2.0.
- Updated generated projects to reference `Pillaro.Dataverse.PluginFramework` and `Pillaro.Dataverse.PluginFramework.Testing` version `1.1.2`.

## 1.1.1

### Pillaro.Dataverse.PluginFramework

- Fixed generated Windows deployment wrappers when NuGet package or project paths contain diacritics or other non-ASCII characters.
- Improved the local NuGet package build helper so it resolves repository paths correctly when run from the `scripts` directory and keeps the downloaded NuGet CLI outside the repository.

### Pillaro.Dataverse.PluginFramework.Testing

- Added Windows deployment scaffolding coverage for Unicode paths, profile forwarding, settings resolution, working directory handling, and exit code propagation.

## 1.1.1-rc

### Pillaro.Dataverse.PluginFramework

- Fixed generated Windows deployment wrappers when NuGet package or project paths contain diacritics or other non-ASCII characters.
- Improved the local NuGet package build helper so it resolves repository paths correctly when run from the `scripts` directory and keeps the downloaded NuGet CLI outside the repository.

### Pillaro.Dataverse.PluginFramework.Testing

- Added Windows deployment scaffolding coverage for Unicode paths, profile forwarding, settings resolution, working directory handling, and exit code propagation.

## 1.1.0

### Pillaro.Dataverse.PluginFramework

- Added code-first plugin registration metadata API through `Register(IPluginRegistration registration)`.
- Added deployment CLI support for Dataverse plugin assembly deployment and plugin step/image synchronization.
- Added generated deployment tooling for consuming plugin projects.
- Added generated early-bound entity generation tooling.
- Added Visual Studio VSIX project template support for generating a standard Logic / Plugins / Tests solution structure.
- Fixed deployment registration upsert so image create/update changes are applied even when the parent step is unchanged.
- Changed `SecureConfig` and `UnsecureConfig` handling to expose raw string values intentionally instead of automatic JSON parsing.
- Improved logging for plugin registration, configuration, and empty registration metadata.
- Updated documentation for plugin registration, deployment, early-bound generation, CI/CD, generated tooling, and Visual Studio template packaging.

### Pillaro.Dataverse.PluginFramework.Testing

- Aligned package dependencies and metadata for configuration, environment variables, memory cache, and Dataverse testing support.
- Added support required by generated template test projects.

## 1.1.0-rc

### Pillaro.Dataverse.PluginFramework

- Added code-first plugin registration metadata API through `Register(IPluginRegistration registration)`. See [Plugin Registration API](docs/plugins/plugin-registration-api.md).
- Added deployment CLI support for Dataverse plugin assembly deployment and plugin step/image synchronization. See [Deployment Plugins](docs/plugins/deployment-plugins.md).
- Added generated deployment tooling for consuming plugin projects, including `PillaroSettings.json`, deployment wrappers, and deployment documentation.
- Added generated early-bound entity generation tooling for consuming plugin projects, including `Tools/EarlyBound/GenerateEarlyBound.bat`, user-owned `Tools/EarlyBound/EarlyBoundSettings.json`, package-managed tooling documentation, and Power Platform CLI (`pac modelbuilder`) support. See [Early-Bound Entity Generation](docs/plugins/early-bound-generation.md).
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

### 1.0.2

### Pillaro.Dataverse.PluginFramework
- Promoted package version after successful verification and production deployment.
- No functional changes were introduced in this release.

### Pillaro.Dataverse.PluginFramework.Testing
- Promoted package version after successful verification and production deployment.
- No functional changes were introduced in this release.


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
