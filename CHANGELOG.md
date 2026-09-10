# Changelog

## Unreleased

### Pillaro.Dataverse.PluginFramework

- Autonumbering no longer resolves deactivated configurations (#88). Both the `GetAutoNumber` Custom API task and `AutoNumberingService` looked configurations up without any condition on `statecode`, so deactivating a `pl_autonumbering` record did not take it out of service. With a deactivated leftover beside a current configuration, the Custom API reported *More than one primary autonumbering configuration exists* even though only one was in use, and `AutoNumberingService` picked one of them at random: its query had no `Orders`, so `FirstOrDefault()` over `RetrieveMultiple` returned whichever row the platform happened to return first. Both now filter to active configurations, and `AutoNumberingService` orders its results so repeated calls resolve the same configuration, and reports more than one match as an error instead of choosing silently — the behaviour the Custom API task already had.

### Solutions

- Exported `PillaroFramework` 1.0.0.2 and `PillaroPluginFrameworkExamples` 1.0.0.1, managed and unmanaged. The framework solution carries the autonumbering fix above in its plugin assembly. The examples solution catches up with registrations that were added to the code after the last export: the `Post Delete Contact` step for `ArchiveDeletedContact`, `jobtitle` in the `Pre Update Contact` filter for `RecordJobTitleChange`, and a single Both image on `Post Update Task` in place of a separate pre-image and post-image. In both solutions the plugin assembly is registered under a new id, so an import replaces the previous assembly and plugin type registration rather than updating it in place. The previous versions moved into the `Archive` folders; per-solution notes are in `power-platform-solutions/framework/changelog.md` and `power-platform-solutions/examples/changelog.md`.

### Packaging

- Rewrote the NuGet `Description` of all four packages. nuget.org renders that field in search results and in `og:description`, not the package README, and every description still described how the package was built rather than the problem it solves. None of them contained "Dynamics 365" or the hyphenated "plug-in" spelling. Each description now opens with the problem domain, states what the artifact is, and ends with the license. The framework and testing descriptions live in the nuspec files, since those two packages are built with `nuget pack`.
- Aligned `PackageTags` across all four packages. The template advertised `vscode` and `templates`, the framework `dynamics crm`, and the CLI spelled Dynamics without a hyphen. All four now carry `dataverse`, `dynamics-365`, `power-platform`, `dataverse-plugin` and `csharp`, plus terms specific to each package. Package content is unchanged; only the search-facing metadata moved.

### Documentation

- Replaced the root README tagline. It omitted "Dataverse", the most-searched term in this domain, and led with "AI-ready standard", a claim the reader cannot verify from the page.
- Expanded the root README License section, which was a bare link. It now states that commercial use is free of charge, notes the Apache-2.0 patent grant and the NOTICE requirement, and links to the new `TRADEMARK.md`.
- The template package README now tells the reader to import the Pillaro framework solution into the Dataverse environment. `docs/plugins/getting-started.md` makes that a prerequisite for the runtime features — without it settings and logging do not work — but the step list went straight from build to deployment, so a reader who only ever sees the package README had no way to learn it. It is now step 4, ahead of the deployment step.
- Removed the commercial support paragraph from the root README `Overview`, where it sat as the second paragraph, before the reader knew what the framework does. The same offer, with the same link, already appears in `Support & Partnership` and again in `Need help?`.
- Added `TRADEMARK.md`, recording that the Apache-2.0 grant does not cover the names "Pillaro" and "Pillaro Dataverse Plugin Framework", what forks may and may not do with them, and where to send permission requests.


## 1.2.1

### Templates

- Fixed the Visual Studio Marketplace upload, which the Marketplace rejected with "Your extension type does not match the VSIX type. It should be uploaded as a Tools". The listing is registered with extension type `Templates` and that type cannot be changed after the extension is created, but the Marketplace only recognizes a package as a template extension when it carries nothing besides the templates. The VSIX shipped the listing logo, the `Overview.md` details asset and the license asset alongside the template, so it was classified as a tool. Those are now maintained in the publisher portal instead, and the VSIX ships only the project template: the manifest declares a single `Microsoft.VisualStudio.ProjectTemplate` asset, and `Icon`, `PreviewImage`, `License` and both `Microsoft.VisualStudio.Services.Content.*` assets are gone. This reverts the 1.2.0 changes that moved the listing overview and license into the package. `Test-VisualStudioVsix` now fails the build if any payload outside `ProjectTemplates/` reappears.
- Removed `Overview.md` from the Visual Studio packaging project. It was only there to be published as the details asset, which is exactly what the Marketplace rejects, and nothing referenced it any more.

## 1.2.0

First stable release since 1.1.2. It also carries the changes from 1.1.3-rc, which was never released as a stable version, so everything below reaches a 1.1.2 consumer for the first time.

### Pillaro.Dataverse.PluginFramework

- `TaskContext.AddEntityToUpdate(...)` is now actually written (#63). `PluginBase` applies the queued entities once all tasks of the execution have run: attributes queued for the same record by several tasks are merged and written with a single `Update`, so the registered steps are not triggered repeatedly, and in a pre-stage values for the record the plugin is running on are merged into the message target instead of being written separately. Nothing is written when a task fails. Writes are performed as the user the step runs as, so the audit keeps showing who changed the record; a task can ask for another one with `AddEntityToUpdate(entity, ServiceUser.Admin)` (`ServiceUser` mirrors `OrganizationServiceProvider`: `User`, `Admin`, `InitiatingUser`), in which case the record is written once per service user and never merged into the message target. The queue members are now documented, in the API and in `docs/plugins/task-model.md`.
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
- Cleaned up the README packed into the NuGet package: it no longer opens by explaining that it is included in the NuGet package. The `pillaro-dv` CLI bundled under `tools/Deployment` now carries `Company` and `Copyright` assembly metadata; its behavior is unchanged.
- Fixed step image registration so `MessagePropertyName` is derived from the step message instead of always sending `Target` (#57). Post-images on `Create` steps now register with `Id`; `SetState`/`SetStateDynamicEntity` use `EntityMoniker` and `Send`/`DeliverIncoming`/`DeliverPromote` use `EmailId`.
- Added support for Custom API MainOperation handlers in the deployment manifest (#56). A step registered with `OnMessage(...).MainOperation()` keeps the plugin type in the manifest so the assembly and plugin type are deployed, but no `SdkMessageProcessingStep` is created, updated, or deleted for it; the diff output marks it as `[TYPE-ONLY]`. Stage-30 steps auto-created by Dataverse for Custom APIs are never touched by step synchronization.
- Manifest validation now rejects MainOperation registrations that define images or target the platform messages `Create`, `Update`, or `Delete`.
- Deployment now re-enables steps that were manually disabled in Dataverse. A disabled step in the manifest is reported in the diff output (`State: step was disabled in Dataverse and will be re-enabled by deployment.`) and updated back to enabled, so deployed registrations always end up active.
- Deployment output now colors status labels: `CREATE` green, `UPDATE` yellow, `CHANGE`/`WARN` orange, `DELETE`/`ERROR` red, `TYPE-ONLY` cyan, and `OK` dimmed gray so changes stand out. Colors can be disabled with the standard `NO_COLOR` environment variable.

### Templates

- Rewrote the README packed into `Pillaro.Dataverse.PluginTemplate.DotNetNew` for first-time users. It was internal build documentation describing how the package is assembled from shared source and an overlay; it now covers the install and create commands, the generated solution structure, what the first build scaffolds into `Tools/`, the steps needed before a first deployment, and prerequisites. The packaging details moved to `docs/contributing/template-packaging.md`.
- Set `PackageProjectUrl` on the `dotnet new` template package. It was missing from the packed nuspec, so nuget.org showed no link to the repository.
- The Visual Studio Marketplace listing now states the Apache-2.0 license, through a `Microsoft.VisualStudio.Services.Content.License` asset. The manifest `<License>` element already pointed at the license text, but that is only shown in the Visual Studio install dialog, not on the listing.
- Fixed the VSIX `<Tags>` metadata. It was space-separated, and the Marketplace reads that element as a comma-separated list, so the extension carried the single tag `Dataverse Power Platform Dynamics 365 Plugin` and matched neither `dataverse` nor `plugin` in search.
- The Marketplace overview is now maintained in the repository as `Overview.md` and published through a `Microsoft.VisualStudio.Services.Content.Details` asset. Publishing replaces the overview text previously entered by hand in the publisher portal. `<MoreInfo>` now points at the repository instead of the company site.
- Removed the pre-relicensing "source-open" wording from the generated solution's `Logic/README.md`, which is shared template content and therefore shipped in both delivery formats.
- The framework version that generated projects reference is now stamped by the pipeline from a new `frameworkVersion` parameter, via `scripts/Set-TemplateFrameworkVersion.ps1`. It was a manual edit across six overlay csproj files, which is why released templates kept referencing an older framework than the one shipping alongside them. The parameter is required for `packageType: release`; the versions committed in the repository are now only a fallback for local builds. The references stay pinned rather than floating, because the `Plugins` project ILMerges the framework into the signed assembly that gets deployed, and a minor-version drift can change plugin behaviour in a working solution.
- Added links to the framework and testing NuGet packages to the `dotnet new` package README and the Marketplace overview.

### Documentation

- Corrected the root README tagline, which still called the project "source-open" after the 1.1.2 relicensing to the Apache License, Version 2.0, and added a license badge to the badge block.
- Added `docs/contributing/template-packaging.md`, covering the shared template source, the two delivery formats and how they differ, the shared-source staging that runs during pack, the VSIX build flow, and how the packaging projects are versioned.

### Examples

- Added two example tasks that cover the runtime behavior fixed in this release, with functional tests against a Dataverse environment: `ArchiveDeletedContact` records a deleted contact on its parent account (pre-image on a `Delete` step, written through the update queue) and `RecordJobTitleChange` queues a value in a pre-stage, where it is merged into the message target. Both need the examples solution to be deployed before the tests can pass.

## 1.2.0-rc

### Pillaro.Dataverse.PluginFramework

- `TaskContext.AddEntityToUpdate(...)` is now actually written (#63). `PluginBase` applies the queued entities once all tasks of the execution have run: attributes queued for the same record by several tasks are merged and written with a single `Update`, so the registered steps are not triggered repeatedly, and in a pre-stage values for the record the plugin is running on are merged into the message target instead of being written separately. Nothing is written when a task fails. Writes are performed as the user the step runs as, so the audit keeps showing who changed the record; a task can ask for another one with `AddEntityToUpdate(entity, ServiceUser.Admin)` (`ServiceUser` mirrors `OrganizationServiceProvider`: `User`, `Admin`, `InitiatingUser`), in which case the record is written once per service user and never merged into the message target. The queue members are now documented, in the API and in `docs/plugins/task-model.md`.
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
- Cleaned up the README packed into the NuGet package: it no longer opens by explaining that it is included in the NuGet package. The `pillaro-dv` CLI bundled under `tools/Deployment` now carries `Company` and `Copyright` assembly metadata; its behavior is unchanged.

### Templates

- Rewrote the README packed into `Pillaro.Dataverse.PluginTemplate.DotNetNew` for first-time users. It was internal build documentation describing how the package is assembled from shared source and an overlay; it now covers the install and create commands, the generated solution structure, what the first build scaffolds into `Tools/`, the steps needed before a first deployment, and prerequisites. The packaging details moved to `docs/contributing/template-packaging.md`.
- Set `PackageProjectUrl` on the `dotnet new` template package. It was missing from the packed nuspec, so nuget.org showed no link to the repository.
- The Visual Studio Marketplace listing now states the Apache-2.0 license, through a `Microsoft.VisualStudio.Services.Content.License` asset. The manifest `<License>` element already pointed at the license text, but that is only shown in the Visual Studio install dialog, not on the listing.
- Fixed the VSIX `<Tags>` metadata. It was space-separated, and the Marketplace reads that element as a comma-separated list, so the extension carried the single tag `Dataverse Power Platform Dynamics 365 Plugin` and matched neither `dataverse` nor `plugin` in search.
- The Marketplace overview is now maintained in the repository as `Overview.md` and published through a `Microsoft.VisualStudio.Services.Content.Details` asset. Publishing replaces the overview text previously entered by hand in the publisher portal. `<MoreInfo>` now points at the repository instead of the company site.
- Removed the pre-relicensing "source-open" wording from the generated solution's `Logic/README.md`, which is shared template content and therefore shipped in both delivery formats.
- The framework version that generated projects reference is now stamped by the pipeline from a new `frameworkVersion` parameter, via `scripts/Set-TemplateFrameworkVersion.ps1`. It was a manual edit across six overlay csproj files, which is why released templates kept referencing an older framework than the one shipping alongside them. The parameter is required for `packageType: release`; the versions committed in the repository are now only a fallback for local builds. The references stay pinned rather than floating, because the `Plugins` project ILMerges the framework into the signed assembly that gets deployed, and a minor-version drift can change plugin behaviour in a working solution.
- Added links to the framework and testing NuGet packages to the `dotnet new` package README and the Marketplace overview.

### Documentation

- Corrected the root README tagline, which still called the project "source-open" after the 1.1.2 relicensing to the Apache License, Version 2.0, and added a license badge to the badge block.
- Added `docs/contributing/template-packaging.md`, covering the shared template source, the two delivery formats and how they differ, the shared-source staging that runs during pack, the VSIX build flow, and how the packaging projects are versioned.

### Examples

- Added two example tasks that cover the runtime behavior fixed in this release, with functional tests against a Dataverse environment: `ArchiveDeletedContact` records a deleted contact on its parent account (pre-image on a `Delete` step, written through the update queue) and `RecordJobTitleChange` queues a value in a pre-stage, where it is merged into the message target. Both need the examples solution to be deployed before the tests can pass.

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
