# Tasks

> [!IMPORTANT]
> Read [`AGENTS.md`](../../../AGENTS.md) first. This file is one entry in its rule catalog.

| ID | Rule | Source |
|---|---|---|
| PF-TASK-001 | A task inherits `TaskBase<TEntity>` with an early-bound type. | [task-model.md](../../plugins/task-model.md) |
| PF-TASK-002 | One task = one business responsibility. Two unrelated operations = two tasks. | [task-model.md](../../plugins/task-model.md) |
| PF-TASK-003 | `AddValidations()` decides **whether** the task runs: message, stage, entity, the attributes it reacts to, required images, and preconditions without which it has nothing to do. `DoExecute()` does **what** the task is responsible for. No silent early `return` in `DoExecute()` for a case the chain should have filtered out. | [validation.md](../../plugins/validation.md) |
| PF-TASK-004 | NEVER bootstrap `IOrganizationService` by hand — use the provided providers. | [40-data-access.md](./40-data-access.md) |
| PF-TASK-005 | Use the shared `TaskContext` deliberately, not as a hidden cross-task dependency mechanism. | [execution-pipeline.md](../../plugins/execution-pipeline.md) |
| PF-TASK-006 | Reusable logic goes into `Features/`, not copy-pasted between tasks. | [task-model.md](../../plugins/task-model.md) |
| PF-TASK-007 | Name tasks verb-first by business intent, no `Task` suffix, no entity name (the `Tasks/<Entity>/` folder carries it). Use the verb vocabulary below. | observed in large production solutions |
| PF-TASK-008 | A task whose job is to reject (`Validate…`, `Restrict…`, `Check…`) is a normal task. Checking the business rule **is** its responsibility, so the check runs in `DoExecute()` and rejects with `DataverseValidationException` and the user's message; the task ends `Success`. The chain only decides when the check is needed — typically that the attributes it guards changed. `ThrowWithWarning(...)` at the end of the chain is an accepted alternative with the same outcome. Reference: `examples/…/Tasks/Contact/ValidateNames.cs`. | observed in large production solutions, `/examples` |
| PF-TASK-009 | On Update the target holds only changed columns. When the rule needs the full current state, read unchanged columns from the pre-image, and register that image with exactly those columns. | [task-model.md](../../plugins/task-model.md) |
| PF-TASK-010 | Validation predicates are pure: no field assignment, no `TaskContext.AddItem`, no writes. `DoExecute()` must not depend on which predicates happened to run. | observed anti-pattern |
| PF-TASK-011 | A task that writes to its own entity must not re-trigger itself: keep the columns it writes out of its step's filtering attributes, or reject nested runs with a depth validation. | — |
| PF-TASK-012 | Sync and Async variants of the same behavior share one implementation in `Features/`; the two tasks differ only in `WithMode(...)`. | observed anti-pattern |

## Naming vocabulary (PF-TASK-007)

The verb tells a reviewer — and a consultant reading the plan — what kind of task it is before they
open the file. Pick from this list; if nothing fits, the task probably has two responsibilities.

| Verb | Kind of task | Typical stage | `DoExecute()` |
|---|---|---|---|
| `Validate…` / `Restrict…` / `Check…` | Business rejection (PF-TASK-008) | PreValidation / PreOperation | checks the rule, throws `DataverseValidationException` when it is broken |
| `Set…` | Fill or derive a value on the record being saved | PreOperation | assigns `ContextEntity`, or queues the target (PF-DATA-010) |
| `Recalculate…` / `Update…` | Change **other** records derived from this one | PostOperation | `TaskContext.AddEntityToUpdate(...)` |
| `Create…` / `Assign…` | Create or reassign related records | PostOperation | explicit `Create(...)` / request |
| `Sync…` / `Send…` | Push data to an external system | PostOperation, usually Asynchronous | integration call |
| `Get…` | Answer a Custom API | MainOperation (PF-REG-012) | writes `OutputParameters` |

Examples: `SetNormalizedPhoneNumber`, `RestrictStatusChange`, `SyncCustomerToErp`. Not
`ContactPhoneTask`, not `PhoneNumberHandler`.

## A task that rejects (PF-TASK-008)

The validation chain answers "does this operation concern me?" — here: a contact update that
changes the status. Whether the change is allowed is the business rule the task exists for, so it is
checked in `DoExecute()`:

```csharp
public class RestrictStatusChange(IServiceProvider serviceProvider, TaskContext taskContext)
    : TaskBase<Logic.Contact>(serviceProvider, taskContext)
{
    protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
    {
        return validator
            .WithMode(PluginMode.Synchronous)
            .WithStage(PluginStage.Preoperation)
            .WithMessage("Update")
            .ForEntity(Logic.Contact.EntityLogicalName)
            .EntityWithAtLeastOneAttribute(ContextEntity, Logic.Contact.Fields.StateCode);
    }

    protected override void DoExecute()
    {
        if (!IsAdministrator())
            throw new DataverseValidationException("Only an administrator can deactivate a customer.");
    }
}
```

