# Task Model

> [!IMPORTANT]
> In this framework, a task is the main unit of business logic.
> A plugin orchestrates execution.
> A task performs the work.

---

## 📑 Navigation

- [🔍 What a task is](#-what-a-task-is)
- [🎯 Responsibilities of a task](#-responsibilities-of-a-task)
- [📦 What belongs in a task](#-what-belongs-in-a-task)
- [🧰 Prepared services and runtime context](#-prepared-services-and-runtime-context)
- [🚫 What should not be in a task](#-what-should-not-be-in-a-task)
- [🧭 Task scope patterns](#-task-scope-patterns)
- [🔗 Relationship between task, validation, and plugin](#-relationship-between-task-validation-and-plugin)
- [💻 Example](#-example)
- [⚠️ User-facing business failures](#-user-facing-business-failures)
- [✅ Design recommendations](#-design-recommendations)
- [➡️ Related documents](#-related-documents)

---

## 🔍 What a task is

A task is the executable business unit in the framework.

A task is used to:

- hold one focused piece of business logic
- define the conditions under which that logic can run
- execute that logic in a predictable way
- stay small enough to understand, maintain, and test

In practice, a task is where the actual plugin behavior is implemented.

The plugin remains the orchestration layer.  
The task carries the business responsibility.

---

## 🎯 Responsibilities of a task

A task is responsible for:

- implementing one clear business responsibility
- defining validation rules in `AddValidations()`
- executing business behavior in `DoExecute()`
- working inside the shared task execution context
- remaining understandable as a standalone unit

A good task should answer a simple question:

**What business action does this unit perform?**

Examples:

- validate contact names
- update address label
- calculate or assign values during record processing
- synchronize related data after a change
- perform one business rule in a larger execution flow

---

## 📦 What belongs in a task

A task should contain:

- one focused business responsibility
- validation setup in `AddValidations()`
- execution logic in `DoExecute()`
- use of the shared task context and framework services when needed
- naming that clearly reflects the purpose of the task

Typical structure of a task:

- class inheriting from `TaskBase<TEntity>`
- constructor receiving framework services
- `AddValidations()` for preconditions
- `DoExecute()` for business logic

A task should be small enough that its purpose is obvious without reading unrelated files.

---

## 🧰 Prepared services and runtime context

A task does not start from an empty runtime environment.

When you inherit from `TaskBase<TEntity>`, the framework already prepares the core runtime services and context data you typically need during task execution.

This reduces repeated setup code and gives you a consistent way to work inside plugin execution.

The most important prepared members are:

| Member | Purpose |
|---|---|
| `TaskContext` | Shared execution context for the current task run |
| `ContextEntity` | Current entity target for supported message flows |
| `PreImage` | Pre-image when available |
| `PostImage` | Post-image when available |
| `ContextEntityReference` | Reference to the current primary entity record |
| `TracingService` | Dataverse tracing support |
| `SettingService` | Runtime settings access |
| `OrganizationServiceProvider` | Prepared access to Dataverse organization services |
| `DataServiceProvider` | Prepared access to higher-level framework data services |
| `AddLogMessageLine(...)` | Adds a line to the task execution message stored in the task log |
| `AddLogDetail(...)` | Adds a structured detail record to the task log |

This means a task can focus on:

- validation
- business logic
- clear use of prepared framework services

instead of repeatedly bootstrapping service access and execution plumbing.

> [!IMPORTANT]
> `TaskBase<TEntity>` is not only an execution base class.
> It also provides the prepared runtime surface that tasks use during validation and execution.

> [!IMPORTANT]
> Each task has its own log context.
> For task-level execution logging, prefer the prepared task logging helpers:
>
> - `AddLogMessageLine(...)`
> - `AddLogDetail(...)`
>
> instead of writing all execution details directly through `LogService`.

`AddLogMessageLine(...)` appends a line into the task execution message.

Use it for:

- execution flow notes
- decision points
- business-relevant runtime messages
- short explanations of what the task actually did

`AddLogDetail(...)` adds a structured detail item to the task log.

Use it for:

- named values
- serialized objects
- payload snapshots
- diagnostic details that should be visible in the detailed log view of the application

> [!NOTE]
> Input parameters from the plugin context are already logged automatically.
> Use task-level logging helpers to add the task-specific details that explain the behavior of your task.

> [!NOTE]
> Data access, settings, service contexts, and task logging helpers are documented separately in dedicated documents.
> This section only explains that these services and helpers are already available in the task runtime model.

---

## 🧭 Task scope patterns

A task does not always have to mean one entity only.
The real boundary is the business responsibility.

### Entity-specific task

This is the most common pattern.

Examples:

- validating a contact field set
- updating a task subject
- composing an address label for a single entity

Use this when the business behavior clearly belongs to one Dataverse table.

### Capability-oriented task

Use this when one business capability applies across several entities.

Examples:

- VAT recalculation for quote, order, and invoice
- shared business validation across several document types
- common status handling for related records

Use this pattern when the responsibility is clearer than the entity boundary.

> [!NOTE]
> Most tasks are entity-specific.
> A task can still span multiple entities when one shared business capability is the real design boundary.

---

## 🔗 Relationship between task, validation, and plugin

The framework separates responsibilities clearly:

- **Plugin** = orchestration layer
- **Task** = business unit
- **Validation** = execution preconditions

The expected flow is:

1. the plugin registers one or more tasks
2. the task defines its conditions in `AddValidations()`
3. the task performs business logic in `DoExecute()`

This separation keeps the structure predictable:

- plugins stay small
- tasks stay focused
- validation is explicit
- execution logic stays cleaner

A good plugin tells you **what is being orchestrated**.  
A good task tells you **what business unit is being executed**.  
A good validation setup tells you **when that unit is allowed to run**.

---

## 💻 Example

Minimal example:

    public class MinimalTask(IServiceProvider serviceProvider, TaskContext taskContext)
        : TaskBase<Logic.Contact>(serviceProvider, taskContext)
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

This example shows the intended task shape:

- inherit from `TaskBase<TEntity>`
- keep validation in `AddValidations()`
- keep business logic in `DoExecute()`
- keep the responsibility focused

---

## ⚠️ User-facing business failures

Not every task failure should be treated as a technical error.

In some cases, the correct outcome is to stop execution and show a clear business message to the user.

For this scenario, use:

- `DataverseValidationException`

This is appropriate when:

- the outcome is an expected business validation failure
- the user should see a clear message
- the situation should not be treated as a system failure
- you do not want expected business behavior to appear as technical error handling

In this case, the task is logged as:

- `TaskStatus.NotValid`
- `LogSeverity.Info`

This makes it useful for normal business validation outcomes that should be visible to the user without being treated as unexpected runtime errors.

> [!IMPORTANT]
> Use `DataverseValidationException` for expected user-facing business failures.
> Do not use technical exceptions for scenarios that are part of normal business behavior.

---

## ✅ Design recommendations

Use these rules by default:

- keep one task responsible for one business concern
- use clear names that describe the actual business behavior
- move repeated reusable logic into shared services or features
- keep validation explicit and readable
- do not hide unrelated workflow steps inside one task
- split large flows into multiple tasks when that improves clarity

When deciding whether something should be a separate task, ask:

- does it have its own responsibility?
- would it be clearer if it had its own name?
- would it be useful to test it independently?
- would splitting it make the execution flow easier to understand?

If the answer is yes, it should probably be its own task.

---

## ➡️ Related documents

Continue with:

- [Getting Started](./getting-started.md)
- [Plugin Model](./plugin-model.md)
- [Validation Model](./validation.md)
- [Data Access](./data-access.md)
- [Architecture](../architecture.md)
- [Execution Pipeline](../execution-pipeline.md)
- [Data Access](./data-access.md)
- [DataService](./data-service.md)