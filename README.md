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

Detailed documentation, including setup guides and architecture explanations, is available in the [docs](./docs) section.

---

## What Problem It Solves

Standard plugin development often leads to:

| Problem | Framework Solution |
|---|---|
| Large classes with mixed responsibilities | Each piece of functionality = one task |
| Validation and execution logic combined | Validation is strictly separated from execution |
| Duplicated patterns across projects | Consistent structure enforced by base classes |
| Difficult testing and debugging | Built-in logging; tasks are independently testable |

### Example Scenarios

- **Updating related records after a change**
  → validation checks conditions, execution updates records

- **Complex business rules on create/update**
  → each rule is implemented as a separate task instead of one large plugin

- **Owner change validation with multiple rules**
  → each rule is a fluent validator, ordered from cheapest to most expensive

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

* **.NET Framework 4.6.2** (required by Dataverse plugin sandbox)
* Dataverse / Dynamics 365 environment
* Visual Studio
* Plugin must be deployed as a single assembly

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
public class MyPluginBase : PluginBase
{
    protected override void Configure(IPluginConfiguration config)
    {
        // solution-level tasks
        config.AddTask<SetSolutionNameTask>();
    }
}
~~~

### Plugin (one per entity)

Inherit from `PluginBase` and register plugin-specific tasks.

~~~csharp
public class AccountCreatePlugin : MyPluginBase
{
    protected override void Configure(IPluginConfiguration config)
    {
        base.Configure(config); // call base implementation

        config.AddTask<SetAccountNameTask>();
    }
}
~~~

### Task

Define validation and execution logic for a specific task.

~~~csharp
public class SetAccountNameTask : PluginTask
{
    public override void AddValidations()
    {
        Rule.For("Target")
            .MustExist()
            .WithMessage("Target entity is required");

        Rule.ForEntity("account")
            .Attribute("name")
            .MustBeEmpty()
            .WithMessage("Name must not be set");
    }

    public override void Execute()
    {
        var account = Context.Target.ToEntity<Account>();

        account.Name = "New Account";

        Service.Update(account);
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
/src        → framework source code
/tests      → test projects
/examples   → sample implementations
/docs       → documentation
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
| **Make execution flow explicit** | Logging is automatic; every step is traceable |
| **Maintain consistency** | All plugins follow the same base pattern |

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
