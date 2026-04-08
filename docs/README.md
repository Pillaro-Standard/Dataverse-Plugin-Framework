# Pillaro Dataverse Plugin Framework — Documentation

[![Docs: In Progress](https://img.shields.io/badge/Docs-In%20Progress-orange.svg)](#-table-of-contents)
[![License: PCL v1.0](https://img.shields.io/badge/License-PCL%20v1.0-blue.svg)](../LICENSE)

> Structured documentation for understanding, using, and extending the Pillaro Dataverse Plugin Framework.

> [!NOTE]
> Documentation is in progress. Sections marked with 🚧 are placeholders and will be completed in future updates. 
Sections marked with ⌛ are partially complete and may be updated with additional details. 

---

## 📖 Table of Contents

| Section | Description |
|---|---|
| 🚀 [Getting Started](#-getting-started) | Setup, prerequisites, and first plugin |
| 🧱 [Core Concepts](#-core-concepts) | Architecture, plugins, tasks, and validation |
| ⚙️ [Execution & Runtime](#-execution--runtime) | Pipeline and configuration |
| 🔢 [Autonumbering](#-autonumbering) | Sequence generation and concurrency handling |
| 🔍 [Observability](#-observability) | Logging and error handling |
| 🧪 [Testing](#-testing) | Testing principles and integration testing |
| 📦 [Packaging & Deployment](#-packaging--deployment) | Signing, assembly structure, and deployment |
| ⚠️ [Known Limitations](#-known-limitations) | Package constraints and compatibility notes |
| 💡 [Examples](#-examples) | Sample implementations |
| 🤝 [Contributing](#-contributing) | Guidelines for contributors |

---

## 🚀 Getting Started

Start here if you are new to the framework.

| Document | Description | Status |
|---|---|---|
| [Getting Started](./getting-started.md) | Setup, prerequisites, and first plugin implementation | ⌛ |

### Prerequisites

- .NET Framework 4.6.2 (plugin assemblies)
- .NET 10 (test projects)
- Microsoft Dataverse environment
- Visual Studio 2022 or later

---

## 🧱 Core Concepts

Understanding how the framework is designed — from architecture to validation.

| Document | Description | Status |
|---|---|---|
| [Architecture](./architecture.md) | High-level execution model and structural overview | 🚧 |
| [Plugin Model](./plugin-model.md) | Role and responsibilities of plugins | 🚧 |
| [Task Model](./task-model.md) | Task lifecycle, structure, and single-responsibility pattern | 🚧 |
| [Validation Model](./validation.md) | Fluent validation API and execution flow | 🚧 |

### Plugin Model Summary

Plugins are **orchestration layers** that register tasks against Dataverse messages. They follow one of two patterns:

| Pattern | Scope | Example |
|---|---|---|
| **Entity-based** | Single entity lifecycle | `ContactPlugin` — handles Create/Update on `contact` |
| **Functionality-based** | Business capability | `TaskPlugin` — handles Create/Update on `task` |

Plugins inherit from `PluginBase` and register tasks in the constructor:

---

### Task Model Summary

Tasks inherit from `TaskBase<TEntity>` and implement single-responsibility business logic. Each task provides:

- **`AddValidations()`** — Fluent validation chain (mode → stage → messages → entity → attributes)
- **`DoExecute()`** — Business logic only (no guard clauses)

Built-in services available in every task:

| Service | Purpose |
|---|---|
| `DataServiceProvider.Admin` / `.User` | Dataverse CRUD with security context separation |
| `SettingService` | Runtime configuration from `pl_setting` entity (cached) |
| `LogService` | Structured logging to `pl_log` entity |
| `TracingService` | Dataverse tracing service |
| `ContextEntity` / `PreImage` / `PostImage` | Automatic entity context initialization |

### Fluent Validation Summary

Validations use a chainable API executed **before** `DoExecute()`:

Validation failures set `TaskStatus.NotValid` and skip execution — no exception is thrown unless `DataverseValidationException` is explicitly raised.

---

## ⚙️ Execution & Runtime

How the framework behaves during plugin execution in Dataverse.

| Document | Description | Status |
|---|---|---|
| [Execution Pipeline](./execution-pipeline.md) | Task orchestration, ordering, and execution flow | 🚧 |
| [Runtime Configuration](./configuration.md) | Configuration stored in Dataverse, loaded at runtime | 🚧 |

---

### Execution Pipeline

The framework enforces a deterministic execution model aligned with the Dataverse plugin runtime.

All logic is executed strictly within a single `Execute` method and its execution order (execution index). This is a fundamental assumption for correct plugin behavior.

To support task-based architecture, the framework provides a custom `PluginBase` that:
- dynamically instantiates and executes individual Tasks within the `Execute` method
- ensures all Tasks share a single, consistent execution context

Each Task receives the same `TaskContext`, which:
- encapsulates Dataverse services and execution data
- provides a shared `Items` collection for cross-task communication
  - `AddItem(key, value)`
  - `GetItem<T>(key)`

This enables lightweight data sharing between Tasks during a single execution while keeping them loosely coupled.

As a result, the pipeline ensures:
- strict control over execution flow  
- consistent context across all Tasks  
- modular and testable business logic  
- safe and predictable intra-execution communication  

---

### Runtime Configuration

The `SettingsService` reads key-value pairs from the `pl_setting` Dataverse table. Supported value types: Text, JSON, Integer, Boolean, Decimal, DateTime. Values are cached in-memory with configurable TTL (default: 60s).

---

## 🔢 Autonumbering

Configurable sequence generation built into the framework.

| Document | Description | Status |
|---|---|---|
| [Autonumbering](./autonumbering.md) | Sequence configuration, parent-based numbering, and concurrency handling | 🚧 |

### Overview

`AutoNumberingService` generates formatted sequence numbers stored in `pl_autonumbering` records. Features:

- **Format tokens**: `{NUM}`, `{date1}`, `{date2}`, `{date3}`, `{grouping}`, `{attributeName}`
- **Parent-based numbering**: Separate sequences per parent entity lookup
- **Grouping**: Separate sequences per grouping value (e.g., year)
- **Concurrency**: Uses `RowVersion` with `ConcurrencyBehavior.IfRowVersionMatches`
- **Transactional**: Returns an `UpdateRequest` for the caller to execute within the plugin transaction

Example usage from `TaskAutoNumbering`:

---

## 🔍 Observability

Monitoring, debugging, and diagnosing execution behavior.

| Document | Description | Status |
|---|---|---|
| [Logging](./logging.md) | Diagnostic logging and execution tracing | 🚧 |
| [Error Handling](./error-handling.md) | Validation failures, warnings, and exception handling | 🚧 |

### Logging

`LogService` persists structured logs to the `pl_log` entity with related `pl_logdetail` records. Log severity levels: `Debug`, `Info`, `Warning`, `Error`, `Fatal`. A minimum severity threshold is configurable via the `MinimalSeverityLevel` setting key.

Each task execution automatically produces a log entry containing:
- Execution elapsed time, task name, correlation ID
- Input/Output parameters and Pre/Post entity images
- Validation messages and execution messages

### Error Handling

| Exception | Behavior |
|---|---|
| `DataverseValidationException` | Surfaces message to user, logged as `Info` severity |
| `InvalidPluginExecutionException` | Logged as `Error`, re-thrown to Dataverse |
| Other `Exception` | Logged as `Error`, wrapped in `InvalidPluginExecutionException` |

---

## 🧪 Testing

How to verify behavior against real Dataverse environments.

| Document | Description | Status |
|---|---|---|
| [Testing Overview](./testing.md) | Testing principles and approach | 🚧 |
| [Integration Testing](./integration-testing.md) | Working with Dataverse environments | 🚧 |

### Testing Architecture

- **Integration tests only** — no mocks, no fakes
- **Framework**: xUnit v3 on .NET 10
- **DI**: Autofac with `TestAutofacModule`
- **Base class**: `TestBase<TAutofacModule>` provides `DataService`, `OrganizationService`, `ConnectionService`
- **Cleanup**: Automatic via `IDisposable` — test entities are deleted after each test
- **Configuration**: `appsettings.json` / `appsettings.Development.json` for connection strings

Key rule: all Dataverse operations in tests MUST go through `DataService` — never resolve `IOrganizationService` directly.

---

## 📦 Packaging & Deployment

How to prepare, sign, and deploy plugin assemblies.

| Document | Description | Status |
|---|---|---|
| [Packaging and Deployment](./packaging-and-deployment.md) | Signing, assembly structure, and deployment process | 🚧 |
| [Versioning](./VERSIONING.md) | Versioning strategy and release model | ✅ |
| [Changelog](../CHANGELOG.md) | Release notes and changes per version | ✅ |

### Assembly Structure

The deployable plugin is a **single signed assembly** produced via ILMerge:

1. **Logic project** (`Examples.Logic`) — Plugins, Tasks, Features, Early-bound types
2. **Plugin project** (`Examples.Plugins`) — References Logic; post-build merges all dependencies into one signed DLL
3. **Signing**: Strong-name key (`key.snk`) applied during ILMerge
4. **Registration**: SPKL with `CrmPluginRegistration` attributes

> ⚠️ Do **not** modify the ILMerge setup. It is intentional and required for Dataverse deployment.

---

## ⚠️ Known Limitations

### Microsoft.CrmSdk.CoreTools maximum version

> [!WARNING]
> The maximum supported version of `Microsoft.CrmSdk.CoreTools` is **9.1.0.92**. Upgrading to a higher version will break early-bound entity generation via SPKL (`CrmSvcUtil.exe`). Do **not** update this package beyond the specified version.

This constraint applies to any project that uses SPKL for early-bound type generation (e.g., `Pillaro.Dataverse.PluginFramework.Tests.EarlyBoundGen`).

### Target Framework

All plugin assemblies **must** target **.NET Framework 4.6.2**. This is a Dataverse platform requirement. Test projects may target modern .NET.

---

## 💡 Examples

The `examples/` folder contains working implementations demonstrating framework patterns.

### Plugins

| Plugin | Entity | Pattern | Stages |
|---|---|---|---|
| `ContactPlugin` | `contact` | Entity-based | PreValidation, PreOperation |
| `TaskPlugin` | `task` | Entity-based | PreOperation, PostOperation |

### Tasks

| Task | Entity | Stage | Purpose |
|---|---|---|---|
| `ValidateContactNamesTask` | `contact` | PreValidation | Rejects forbidden first/last names using `SettingsService` |
| `UpdateAddressLabel` | `contact` | PreOperation | Composes `Address1_Name` from address fields with PreImage merge |
| `TaskAutoNumbering` | `task` | PreOperation | Generates sequential subject prefix via `AutoNumberingService` |
| `TaskSummarySync` | `task` | PostOperation | Recalculates parent contact description from related tasks |

### Features

| Feature | Description |
|---|---|
| `CustomerForbiddenNameService` | Reads forbidden word list from `SettingsService` JSON configuration |

---

## 🤝 Contributing

The repository maintains contribution policies and community guidelines at the repository root. Please review these documents before opening issues or pull requests.

| Document | Description | Link | Status |
|---|---|---|---|
| Contributing guide | How to contribute, PR process, and development setup | [CONTRIBUTING.md](CONTRIBUTING.md) | ✅ |
| Security policy | How to report vulnerabilities and our disclosure process | [SECURITY.md](SECURITY.md) | ✅ |
| Code of Conduct | Community behavior expectations and reporting | [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) | ✅ |

---

## 🗺️ Recommended Reading Path

If you are new to the framework:

1. Start with **Getting Started** and **Architecture**
2. Review the **Examples** section above for concrete patterns
3. Study `ContactPlugin` → `ValidateContactNamesTask` → `UpdateAddressLabel` as a complete flow
4. Explore **Autonumbering** and **Observability** for advanced features

If you are implementing a solution:

1. Review **Packaging & Deployment** and **Known Limitations**
2. Set up **Testing** with `TestBase` and `DataService`
3. Use **Logging** and **Error Handling** for diagnostics
