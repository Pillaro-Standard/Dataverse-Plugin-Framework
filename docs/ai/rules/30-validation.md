# Validation — the highest-value enforceable rule

> [!IMPORTANT]
> Read [`AGENTS.md`](../../../AGENTS.md) first. This file is one entry in its rule catalog.
> Full model: [Validation Model](../../plugins/validation.md).

| ID | Rule | Source |
|---|---|---|
| PF-VAL-001 | Chain order is FIXED: `WithMode` → `WithStage` → `WithMessage(s)` → `ForEntity(-ies)` → image checks → attribute checks → `WithValidation` → `WithBreakValidation` / `ThrowWith*`. | [validation.md](../../plugins/validation.md) |
| PF-VAL-002 | `WithValidation(...)` only for checks that do not query Dataverse. | [validation.md](../../plugins/validation.md) |
| PF-VAL-003 | Anything that reads from Dataverse MUST be in `WithBreakValidation(...)`, and last in the chain. | [validation.md](../../plugins/validation.md) |
| PF-VAL-004 | No "god predicate" — several named validations instead of one large lambda. | [validation.md](../../plugins/validation.md) |
| PF-VAL-005 | Every validation has a human-readable message (goes to the log and to the user). | [validation.md](../../plugins/validation.md) |
| PF-VAL-006 | Narrow with both registration filtering attributes AND validation — do not rely on only one. | [validation.md](../../plugins/validation.md), [execution-pipeline.md](../../plugins/execution-pipeline.md) |

## Canonical chain

```csharp
protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
{
    return validator
        .WithMode(PluginMode.Synchronous)
        .WithStage(PluginStage.Preoperation)
        .WithMessages(["Create", "Update"])
        .ForEntity(Contact.EntityLogicalName)
        .HasPreImageWhen(x => x.Message == "Update")
        .EntityWithAtLeastOneAttribute(ContextEntity, Contact.Fields.FirstName, Contact.Fields.LastName)
        .WithValidation("First name or last name must be present.", x =>
            ContextEntity.Contains(Contact.Fields.FirstName) || ContextEntity.Contains(Contact.Fields.LastName))
        .WithBreakValidation("The contact already violates a cross-record rule.", x =>
        {
            // expensive check, may query Dataverse — always last
            return true;
        });
}
```

## Anti-patterns to reject on sight

```csharp
// ❌ PF-VAL-003 — Dataverse query inside WithValidation(...)
.WithValidation("Duplicate check failed.", x => DataServiceProvider.User.Query<Contact>().Any(...))
// Move it to WithBreakValidation(...) instead, and put it last in the chain.
```

```csharp
// ❌ PF-VAL-004 — god predicate: one lambda hiding three unrelated rules
.WithValidation("Invalid contact.", x =>
    ContextEntity.Contains(Contact.Fields.FirstName) &&
    ContextEntity.GetAttributeValue<string>(Contact.Fields.LastName)?.Length > 1 &&
    !ContextEntity.GetAttributeValue<string>(Contact.Fields.EMailAddress1).Contains("test"))
// Split into three named validations, each with its own message.
```

```csharp
// ❌ PF-VAL-001 — wrong order: attribute check before entity/message filters
return validator
    .WithMode(PluginMode.Synchronous)
    .EntityWithAtLeastOneAttribute(ContextEntity, Contact.Fields.FirstName) // too early
    .WithStage(PluginStage.Preoperation)
    .WithMessages(["Create"])
    .ForEntity(Contact.EntityLogicalName);
```

The fixed order exists because the framework is designed cheapest-check-first: mode/stage/message/
entity are near-free filters that should eliminate irrelevant invocations before any attribute or
image inspection runs, and before any custom or Dataverse-backed check runs at all.

## ➡️ Related

- [20-task.md](./20-task.md)
- [50-logging-errors.md](./50-logging-errors.md) — `ThrowWithError` vs `ThrowWithWarning`
- [Validation Model](../../plugins/validation.md)
