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
| PF-DATA-008 | Wherever an API takes an attribute name as a string (validation chain, `ColumnSet`, indexers), use `Entity.Fields.X`; in `Register(...)` use typed selectors (PF-REG-007). `nameof(...)` as an attribute name is NEVER correct — it returns the C# property name, not the logical name, and the mismatch fails silently at runtime. | decision D2 |
| PF-DATA-009 | Until early-bound types exist for an entity (the default state of a brand-new project), use logical names as string literals. Switch to `Fields` constants once the type exists. | decision D2 |
| PF-DATA-010 | Writes that belong to the current operation go through `TaskContext.AddEntityToUpdate(entity, ServiceUser)`, not a direct `Update(...)`. Pick `ServiceUser` like a provider context (`User` default; `InitiatingUser` when the audit must show the person, e.g. post-operation Delete; `Admin` with a justification). | [task-model.md](../../plugins/task-model.md) |

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

## Writing: queue, don't update (PF-DATA-010)

```csharp
// ✅ Queued — merged with what other tasks queue for the same record, written once after all tasks succeed.
// In PreValidation/PreOperation, a queued change to the record being saved is merged into the target:
// no extra write, no re-triggered steps.
TaskContext.AddEntityToUpdate(new Logic.Account { Id = accountId, EMailAddress1 = email });
```

```csharp
// ❌ Direct update of a record other tasks on the same step may also change — written several times,
// re-triggers the account's own steps each time, and is not rolled back if a later task fails
OrganizationServiceProvider.User.Update(new Logic.Account { Id = accountId, EMailAddress1 = email });
```

Use `OrganizationServiceProvider` / `DataServiceProvider` directly for reads, creates, deletes and
anything deliberately written outside the plugin transaction. `TaskContext.GetActualEntityToUpdate(...)`
returns what earlier tasks queued, when a later task has to build on it.
`examples/…/Tasks/Contact/ArchiveDeletedContact.cs` and `RecordJobTitleChange.cs` are the reference.

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

## ⚠️ Early-bound types fail only at runtime? Check the proxy-types attribute first (PF-BUILD-006)

If a task that uses early-bound types builds cleanly but fails **inside Dataverse** with either of
these, the cause is almost certainly the plugin assembly, not your task code:

```text
The specified type 'YourSolution.Logic.Contact' is not a known entity type.
```

```text
SerializationException: ... contains data from a type that maps to the name
'YourSolution.Logic:Account'. The deserializer has no knowledge of any type that maps to this name.
```

**Cause:** the sandbox resolves early-bound types only from an assembly marked with
`[assembly: Microsoft.Xrm.Sdk.Client.ProxyTypesAssemblyAttribute]`. `pac modelbuilder` puts that
attribute into the `Logic` project, but ILMerge keeps only the primary (`Plugins`) assembly's
attributes, so the merged, deployed DLL loses it. Both LINQ reads (`DataServiceProvider.X.Query<T>()`)
and early-bound writes (`Update(new Logic.Account { ... })`) then fail.

**Fix:** declare the attribute on the `Plugins` project (the template does this since this fix; older
projects may not):

```xml
<ItemGroup>
  <AssemblyAttribute Include="Microsoft.Xrm.Sdk.Client.ProxyTypesAssemblyAttribute" />
</ItemGroup>
```

Verified end-to-end: the same early-bound task failed with both errors above and passed 5/5
integration tests after adding only this attribute, with no code change. Check a built DLL with:

```powershell
[Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes("bin\Release\YourSolution.Plugins.dll")).Contains('ProxyTypesAssemblyAttribute')
```

```csharp
// ❌ Never "fix" it by rewriting writes to late-bound — that routes around a build defect
// (hard boundary 2 in AGENTS.md) and leaves every other early-bound call in the solution broken.
var update = new Entity("account", ContextEntity.Id);
update["emailaddress1"] = email;
```

## ➡️ Related

- [Data Access](../../plugins/data-access.md)
- [Early-Bound Entity Generation](../../plugins/early-bound-generation.md)
- [60-registration.md](./60-registration.md) — typed vs. string attribute selection at registration time
