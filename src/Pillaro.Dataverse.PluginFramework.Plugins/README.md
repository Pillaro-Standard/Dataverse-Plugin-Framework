# Pillaro.Dataverse.PluginFramework.Plugins

This project contains Dataverse plugin implementations and related logic used by the Pillaro Framework.

## Structure Overview

The project follows a simple and consistent structure to keep responsibilities clear and easy to navigate:

* **Plugins/**

  * Contains plugin entry points (e.g. `AutonumberingPlugin`)
  * Each plugin represents a Dataverse execution entry

* **Tasks/**

  * Contains executable units (use-cases)
  * Each task represents a single operation triggered by a plugin

* **Features/**

  * Groups feature-specific logic
  * Contains supporting components used by tasks (renderers, providers, helpers, etc.)
  * Example: `Autonumbering/`

## Execution Flow

1. Dataverse triggers a plugin
2. The plugin maps execution to a Task
3. The Task performs validation and execution using Feature components

## Task Model

Tasks are the core execution units of the framework.

Each task:

* Inherits from a generic base class
* Has two distinct phases:

  * **Validation** – checks whether execution is allowed
  * **Execution** – performs the actual logic

### Task Types

Tasks can be implemented in two ways:

* **Typed (entity-specific)**

  * Designed for a specific Dataverse entity
  * Most common approach

* **Generic (multi-entity)**

  * Works across multiple entities
  * Useful for cross-cutting logic

## Design Principles

* Plugins are **thin entry points**
* Tasks contain **application logic orchestration**
* Features contain **feature-specific implementation details**
* No unnecessary abstraction layers (e.g. generic services) are introduced to keep the code simple and explicit

This structure ensures:

* high readability
* clear separation of concerns
* easy onboarding for new contributors

## Plugin Registration

Plugin registration is defined directly on plugin classes using `CrmPluginRegistration` attributes.

Each attribute represents a Dataverse plugin step.

Step identification:

* **Explicit ID (recommended)**
  * Define a fixed GUID using the `Id` property
  * Ensures stable and predictable deployments

Key rules:

* Each step must have a **stable unique ID across deployments**
* This ensures **idempotent deployments** and prevents duplicate step creation

## Build & Deployment

* Plugins must be built using a **strong name key (`.snk`)**
* Deployment settings are defined in `PillaroSettings.json`
* Early-bound types are generated using PAC modelbuilder (`pac modelbuilder build`)

### Local tooling vs packaged behavior

Note: In this repository, the helper tooling under `Tools/` (deployment wrappers, ILMerge, early-bound helpers) is included and managed in-source for examples and local development. When the framework is consumed as a NuGet package by another project, equivalent wrapper files are generated from package templates and may reference the CLI inside the package rather than the local tools folder.

For local development the recommended deploy flow is:

1. Build the CLI tool:

    dotnet build tools\Pillaro.Dataverse.PluginFramework.Cli -c Debug

2. Run the wrapper or invoke the built DLL directly:

    dotnet "tools\Pillaro.Dataverse.PluginFramework.Cli\bin\Debug\net8.0\pillaro-dv.dll" plugin deploy --settings "PillaroSettings.json" --profile "debug"

Do not hardcode user-specific NuGet paths in documentation (for example `C:\Users\...\.nuget\packages\...`). If you need to document both scenarios, clearly separate the local development flow (use build output) and the NuGet consumer flow (package-provided wrappers).

## Adding a New Plugin

1. Create a new plugin class in `Plugins/`
2. Add one or more `CrmPluginRegistration` attributes with fixed GUIDs
3. Register a Task in the plugin constructor using `RegisterTask<TTask>(...)`
4. Implement the task in `Tasks/`
5. Add supporting logic into `Features/<FeatureName>/`
6. Deploy using `pillaro-dv plugin deploy` or the Plugin Registration Tool

## Example

A working example is available in the project:

* Plugin: `AutonumberingPlugin`
* Task: `GetAutoNumber`
* Feature: `Autonumbering`

This example demonstrates:

* plugin → task mapping
* task validation and execution flow
* feature-based implementation structure

## Diagnostics & Logging

The framework provides detailed diagnostic logging stored directly in Dataverse.

Each execution records:

* executed plugin and task
* validation and execution phases
* input data and context
* execution time and correlation identifiers

These logs are available in the Pillaro Framework application and allow full traceability of plugin execution.

## Versioning

Each plugin execution log includes a **framework version**, defined in `PluginBase`.

The version is returned from a method (e.g. `GetSolutionVersion()`) and must be maintained manually or via build automation.

Why this matters:

* allows linking errors to a specific deployed version
* helps determine whether a fix is already deployed or not
* enables reliable troubleshooting in shared environments

Current approach:

* version is **manually updated in code**, or
* injected via **CI/CD pipeline (recommended for production)**

There is currently no automatic versioning mechanism built into the framework.

## Scope

This project focuses only on plugin execution and related logic.
It does not aim to provide a full application framework or domain layer.

## Notes

* This project intentionally stays minimal
