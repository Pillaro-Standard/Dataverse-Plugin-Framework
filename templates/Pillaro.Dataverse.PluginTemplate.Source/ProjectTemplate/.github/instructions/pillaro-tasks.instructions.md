---
name: Tasks
description: Tasks and features — business logic, the validation chain, data access, errors and logging.
applyTo: "**/*Logic/Tasks/**/*.cs,**/*Logic/Features/**/*.cs"
---

<!-- Managed by pillaro-dv ai-sync (Pillaro Dataverse Plugin Framework). Do not edit: the next sync overwrites this file. Your own rules belong in AGENTS.md, outside the managed block. -->

# Tasks and features

Full rules and reasons: [`20-task.md`](../../.pillaro/ai/rules/20-task.md),
[`30-validation.md`](../../.pillaro/ai/rules/30-validation.md),
[`40-data-access.md`](../../.pillaro/ai/rules/40-data-access.md),
[`50-logging-errors.md`](../../.pillaro/ai/rules/50-logging-errors.md). Paths, namespaces and the reference
tasks: [`project-setup.md`](../../.pillaro/project-setup.md).

A task is one business responsibility. `AddValidations()` decides **whether** it runs — does this
operation concern the task? `DoExecute()` does **what** the task is responsible for. For a task that
validates, that includes checking the rule and rejecting.

## Structure and naming

- `Tasks/<Entity>/<TaskName>.cs`, inheriting `TaskBase<Logic.<Entity>>`, with a primary constructor
  taking only `IServiceProvider` and `TaskContext` (PF-TASK-001).
- One responsibility per task. Two unrelated operations are two tasks (PF-TASK-002).
- Name the task verb-first by business intent, without the entity name and without a `Task` suffix
  (PF-TASK-007): `Validate…`/`Restrict…`/`Check…` rejects, `Set…` fills a value on the saved record,
  `Recalculate…`/`Update…` changes other records, `Create…`/`Assign…` creates or reassigns related
  records, `Sync…`/`Send…` calls an external system, `Get…` answers a Custom API.
- Logic reused by several tasks, or too large for the task, goes to `Features/<FeatureName>/`
  (PF-TASK-006). Sync and Async variants of one behavior share one feature class (PF-TASK-012).
  A new top-level folder needs the developer's approval (PF-ARCH-006).
- Add every new file to the `Logic` project file — the project lists its compile items explicitly,
  and a file that is not listed is silently left out of the build (PF-BUILD-007).
- `TaskContext` is shared by all tasks on the step. Use it deliberately, not as a hidden channel
  between tasks (PF-TASK-005).

## `AddValidations()` — when the task runs

- It filters: message, stage, entity, the attributes the task reacts to, required images, and
  preconditions without which the task has nothing to do (PF-TASK-003).
- Fixed order: `WithMode` → `WithStage` → `WithMessage(s)` → `ForEntity` → image checks → attribute
  checks → `WithValidation` → `WithBreakValidation` / `ThrowWith*` (PF-VAL-001).
- `WithValidation(...)` never queries Dataverse. A check that reads data is `WithBreakValidation(...)`,
  last in the chain (PF-VAL-002/003).
- Several small named validations, each with a readable message — not one large lambda
  (PF-VAL-004/005).
- **The predicate means "is valid"**: `true` passes, `false` stops or throws. This holds for
  `ThrowWithWarning(...)` too — written the other way round, the task rejects every record.
- Predicates are pure: no assignments, no `TaskContext.AddItem`, no writes (PF-TASK-010).
- A task that writes to its own entity must not trigger itself again: keep the written columns out of
  its step's filtering attributes, or add `.WithValidation("Skipped: nested execution.", x => x.PluginExecutionContext.Depth <= 1)`
  (PF-TASK-011).

## `DoExecute()` — what the task does

- The task's business responsibility. No silent early `return` for a case the chain should have
  filtered out — there it would be logged as skipped (PF-TASK-003).
- A `Validate…`/`Restrict…`/`Check…` task checks its rule here and, when it is broken, throws
  `DataverseValidationException` with the user's message (PF-TASK-008). That `if` is the task's
  outcome, not a guard. Reference: `ValidateNames.cs` in `/examples`. `ThrowWithWarning(...)` at the
  end of the chain is an accepted alternative with the same outcome — mind that its predicate means
  "is valid".
