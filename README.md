# Pillaro Dataverse Plugin Framework

Structured framework for building Microsoft Dataverse plugins with a task-based execution model.

It is intended for teams that need predictable plugin behavior, clear separation of responsibilities, and a maintainable structure for long-term development.

> [!IMPORTANT]
> This repository should help you answer one question quickly:
> **Is this framework a good fit for my Dataverse plugin solution?**

> [!NOTE]
> Status: Prerelease

---

## 📑 Navigation

- [🔍 What this framework is](#-what-this-framework-is)
- [🎯 Why use it](#-why-use-it)
- [📈 Added value by project type](#-added-value-by-project-type)
- [⚠️ What you need to know before adopting it](#-what-you-need-to-know-before-adopting-it)
- [🧭 Quick decision guide](#-quick-decision-guide)
- [🚀 Quick start](#-quick-start)
- [📚 Documentation](#-documentation)
- [🗂️ Repository structure](#-repository-structure)
- [📌 Current status](#-current-status)
- [📄 License](#-license)
---

## 🔍 What this framework is

Pillaro Dataverse Plugin Framework is a source-available framework for C# Dataverse plugin development.

It provides:

- task-based plugin composition
- explicit separation of validation and execution
- consistent plugin structure across solutions
- shared execution context for tasks
- built-in logging support
- runtime configuration from Dataverse
- a deployment model aligned with Dataverse plugin constraints

This framework does not remove Dataverse complexity. It gives that complexity a stable and understandable structure.

---

## 🎯 Why use it

Use this framework when standard Dataverse plugin code starts becoming inconsistent, hard to test, and difficult to maintain.

Typical problems it addresses:

- plugin classes growing into large mixed-responsibility files
- validation and execution logic being mixed together
- repeated patterns across multiple plugins and projects
- low visibility into runtime behavior
- weak long-term maintainability as business logic grows

The framework enforces a simple mental model:

- **Plugin** = orchestration layer
- **Task** = one unit of business logic
- **Validation** = explicit preconditions
- **Execution** = business logic only

---

## 📈 Added value by project type

This framework is useful across small, medium, and large Dataverse plugin solutions.

The difference is not whether the framework fits.  
The difference is **how much value it adds** in a given context.

### 🚀 Higher added value

The framework brings the highest value when:

- the solution contains multiple plugins or integration points
- the project is expected to grow over time
- business logic is becoming more complex
- maintainability matters across multiple developers or teams
- structured logging and runtime diagnostics are important
- consistent architecture is needed across projects
- testing and long-term scalability are part of the delivery approach

### ⚖️ Lower added value, still worth using

The framework still brings value even in smaller or simpler solutions because the overhead is low and the structure remains useful.

Typical examples:

- a small plugin with limited business logic
- a short-term or isolated customization
- an early project phase before complexity grows
- a solution where logging and structure are still useful even if advanced patterns are not yet needed

Even in these cases, the framework still provides benefits such as:

- a clean and predictable structure from the start
- built-in logging support
- a consistent execution model
- easier future growth if the solution expands later

### Summary

There is generally no architectural reason to avoid the framework for small plugins.

For larger and more complex solutions, its added value is higher.  
For smaller solutions, the added value is lower, but still real.

---

## ⚠️ What you need to know before adopting it

Using this framework does not change the standard technical prerequisites for Dataverse plugin development.

What is specific to this framework:

- the framework solution must be imported into Dataverse before framework runtime features work
- the recommended structure separates business logic, deployable plugin assembly, and tests

> [!NOTE]
> Standard Dataverse plugin prerequisites remain the same whether you use this framework or not.

---

## 🧭 Quick decision guide

This framework is useful across small, medium, and large Dataverse plugin solutions.

The difference is not whether the framework fits.  
The difference is **where it brings the most value** for your work.

### 👨‍💻 For developers

The framework brings immediate value during day-to-day plugin development.

It helps when you want:

- built-in logging without repeatedly implementing your own logging patterns
- faster issue analysis without relying on plugin debugging as the primary tool
- clear visibility into execution flow, validation results, and runtime behavior
- reusable helpers and prepared services instead of copy-pasting the same infrastructure code
- a structured way to implement business logic in small isolated tasks

This usually means:

- faster plugin development
- less time spent debugging
- less repeated boilerplate code
- easier onboarding for new developers

### 🧪 For testers and developers writing programmatic tests

The framework also brings value when you want programmatic testing against Dataverse.

It helps when you want:

- a prepared testing package for programmatic integration tests
- business logic structured into tasks that can be tested in isolation
- a cleaner path for verifying behavior without relying only on manual testing
- a repeatable test approach for plugin behavior

Even if a team does not fully adopt the architectural style everywhere, the testing project and testing support can still provide value on their own.

### 🏗️ For architects and long-term solution design

The added value grows further when the solution becomes larger or is expected to evolve over time.

It helps when you want:

- a consistent plugin architecture across teams and projects
- a structure that remains understandable as business logic grows
- better scalability for larger plugin-based solutions
- long-term maintainability instead of short-term plugin-by-plugin growth
- a shared development model that reduces architectural drift

### ⚙️ For solution delivery speed

The framework also speeds up implementation because useful building blocks are already prepared.

It includes support for areas such as:

- runtime settings
- autonumbering
- logging
- prepared services and helpers
- shared execution context and common infrastructure patterns

This reduces the need to rebuild the same technical foundation in every project.

### Summary

This framework is worth using even for smaller plugins because the overhead is low and the practical value starts immediately.

For small solutions, the value is mainly in:

- faster development
- built-in logging
- prepared infrastructure
- better structure from the start

For larger or growing solutions, the value becomes much higher because the same structure also improves:

- maintainability
- scalability
- consistency across the codebase
- long-term architecture quality
---

## 🚀 Quick start

### 1. Import the framework solution into Dataverse

Import the framework solution from:

    power-platform-solutions/framework

This installs the Dataverse components required by the framework.

### 2. Create the recommended solution structure

    YourSolution/
    ├── YourSolution.Logic/
    ├── YourSolution.Plugins/
    └── YourSolution.Tests/

- `YourSolution.Logic` → business logic, tasks, plugin classes
- `YourSolution.Plugins` → final deployable plugin assembly
- `YourSolution.Tests` → integration tests

### 3. Target the correct frameworks

- `YourSolution.Logic` → `.NET Framework 4.6.2`
- `YourSolution.Plugins` → `.NET Framework 4.6.2`
- `YourSolution.Tests` → modern .NET

### 4. Create your solution `PluginBase`

    namespace YourSolution.Logic.Plugins
    {
        public class PluginBase : PluginFramework.Plugins.PluginBase
        {
            public PluginBase(string unsecureConfig, string secureConfig)
                : base(unsecureConfig, secureConfig)
            {
            }

            public override string GetSolutionVersion()
            {
                return "1.0.0";
            }
        }
    }

### 5. Create a plugin and register a task

    using Pillaro.Dataverse.PluginFramework.Plugins;
    using YourSolution.Logic.Tasks.Task;

    namespace YourSolution.Logic.Plugins
    {
        public class TaskPlugin : PluginBase
        {
            public TaskPlugin(string unsecureConfig, string secureConfig)
                : base(unsecureConfig, secureConfig)
            {
                RegisterTask<TaskAutoNumbering>(
                    PluginStage.Preoperation,
                    ["Create"],
                    Task.EntityLogicalName,
                    PluginMode.Synchronous);
            }
        }
    }

### 6. Implement the task

    using Pillaro.Dataverse.PluginFramework.Plugins;
    using Pillaro.Dataverse.PluginFramework.Tasks;
    using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
    using System;

    namespace YourSolution.Logic.Tasks.Task
    {
        public class MyFirstTask(IServiceProvider serviceProvider, TaskContext taskContext)
            : TaskBase<Logic.Task>(serviceProvider, taskContext)
        {
            protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
            {
                return validator
                    .WithMode(PluginMode.Synchronous)
                    .WithStage(PluginStage.Preoperation)
                    .WithMessages(["Create"])
                    .ForEntity(ContextEntity.LogicalName);
            }

            protected override void DoExecute()
            {
                // Business logic
            }
        }
    }

### 7. Use the prepared post-build resources from the installed package

For the multi-project setup, the package provides prepared resources for merging the plugin and logic outputs into a single deployable assembly.

After installing the package into the `Plugins` project, the following folders are available in that project:

    tools/CrmTools/
    tools/ILMerge/

These resources are added by the package and include:

- prepared post-build action templates in `tools/CrmTools/`
- ILMerge binaries in `tools/ILMerge/`

For the standard logic + plugin project setup, use:

    tools/CrmTools/PostBuildAction-logic_plugin-projects.txt

This prepared post-build action is intended for merging:

- the `Plugins` project output
- the referenced `Logic` project output

into one final deployable DLL for Dataverse.

> [!IMPORTANT]
> The final deployment artifact is one DLL, even though development is split into separate projects for logic, deployment, and tests.

> [!NOTE]
> The `Plugins` project must be configured for assembly signing.
> The final assembly must be strong-name signed, and `key.snk` must exist in the root of the `Plugins` project so the prepared post-build action can use it.

For the full setup, see [Getting Started](./docs/getting-started.md).

---

## 📚 Documentation

Start with these documents:

- [Getting Started](./docs/getting-started.md)
- [Full Documentation](./docs/README.md)

---

## 🗂️ Repository structure

    /src
      /Pillaro.Dataverse.PluginFramework
      /Pillaro.Dataverse.PluginFramework.Plugins
      /Pillaro.Dataverse.PluginFramework.Testing

    /tests
      /Pillaro.Dataverse.PluginFramework.Tests
      /Pillaro.Dataverse.PluginFramework.Tests.EarlyBoundGen

    /examples
      /Pillaro.Dataverse.PluginFramework.Examples.Logic
      /Pillaro.Dataverse.PluginFramework.Examples.Plugins
      /Pillaro.Dataverse.PluginFramework.Examples.Tests

    /docs
    /power-platform-solutions

---

## 📌 Current status

This repository is in prerelease state.

What to expect:

- the core framework structure is in place
- the deployment model is intentional and opinionated
- documentation is being tightened for public use
- some areas may still evolve before the first stable release

---

## 📄 License

This project is licensed under the **Pillaro Community License (PCL) v1.0**.

At a glance, the license allows you to:

- use the framework in commercial projects
- modify it for your own needs
- share it as part of your solution or delivery
- charge for implementation, customization, and related services

The main restriction is that you must not resell, repackage, or present the framework itself as the main product or primary commercial value.

> [!NOTE]
> This is only a practical summary for quick orientation.
> See [LICENSE](./LICENSE) for the full license text.