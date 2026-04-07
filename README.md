# Pillaro Dataverse Plugin Framework

> A source-available framework providing a structured, task-based approach for building predictable, testable, and maintainable Microsoft Dataverse plugins in C#.

[![License: PCL v1.0](https://img.shields.io/badge/License-PCL%20v1.0-blue.svg)](#license)
[![Status: Pre-release](https://img.shields.io/badge/Status-Pre--release-orange.svg)](#current-status)

---

## Table of Contents

- [Overview](#overview)
- [What Problem It Solves](#what-problem-it-solves)
- [Architecture](#architecture)
- [Key Features](#key-features)
- [Getting Started](#getting-started)
- [Examples](#examples)
- [When to Use](#when-to-use)
- [Repository Structure](#repository-structure)
- [AI-Ready Structure](#ai-ready-structure)
- [Current Status](#current-status)
- [License](#license)

---

## Overview
> This project uses a community license that allows commercial use in services but restricts resale as a standalone product.

Pillaro Framework provides a consistent way to design and implement Dataverse plugins using a **task-based execution model**.

Each piece of business logic is split into small, focused units called **tasks** with a clear lifecycle:

1. **Validation** — can this task run?
2. **Execution** — do the work.

This structure makes plugins predictable, testable, and easier to maintain over time.

---

## Documentation

Detailed documentation, including setup guides and architecture explanations, is available in the [docs](./docs/README.md) section.

---

## What Problem It Solves

Standard plugin development often leads to:

| Problem | Framework Solution |
|---|---|
| Large classes with mixed responsibilities | Each piece of functionality = one task |
| Validation and execution logic combined | Validation is strictly separated from execution |
| Duplicated patterns across projects | Consistent structure enforced by base classes |
| Difficult testing and debugging | Built-in logging; tasks are independently testable |
| No structured approach to integration testing | Built-in xUnit toolkit for writing programmatic integration tests |

### Example Scenarios

> **Note:** The following scenarios are provided solely to demonstrate the framework’s functionality. They are not designed for production use and may not meet the security, performance, or process requirements of real-world projects.

- **Automatic validation of contact names on create or update**  
  Each attempt to create or update a contact triggers the `ValidateContactNamesTask`, which:
  - Ensures that both first and last name fields are filled in.
  - Checks that the first name does not contain forbidden words (e.g., according to company policy).
  - If validation fails, returns a user-friendly error and logs the reason in detail.

- **Dynamic update of contact address label**  
  The `UpdateAddressLabel` task, when address fields change:
  - Verifies that relevant fields have actually changed (using PreImage for updates).
  - Normalizes and concatenates address parts into a single label.
  - Ensures the update is performed only when necessary, minimizing unnecessary writes.

- **Task summary synchronization based on changes in related entities**  
  The `TaskSummarySync` task:
  - Monitors changes in fields such as regarding, scheduled time, or task state.
  - On change, recalculates the task description based on the current state of the related record (e.g., contact or account).
  - Uses PreImage and PostImage to compare previous and new states.

- **Complex validation on owner change**  
  A set of validators within a single task:
  - First, quickly checks basic conditions (e.g., user permissions).
  - Then performs more demanding checks (e.g., dependencies on other entities).
  - Each rule is a separate validator, making extension and maintenance easy.

---

## Architecture (Simplified)

```
Plugin
  ↓
Task
  ├─ Validation
  └─ Execution
```

### Plugin

The entry point registered with Dataverse. It:

- Receives the execution context
- Matches registered tasks to the current event (stage, message, entity, mode)
- Executes matching tasks in order
- Handles logging and pipeline flow

### Task

A single unit of business logic. Each task:

- Has one responsibility
- Defines fluent validation rules in `AddValidations()`
- Contains execution logic in `DoExecute()`
- Is independently testable

### Validation

- Runs **before** execution
- Uses a fluent API to chain validators in a specific order
- Can **skip** the task (`WithBreakValidation`) or **throw** an error (`ThrowWithError`)
- Allows other tasks to continue even if one task's validation fails

### Execution

- Runs **only** if all validations pass
- Contains **only** business logic — no guard checks or validation
- Produces predictable, traceable results

---

## Key Features

### 🧩 Task-Based Architecture

Each plugin is composed of independent tasks registered via `RegisterTask<T>()`.

- Keeps business logic small and focused
- Makes code easier to understand and maintain
- Allows testing logic in isolation

### ✅ Fluent Validation Model

Each task defines its own validation rules through a fluent API with enforced ordering:

1. Context filters (`WithMode`, `WithStage`, `WithMessage`)
2. Entity scope (`ForEntity` / `ForEntities`)
3. Image requirements (`HasPreImage` / `HasPostImage`)
4. Attribute presence (`EntityWithAtLeastOneAttribute` / `EntityWithAllAttributes`)
5. Custom validations (`WithValidation`)
6. Flow control (`WithBreakValidation`, `ThrowWithWarning`, `ThrowWithError`)

### ⚙️ Runtime Configuration

Configuration is stored in Dataverse and loaded at runtime.

- Change behavior without redeploying plugins
- Supports environment-specific settings
- Avoids hardcoded values

### 🔢 Autonumbering

Supports configurable number sequences.

- Consistent numbering across records
- Supports parent-based numbering
- Safe for concurrent operations

### 📋 Diagnostic Logging

Logging is built into the execution pipeline — every task produces a structured log automatically.

- Shows exactly what happened during execution
- Tracks execution flow, timing, and depth
- Includes input/output parameters and entity images

### 🧪 Testing Support

Provides a foundation for testing against a real Dataverse environment.

- Validates real behavior, not just isolated code
- Ensures plugins work together correctly
- Reduces risk in production deployments

---

## Getting Started

### Prerequisites

* **Dataverse / Dynamics 365 environment**
* **Framework solution installed** — the framework requires the Dataverse solution located in `power-platform-solutions\framework` to be imported into your environment. This solution contains essential configuration entities and dependencies required for proper functionality.
* **.NET Framework 4.6.2** (required by Dataverse plugin sandbox)
* Visual Studio or Visual Studio Code
* Plugin must be deployed as a single assembly

> [!WARNING]
> If you use SPKL for early-bound entity generation, do **not** upgrade `Microsoft.CrmSdk.CoreTools` beyond version **9.1.0.92** — higher versions break `CrmSvcUtil.exe` generation via SPKL. See [Known Limitations](./docs/README.md#-known-limitations) for details.

### Quick Start

1. Create a Class Library project targeting .NET Framework 4.6.2
2. Add a reference to the framework via NuGet
3. Enable assembly signing for the plugin project
4. Create your solution-specific `PluginBase`
5. Create plugin classes and register tasks
6. Implement validation and execution logic in tasks
7. Configure the build to produce a single deployable assembly
8. Build and register the plugin assembly in Dataverse

Detailed setup, signing, packaging, and deployment guidance will be provided in the [docs](./docs) section.

---

## Examples

### PluginBase (one per solution)

Register your solution-wide plugin configuration and common tasks.

~~~csharp
public class PluginBase : PluginFramework.Plugins.PluginBase
{
    public PluginBase(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
    {
    }

    public override string GetSolutionVersion()
    {
        return "1.0";
    }
}
~~~

### Plugin (one per entity)

Inherit from `PluginBase` and register plugin-specific tasks.

~~~csharp
public class TaskPlugin : PluginBase
{
    public TaskPlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
    {
        //Pre
        RegisterTask<TaskAutoNumbering>(PluginStage.Preoperation, ["Create"], Task.EntityLogicalName,PluginMode.Synchronous);

        //Post
        RegisterTask<TaskSummarySync>(PluginStage.Postoperation, ["Create", "Update"], Task.EntityLogicalName,PluginMode.Synchronous);
    }
}
~~~

### Task

Define validation and execution logic for a specific task.

~~~csharp
 public class TaskAutoNumbering(IServiceProvider serviceProvider, TaskContext taskContext) : TaskBase<Logic.Task>(serviceProvider, taskContext)
 {
     protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
     {
          return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Postoperation)
                .WithMessages(["Create", "Update"])
                .ForEntity(ContextEntity.LogicalName)
                .HasPreImageWhen(ctx => ctx.Message == "Update")
                .HasPostImageWhen(ctx => ctx.Message == "Update")
                .EntityWithAtLeastOneAttributeWhen(
                    ctx => ctx.Message == "Update",
                    ContextEntity,
                    nameof(ContextEntity.RegardingObjectId), 
                    nameof(ContextEntity.ScheduledEnd), 
                    nameof(ContextEntity.ScheduledStart), 
                    nameof(ContextEntity.StateCode),
                    nameof(ContextEntity.StatusCode));
     }

     protected override void DoExecute()
     {
         // Business logic goes here
     }
 }
~~~

---

## When to Use

| 🚀 Highest Added Value | ⚖️ Lower Added Value (but still applicable) |
|---|---|
| Solution contains multiple plugins or integration points | Solution contains one or a few simple plugins |
| Long-term evolution and feature growth are expected | No significant future development is expected |
| Business logic is growing or expected to grow in complexity | Logic is simple (e.g., single operation, basic update) |
| You need reliable, repeatable automated testing | Testing is not a key requirement |
| You want consistent structure across projects and teams | Project is small, isolated, without need for shared standards |
| Maintainability and scalability are important | Short-term or one-off solution |
| You need structured logging for debugging and observability during development | Basic or minimal logging is sufficient |
| You need the ability to adjust or toggle behavior at runtime without a new release | Behavior is static and does not need to change without deployment |

### Summary
The framework can be used in any scenario — its core purpose is to structure the solution and prepare it for future growth while enabling fast development start.

The difference lies in the level of value it brings in a given context.

---

## Repository Structure

~~~
/src → Framework source code
  ├─ Pillaro.Dataverse.PluginFramework
  ├─ Pillaro.Dataverse.PluginFramework.Plugins
  └─ Pillaro.Dataverse.PluginFramework.Testing
/tests → Test projects
  ├─ Pillaro.Dataverse.PluginFramework.Tests
  └─ Pillaro.Dataverse.PluginFramework.Tests.EarlyBoundGen
/examples → Sample implementations
  ├─ Pillaro.Dataverse.PluginFramework.Examples.Logic
  ├─ Pillaro.Dataverse.PluginFramework.Examples.Plugins
  └─ Pillaro.Dataverse.PluginFramework.Examples.Tests
/docs → Documentation
/power-platform-solutions → Solution files ready to import into Dataverse
  ├- examples
  └─ framework
~~~

---

## AI-Ready Structure

The framework enforces a consistent and predictable structure, making the codebase easier to analyze and reason about — both for developers and automated tools.

- Functionality is split into clearly defined tasks
- Each task has explicit validation and execution phases
- Behavior is deterministic and traceable
- Patterns are uniform across all plugins

This enables:

- **Automated code generation** — AI tools can reliably produce new tasks and plugins
- **Consistent structure** — every project follows the same conventions
- **Analysis and mapping** — straightforward correlation between requirements and implementation

---

## Current Status

- 🟡 Preparing for first public release
- 📝 Documentation in progress

---

## Key Principles

| Principle | Description |
|---|---|
| **Separate validation from execution** | `AddValidations()` and `DoExecute()` are always distinct |
| **Keep logic small and focused** | One task = one responsibility |
| **Ensure predictable behavior** | Fluent validators execute in defined order |
| **Make execution flow explicit** | Logging is automatic, every step is traceable |
| **Maintain consistency** | All plugins follow the same base pattern |
| **Versioned logging** | Update `PluginBase.GetSolutionVersion()` on each release so logs include a clear solution/version identifier |
| **Per-task programmatic tests** | Generate and maintain automated tests for every task; tests must run in CI and nightly builds |
| **Production logging levels** | Production environments default to `Warning` or `Error` to limit log volume and storage impact |
| **Environment log retention** | Implement environment-specific log retention/cleanup logic to remove old logs and reduce DB storage usage |
| **Nightly CI runs** | Schedule a nightly build that runs the full test matrix (including programmatic task tests) to detect regressions early |

## Operational guidelines (summary)

Detailed operational guidance is stored in `/docs/operational-guidelines.md`. Summary actions to include there:
- Release checklist: update `PluginBase.GetSolutionVersion()`, tag release, and ensure version appears in all logs.
- Per-task tests: add one xUnit test class per task under `/tests`; tests should be runnable locally and in CI. Prefer programmatic, reproducible tests interacting with a shared test environment.
- CI: configure pipeline (GitHub Actions / Azure DevOps) to run full test matrix on PR and a nightly build executing all programmatic tests.
- Production logging policy: default runtime config for production should set log level to `Warning` or `Error`; allow temporary overrides for troubleshooting via secure runtime settings.
- Log retention: implement environment-specific retention (e.g., purge logs older than N days) via scheduled job (DB stored-procedure, Azure Function, Logic App or similar) to keep DB storage optimal.
- Where to put details: include sample CI YAMLs, retention scripts, test generation tooling, and example `PluginBase` version propagation code in `/docs/operational-guidelines.md`.

Notes:
- Move implementation examples (CI YAML, scripts, sample code) to `/docs` to keep README concise and discoverable.
- I can generate a ready-to-use `/docs/operational-guidelines.md` with sample CI YAML, test stub templates, a log retention SQL/PowerShell script, and a recommended `PluginBase` logging propagation snippet. Tell me if you want that generated now.
---

## License

This project is licensed under the **Pillaro Community License (PCL) v1.0**.

| | |
|---|---|
| ✅ Use the framework in your projects and commercial solutions | ❌ Sell the framework as a product |
| ✅ Modify it and build on top of it | ❌ Rebrand it or present it as your own framework |
| ✅ Charge for implementation and services | ❌ Package it as a paid toolkit, platform, or SaaS where it is the main value |

If you use the framework in a delivered solution, you must include:

> "This solution is built using Pillaro Dataverse Plugin Framework."

This framework is open for use, extension, and contribution,
but is not licensed as a permissive open-source project and is not intended for resale as a standalone or competing product.

See the full license in the [LICENSE](LICENSE) file.
