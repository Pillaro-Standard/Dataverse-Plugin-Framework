# Data Access

> [!IMPORTANT]
> Tasks already have prepared access to Dataverse services.
> You do not need to bootstrap data access manually in every task.

---

## 📑 Navigation

- [🔍 What this document covers](#-what-this-document-covers)
- [🧰 Prepared access in `TaskBase`](#-prepared-access-in-taskbase)
- [🧱 Two access layers](#-two-access-layers)
- [👤 Security contexts](#-security-contexts)
- [🧭 How to choose the right context](#-how-to-choose-the-right-context)
- [📦 `DataServiceProvider`](#-dataserviceprovider)
- [💡 Why `DataService` matters](#-why-dataservice-matters)
- [💻 Example](#-example)
- [✅ Design recommendations](#-design-recommendations)
- [➡️ Related documents](#️-related-documents)

---

## 🔍 What this document covers

This document explains the access model used by tasks when working with Dataverse data.

Its purpose is to explain:

- what access is already prepared in `TaskBase`
- how `OrganizationServiceProvider` and `DataServiceProvider` are used
- what the available security contexts mean
- how to choose the right context for a given operation

This document does **not** describe the full `DataService` API in detail.
That is a separate topic.

---

## 🧰 Prepared access in `TaskBase`

When you inherit from `TaskBase<TEntity>`, the framework already prepares the core data access services during task construction.

The most important access-related members are:

| Member | Purpose |
|---|---|
| `OrganizationServiceProvider` | Prepared access to Dataverse organization services |
| `DataServiceProvider` | Prepared access to framework data services |
| `TaskContext` | Shared execution context for the current task run |
| `ContextEntity` | Current entity target for supported message flows |
| `PreImage` | Pre-image when available |
| `PostImage` | Post-image when available |
| `ContextEntityReference` | Reference to the current primary entity record |

This means a task can start working with Dataverse data immediately, without repeating service factory setup in every implementation.

> [!IMPORTANT]
> In the framework runtime model, data access is already part of the prepared task surface.
> Tasks should use the provided access model instead of rebuilding service access repeatedly.

---

## 🧱 Two access layers

The framework gives tasks two access layers.

### `OrganizationServiceProvider`

This is the prepared access point for direct `IOrganizationService` usage.

Use it when:

- you need direct Dataverse organization service access
- you are working with APIs that require `IOrganizationService`
- you intentionally want the lower-level Dataverse service layer

This is the low-level access model.

### `DataServiceProvider`

This is the prepared access point for the framework data layer.

Use it when:

- you want to work with the framework data access model
- you want prepared helpers and extensions over standard Dataverse service access
- you want to use `DataService` instead of building repetitive access code yourself

This is not just a generic high-level wrapper.
It is the framework data layer built on top of standard organization service access.

> [!NOTE]
> In normal framework usage, `DataServiceProvider` is usually the more practical option for everyday task development.
> `OrganizationServiceProvider` remains available when direct organization service access is needed.

---

## 👤 Security contexts

The prepared providers expose three main security contexts.

### `User`

Represents the plugin execution user.

This is the standard user context for the running plugin step.

### `InitiatingUser`

Represents the original user who initiated the operation.

This is useful when the distinction between the current plugin user and the original initiator matters.

### `Admin`

Represents elevated access using the admin-level service context.

This is useful when the task intentionally needs access beyond the standard execution user context.

> [!IMPORTANT]
> These contexts are available through the prepared providers.
> The framework does not force one context for everything.
> It gives you explicit access to the appropriate execution identity.

---

## 🧭 How to choose the right context

Use the least privileged context that matches the business need.

### Use `User` when:

- the operation should respect the current plugin execution user
- normal business behavior should follow standard user permissions
- there is no reason to elevate access

### Use `InitiatingUser` when:

- the original initiator matters for the business behavior
- execution identity and initiating identity are not the same concern
- the business rule is tied to who originally triggered the operation

### Use `Admin` when:

- elevated access is intentionally required
- the task must perform work that should not depend on the current user’s permissions
- the logic is designed to run with system-level access

> [!WARNING]
> `Admin` should not become the default choice.
> Use it intentionally and only when the task design actually requires elevated access.

---

## 📦 `DataServiceProvider`

`DataServiceProvider` gives access to prepared `DataService` instances for the main framework contexts.

Available members:

| Member | Purpose |
|---|---|
| `DataServiceProvider.User` | `DataService` in the plugin user context |
| `DataServiceProvider.InitiatingUser` | `DataService` in the initiating user context |
| `DataServiceProvider.Admin` | `DataService` in the admin context |
| `DataServiceProvider.ForUser(Guid userId)` | `DataService` for a specific user context |

This makes it easy to choose the right access context while still working in the framework data model.

> [!NOTE]
> `DataServiceProvider` is built on top of the prepared organization service access.
> It gives you the same context model, but through the framework data layer.

---

## 💡 Why `DataService` matters

`DataService` provides prepared helpers and extensions over standard `IOrganizationService` usage.

Its practical value is not limited to one kind of operation.
It includes areas such as:

- query
- multiple request batching
- transactional operations
- metadata and option set access
- low-level record loading helpers

The most important day-to-day value is usually **query**.

~~~
var relatedTasks = DataServiceProvider.Admin
     .Query<Logic.Task>()
     .Where(t => t.RegardingObjectId.Id == regarding.Id)
     .Select(t => new Logic.Task
     {
         ActivityId = t.ActivityId,
         StateCode = t.StateCode,
         ScheduledEnd = t.ScheduledEnd,
         ActualEnd = t.ActualEnd
     }).ToList();
~~~

That is the part most developers use most often because it reduces repetitive setup and makes Dataverse querying more convenient inside tasks.

The other areas are still useful, but they are usually more situational than query.

> [!IMPORTANT]
> This document only explains how `DataService` becomes available in tasks.
> The detailed `DataService` API and query model should be documented separately.

---

## 💻 Example

Example of prepared access inside a task:

    protected override void DoExecute()
    {
        var userData = DataServiceProvider.User;
        var adminData = DataServiceProvider.Admin;

        var userService = OrganizationServiceProvider.User;
        var initiatingUserService = OrganizationServiceProvider.InitiatingUser;

        AddLogMessageLine("Prepared data access services are available.");
    }

This example shows:

- direct access to `DataServiceProvider`
- direct access to `OrganizationServiceProvider`
- explicit choice of security context
- no repeated service factory setup in the task itself

---

## ✅ Design recommendations

Use these rules by default:

- prefer the prepared providers over manual service bootstrapping
- default to the least privileged context that matches the requirement
- use `DataServiceProvider` for normal framework-style data access
- use `OrganizationServiceProvider` when direct `IOrganizationService` access is intentionally needed
- make the chosen context explicit in task code
- avoid hiding elevated access behind generic helper logic

When reviewing a task, it should be easy to answer:

- which context is being used
- why that context is appropriate
- whether elevated access is really necessary

> [!TIP]
> Keep the intent visible.
> A reader of the task should immediately understand whether the logic runs as `User`, `InitiatingUser`, or `Admin`.

---

## ➡️ Related documents

Continue with:

- [Task Model](./task-model.md)
- [Validation Model](./validation.md)
- [Getting Started](./getting-started.md)
- [Architecture](./architecture.md)
- [DataService](./data-service.md)
