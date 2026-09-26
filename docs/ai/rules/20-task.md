# Tasks

> [!IMPORTANT]
> Read [`AGENTS.md`](../../../AGENTS.md) first. This file is one entry in its rule catalog.

| ID | Rule | Source |
|---|---|---|
| PF-TASK-001 | A task inherits `TaskBase<TEntity>` with an early-bound type. | [task-model.md](../../plugins/task-model.md) |
| PF-TASK-002 | One task = one business responsibility. Two unrelated operations = two tasks. | [task-model.md](../../plugins/task-model.md) |
| PF-TASK-003 | Conditions ONLY in `AddValidations()`; logic ONLY in `DoExecute()`. No guard `if`s at the top of `DoExecute()` that belong in validation. | [validation.md](../../plugins/validation.md) |
| PF-TASK-004 | NEVER bootstrap `IOrganizationService` by hand — use the provided providers. | [40-data-access.md](./40-data-access.md) |
| PF-TASK-005 | Use the shared `TaskContext` deliberately, not as a hidden cross-task dependency mechanism. | [execution-pipeline.md](../../plugins/execution-pipeline.md) |
| PF-TASK-006 | Reusable logic goes into `Features/`, not copy-pasted between tasks. | [task-model.md](../../plugins/task-model.md) |

## Canonical shape (primary constructor — PF's F2-02 decision)

```csharp
public class ValidateNames(IServiceProvider serviceProvider, TaskContext taskContext)
    : TaskBase<Logic.Contact>(serviceProvider, taskContext)
{
    protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
    {
        return validator
            .WithMode(PluginMode.Synchronous)
            .WithStage(PluginStage.Prevalidation)
            .WithMessages(["Create", "Update"])
            .ForEntity(ContextEntity.LogicalName)
            .EntityWithAtLeastOneAttribute(ContextEntity, Contact.Fields.FirstName, Contact.Fields.LastName)
            .WithValidation("First name or last name must be present.", x =>
                ContextEntity.Contains(Contact.Fields.FirstName) || ContextEntity.Contains(Contact.Fields.LastName));
    }

    protected override void DoExecute()
    {
        // business logic only — no re-checking of preconditions already covered above
    }
}
```

## Anti-pattern: guard clauses instead of validation

```csharp
// ❌ Wrong — this precondition belongs in AddValidations(), not as an early-return guard
protected override void DoExecute()
{
    if (ContextEntity.FirstName == null)
        return; // silently does nothing — no log line, no visibility, and it duplicates
                // logic that the validation chain already exists to express
    // ...
}
```

The validation chain exists specifically so a skipped task is visible (logged, filterable) instead
of silent. A guard clause at the top of `DoExecute()` throws that visibility away.

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