The difference from a guard clause (below): a guard silently skips a case the task does not concern;
this `if` **is** the task's business outcome, and the user sees it. The task ends `Success` with the
message in the log (PF-ERR-001).

## Current value on Update (PF-TASK-009)

```csharp
// ✅ Target first, pre-image for columns that did not change in this Update
var city = ContextEntity.Contains(Logic.Contact.Fields.Address1_City)
    ? ContextEntity.Address1_City
    : PreImage?.Address1_City;
```

The pre-image must be registered with every column read this way, and validated with
`HasPreImageWhen(x => x.Message == "Update")`. On Create the target is complete — no image needed.
`examples/…/Tasks/Contact/UpdateAddressLabel.cs` is the reference implementation.

## Pure predicates (PF-TASK-010)

```csharp
// ❌ Wrong — the predicate stores state that DoExecute() later relies on
.WithBreakValidation("Offer must exist.", x =>
{
    _offer = LoadOffer();          // side effect
    return _offer != null;
})
```

Load the data in `DoExecute()`. If the check itself needs the data, load it twice or move the lookup
into a `Features/` service with its own caching — never make execution depend on validation order.

## No self-triggering (PF-TASK-011)

There is no depth API on the validation chain; use the execution context directly:

```csharp
.WithValidation("Skipped: nested execution.", x => x.PluginExecutionContext.Depth <= 1)
```

Prefer the structural fix first — a task that sets `description` must not have `description` in its
own step's filtering attributes. Use the depth check only when the loop cannot be broken that way.

## Sync and Async twins (PF-TASK-012)

```csharp
// ✅ One implementation, two thin tasks — they differ only in validation and registration mode
public class SyncCustomerToErpSync(...)  : TaskBase<Logic.Contact>(...) { /* WithMode(Synchronous)  -> new CustomerErpSync(...).Run(...) */ }
public class SyncCustomerToErpAsync(...) : TaskBase<Logic.Contact>(...) { /* WithMode(Asynchronous) -> new CustomerErpSync(...).Run(...) */ }
```

Two near-identical 200-line copies drift apart within months — every fix lands in one and not the other.

## Canonical shape (primary constructor — PF's F2-02 decision)

Shortened from `examples/…/Tasks/Contact/ValidateNames.cs`:

```csharp
public class ValidateNames(IServiceProvider serviceProvider, TaskContext taskContext)
    : TaskBase<Logic.Contact>(serviceProvider, taskContext)
{
    // When: a contact is created or its first or last name changes.
    protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
    {
        return validator
            .WithMode(PluginMode.Synchronous)
            .WithStage(PluginStage.Prevalidation)
            .WithMessages(["Create", "Update"])
            .ForEntity(ContextEntity.LogicalName)
            .EntityWithAtLeastOneAttribute(ContextEntity, Logic.Contact.Fields.FirstName, Logic.Contact.Fields.LastName);
    }

    // What: the names must not be forbidden words — the task's responsibility.
    protected override void DoExecute()
    {
        var forbiddenWords = new CustomerForbiddenNameService(SettingService).GetForbiddenNames();

        if (ContextEntity.Contains(Logic.Contact.Fields.FirstName) && IsForbidden(forbiddenWords, ContextEntity.FirstName))
            throw new DataverseValidationException("First name is forbidden word, please write correct your first name");
        // ... the same for the last name
    }
}
```

## Anti-pattern: guard clauses instead of validation

```csharp
// ❌ Wrong — "this operation does not concern the task" belongs in AddValidations(), not in an early return
protected override void DoExecute()
{
    if (ContextEntity.FirstName == null)
        return; // silently does nothing — no log line, no visibility, and it duplicates
                // logic that the validation chain already exists to express
    // ...
}
```

The validation chain exists specifically so a skipped task is visible (logged, filterable) instead
of silent. A guard clause at the top of `DoExecute()` throws that visibility away. The test: an early
`return` that does nothing is a missing validation; a check that ends in a message for the user is
the task's work (PF-TASK-008).

## One task, one responsibility

```csharp
// ❌ Wrong — two unrelated responsibilities bolted onto one task
public class ContactMaintenance(...) : TaskBase<Logic.Contact>(...)
{
    protected override void DoExecute()
    {
        ValidateForbiddenNames();       // responsibility 1
        RecalculateAddressLabel();      // responsibility 2 — unrelated to the first
    }
}
```

Split into `ValidateNames` and `UpdateAddressLabel`. Each gets its own validation chain, its own
tests, and can be registered on a different stage/mode without the other being disturbed.

## ➡️ Related

- [30-validation.md](./30-validation.md) — the fluent chain in `AddValidations()`
- [40-data-access.md](./40-data-access.md)
- [70-testing.md](./70-testing.md)