- On Update the target holds only changed columns. Read unchanged ones from the pre-image
  (`ContextEntity.Contains(...) ? ContextEntity.X : PreImage?.X`), validate it with
  `HasPreImageWhen(x => x.Message == "Update")`, and register the image with exactly those columns
  (PF-TASK-009).

## Data access

- Default `DataServiceProvider`; `OrganizationServiceProvider` only when a raw `IOrganizationService`
  is needed. Never create an organization service yourself (PF-DATA-001, PF-TASK-004).
- The lowest context that works (`User`, `InitiatingUser`, `Admin`), written at the call site.
  `Admin` needs a comment saying why (PF-DATA-002/003). Security roles, teams and business units are
  read in `Admin` context, and built-in roles are identified by role template id, not by name.
- Writes that belong to the current operation are queued with
  `TaskContext.AddEntityToUpdate(entity, ServiceUser)`, not written with `Update(...)` (PF-DATA-010).
  The queue needs framework 1.2.0 or later — on an older version it is never written, so update the
  record directly and say so in the plan.
- Entity names: `Logic.Contact.EntityLogicalName`. Attribute names: `Logic.Contact.Fields.FirstName`.
  A string literal only while the entity has no early-bound type. `nameof(...)` as an attribute name
  is always wrong (PF-DATA-008/009).
- A missing early-bound type or attribute: stop and tell the developer which entity to add to
  `EarlyBoundSettings.json` and regenerate. Never edit `EarlyBound/`, never write late-bound code to
  get around it (PF-DATA-005–007).

## Errors and logging

- A business rejection is `DataverseValidationException` thrown in `DoExecute()` (or
  `ThrowWithWarning(...)` in the chain): the user sees the message and the task ends `Success`.
  `ThrowWithError(...)` and `InvalidPluginExecutionException`
  end as `Error` in monitoring — only for real technical failures, never for a business rule
  (PF-ERR-001/002). No custom try/catch/log pipeline (PF-ERR-003).
- The rejection message is the user's text: verbatim from the requirement, in the users' language,
  without technical detail. If the requirement has none, ask (PF-ERR-004).
- Log with `AddLogMessageLine(...)` / `AddLogDetail(...)`. Do not log inputs or images (the framework
  does), never secrets or personal data (PF-LOG-001–003).

## Sandbox

The code runs in the Dataverse sandbox on .NET Framework 4.6.2: no `[GeneratedRegex]`, no source
generators, no APIs newer than .NET Framework 4.6.2 offers. Dataverse data is read and written through
the organization service, never the Dataverse Web API.

## Shape

```csharp
// In namespace <Logic root>.Tasks.Contact, "Contact" is the namespace — write Logic.Contact.
public class RestrictSameNames(IServiceProvider serviceProvider, TaskContext taskContext)
    : TaskBase<Logic.Contact>(serviceProvider, taskContext)
{
    // When: a contact is created or its first or last name changes.
    protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
    {
        return validator
            .WithMode(PluginMode.Synchronous)
            .WithStage(PluginStage.Prevalidation)
            .WithMessages(["Create", "Update"])
            .ForEntity(Logic.Contact.EntityLogicalName)
            .HasPreImageWhen(x => x.Message == "Update")
            .EntityWithAtLeastOneAttribute(ContextEntity, Logic.Contact.Fields.FirstName, Logic.Contact.Fields.LastName);
    }

    // What: first and last name must differ — the task's own rule.
    protected override void DoExecute()
    {
        var firstName = ContextEntity.Contains(Logic.Contact.Fields.FirstName) ? ContextEntity.FirstName : PreImage?.FirstName;
        var lastName = ContextEntity.Contains(Logic.Contact.Fields.LastName) ? ContextEntity.LastName : PreImage?.LastName;

        if (string.Equals(firstName, lastName, StringComparison.OrdinalIgnoreCase))
            throw new DataverseValidationException("First name and last name must differ.");
    }
}
```
