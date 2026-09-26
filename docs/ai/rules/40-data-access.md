# Data Access, Attribute Names, Early-Bound

> [!IMPORTANT]
> Read [`AGENTS.md`](../../../AGENTS.md) first. This file is one entry in its rule catalog.

| ID | Rule | Source |
|---|---|---|
| PF-DATA-001 | Default is `DataServiceProvider`; use `OrganizationServiceProvider` only when a direct `IOrganizationService` is required. | [data-access.md](../../plugins/data-access.md) |
| PF-DATA-002 | Lowest sufficient context. `Admin` is NEVER the default — it requires a code comment justifying it. | [data-access.md](../../plugins/data-access.md) |
| PF-DATA-003 | The chosen context MUST be visually obvious in code; do not hide it behind a helper. | [data-access.md](../../plugins/data-access.md) |
| PF-DATA-004 | Early-bound types are namespaced under `Logic.` (e.g. `Logic.Contact`). | [CONTRIBUTING.md](../../CONTRIBUTING.md) |
| PF-DATA-005 | NEVER write or edit files under `EarlyBound/`. `pac modelbuilder` generates them. | [early-bound-generation.md — File Ownership](../../plugins/early-bound-generation.md#file-ownership) |
| PF-DATA-006 | Early-bound classes live in the `Logic` project; generation runs from the `Logic` project root. This framework's own `src/` is not a layout example (see [00-architecture.md](./00-architecture.md)). | decision D1, [architecture.md](../../plugins/architecture.md) |
| PF-DATA-007 | A missing early-bound type or attribute means STOP and tell the developer what to generate. NEVER hand-write a partial class, NEVER fall back to late-bound access to route around it. | [early-bound-generation.md](../../plugins/early-bound-generation.md) |
| PF-DATA-008 | Attribute name is always `Entity.Fields.X`. `nameof(...)` as an attribute name is NEVER correct — it returns the C# property name, not the logical name, and the mismatch fails silently at runtime. | decision D2 |
| PF-DATA-009 | Until early-bound types exist for an entity (the default state of a brand-new project), use logical names as string literals. Switch to `Fields` constants once the type exists. | decision D2 |

## The two-tier attribute name rule (read this before writing your first task)

A freshly generated project from the template has **no early-bound types at all** — `ExampleTask`
uses `TaskBase<Entity>` (late-bound) and `ExamplePlugin` uses string-based registration. That is not
an edge case; it is the starting state of every new solution, until `pac modelbuilder` has been run.

```csharp
// ✅ Before early-bound types exist for this entity — string literal, exactly as Dataverse names it
.EntityWithAtLeastOneAttribute(ContextEntity, "firstname", "lastname")

// ✅ Once Logic.Contact exists (generated) — switch to the constant
.EntityWithAtLeastOneAttribute(ContextEntity, Contact.Fields.FirstName, Contact.Fields.LastName)

// ❌ Never — compiles, but "id" is not a real attribute; task becomes silently NotValid forever
.EntityWithAtLeastOneAttribute(ContextEntity, nameof(ContextEntity.Id))
```

`nameof(...)` is still fine for things that are *not* an attribute-name argument — a log message
label (`AddLogMessageLine($"Updating {nameof(ContextEntity.Address1_Name)}")`) or a test category
(`[Trait("Category", nameof(SummarySync))]`). The rule forbids `nameof` **as the value passed where an
attribute logical name is expected**, not `nameof` in general.

## Choosing an execution context

```csharp
// ✅ Lowest sufficient context, visible at the call site
var relatedTasks = DataServiceProvider.User.Query<Contact>()...;

// ⚠️ Admin — only with a comment explaining why user/initiating-user context is not sufficient
// Admin is required here because settings entity pl_setting is not readable by the calling user role.
var setting = DataServiceProvider.Admin.Query<PlSetting>()...;

// ❌ Admin as an unexamined default
var contact = DataServiceProvider.Admin.Query<Contact>()...; // no justification — reject
```

```csharp
// ❌ Hiding the context behind a helper — reviewer can no longer see which context is used
var contact = GetContact(id); // what context does GetContact use? Not visible without opening it.
```

## ➡️ Related

- [Data Access](../../plugins/data-access.md)
- [Early-Bound Entity Generation](../../plugins/early-bound-generation.md)
- [60-registration.md](./60-registration.md) — typed vs. string attribute selection at registration time
