# Plugin Model

> [!IMPORTANT]
> In this framework, a plugin is the orchestration layer.
> It is the Dataverse entry point that registers and runs tasks.
> It should stay small and focused.

---

## 📑 Navigation

- [🔍 What a plugin is](#-what-a-plugin-is)
- [🎯 Responsibilities of a plugin](#-responsibilities-of-a-plugin)
- [📦 What belongs in a plugin](#-what-belongs-in-a-plugin)
- [🚫 What should not be in a plugin](#-what-should-not-be-in-a-plugin)
- [🧭 Plugin organization patterns](#-plugin-organization-patterns)
- [🔗 Relationship between plugin and task](#-relationship-between-plugin-and-task)
- [💻 Example](#-example)
- [✅ Design recommendations](#-design-recommendations)
- [➡️ Related documents](#️-related-documents)

---

## 🔍 What a plugin is

A plugin is the framework entry point registered in Dataverse.

Its role is to:

- receive Dataverse execution
- define the orchestration boundary
- register the tasks that should run for that plugin
- keep business execution predictable and readable

In this framework, the plugin is not intended to become the main container for business logic.

That responsibility belongs to tasks.

---

## 🎯 Responsibilities of a plugin

A plugin is responsible for:

- acting as the Dataverse-facing entry point
- registering tasks through the framework
- defining which tasks belong to that execution boundary
- keeping orchestration explicit and readable
- inheriting from the shared solution `PluginBase`

A plugin should make it easy to understand:

- what execution it represents
- which tasks it runs
- how that execution is grouped in the solution

> [!NOTE]
> Registration details in Dataverse are part of your deployment process.
> In this framework, the important architectural point is that the plugin remains the entry point and the task remains the business unit.

---

## 📦 What belongs in a plugin

A plugin should contain:

- inheritance from your solution `PluginBase`
- task registration
- a clear orchestration boundary
- naming that reflects the purpose of the plugin
- minimal code outside registration and plugin setup

Typical contents of a plugin class:

- constructor
- one or more `RegisterTask<T>()` calls
- no heavy implementation logic

A plugin should stay easy to scan and easy to reason about.

---

## 🚫 What should not be in a plugin

A plugin should not become the place for:

- heavy business logic
- long conditional execution branches
- repeated validation code
- direct data processing logic that belongs in tasks
- unrelated responsibilities grouped into one class
- logic copied from one plugin to another

If a plugin grows beyond orchestration, the structure is moving in the wrong direction.

> [!WARNING]
> When business logic starts living in plugin classes, readability drops quickly and testability becomes harder.
> In this framework, business logic should move into tasks.

---

## 🧭 Plugin organization patterns

There is no single mandatory naming strategy, but two patterns are especially useful.

### Entity-oriented plugin

This is the most common pattern.

Examples:

- `ContactPlugin`
- `TaskPlugin`
- `OpportunityPlugin`

Use this when:

- the plugin clearly belongs to one entity lifecycle
- the registered tasks are centered around one Dataverse table
- the execution boundary is naturally tied to one entity

This is usually the clearest option for most solutions.

### Capability-oriented plugin

Use this when the plugin represents one business capability rather than one entity.

Examples:

- tax recalculation across several document entities
- shared business process logic across related records
- cross-entity behavior grouped by purpose rather than by table

Use this pattern when the functional boundary is clearer than the entity boundary.

> [!NOTE]
> Entity-oriented plugins are more common.
> Capability-oriented plugins are still valid when one business capability spans multiple entities.

---

## 🔗 Relationship between plugin and task

The framework separates orchestration from execution.

- **Plugin** = orchestration layer
- **Task** = executable business unit

A plugin can register:

- one task
- several tasks
- tasks that belong to one entity
- tasks that participate in a broader functional flow

This gives you a predictable model:

- the plugin stays small
- the tasks carry the business logic
- the execution flow remains readable
- individual units are easier to maintain and test

A good plugin tells you **what is being orchestrated**.  
A good task tells you **what business logic is being executed**.

---

## 💻 Example

Minimal example:

    using Pillaro.Dataverse.PluginFramework.PluginRegistrations;
    using Pillaro.Dataverse.PluginFramework.Plugins;

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

        public override void Register(IPluginRegistration registration)
        {
            registration
                .OnCreate<Task>("8c46d6e6-3c25-4b9d-9264-6c0d02b4d2f1")
                .PreOperation()
                .Synchronous()
                .WithName("My Custom Step Name")
                .Rank(1);
        }
    }

This example shows the intended shape:

- the plugin inherits from `PluginBase`
- deployment metadata is declared in `Register(IPluginRegistration registration)`
- the constructor registers tasks
- the plugin stays thin
- the business logic is delegated to the task

A plugin can also register multiple tasks when one execution flow requires several business steps.

---

## ✅ Design recommendations

Use these rules by default:

- keep plugins small
- keep business logic out of plugin classes
- use plugins for orchestration, not implementation
- prefer clear task registration over clever abstractions
- use names that reflect either entity scope or business capability
- keep one plugin understandable without opening five other files first

When in doubt:

- put orchestration in the plugin
- put business logic in the task
- put shared reusable logic into features or supporting services

---

## ➡️ Related documents

Continue with:

- [Getting Started](./getting-started.md)
- [Plugin Registration API](./plugin-registration-api.md)
- [Task Model](./task-model.md)
- [Validation Model](./validation.md)
- [Data Access](./data-access.md)
- [Architecture](./architecture.md)
- [Execution Pipeline](./execution-pipeline.md)
- [Deployment Plugins](./deployment-plugins.md)
