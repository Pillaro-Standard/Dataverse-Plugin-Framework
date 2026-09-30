# Logging and Errors

> [!IMPORTANT]
> Read [`AGENTS.md`](../../../AGENTS.md) first. This file is one entry in its rule catalog.

| ID | Rule | Source |
|---|---|---|
| PF-LOG-001 | Task-level diagnostics go through `AddLogMessageLine(...)` / `AddLogDetail(...)`, never `LogService` directly. | [task-model.md](../../plugins/task-model.md), [logging.md](../../plugins/logging.md) |
| PF-LOG-002 | Do not log input parameters or images — the framework logs them automatically. | [task-model.md](../../plugins/task-model.md) |
| PF-LOG-003 | NEVER log secrets, tokens, or personal data beyond business need. | [SECURITY.md](../../SECURITY.md), [step-configuration.md](../../plugins/step-configuration.md) |
| PF-ERR-001 | An expected business stop is `DataverseValidationException`: the user sees the message, the task ends `Success` + `Info`. NOT `InvalidPluginExecutionException` — that creates a false `Error` in monitoring. | [error-handling.md](../../plugins/error-handling.md), decision D3 |
| PF-ERR-002 | `InvalidPluginExecutionException` in task code only in exceptional cases — the framework's own conversion handles the common path. | [error-handling.md](../../plugins/error-handling.md) |
| PF-ERR-003 | Do not build a custom try/catch/log pipeline in a task. | [error-handling.md](../../plugins/error-handling.md) |
| PF-ERR-004 | The user-facing rejection message is product text: in the users' language, verbatim from the requirement, actionable, no technical detail. | — |

## The `DataverseValidationException` contract (read this before using `ThrowWithWarning`)

This is the single most important behavioral contract in the framework, and the one most likely to
be described incorrectly by an LLM's general Dataverse knowledge — the common public pattern is
"validation failure = error", which is wrong here.

**Actual contract**, verified against `TaskBase.cs`:

- A task that ends via `DataverseValidationException` (either thrown directly in `DoExecute()`, or
  via `ThrowWithWarning(...)` in the validation chain) ends with `TaskStatus.Success` and logs at
  `LogSeverity.Info`.
- The word **"warning" in `ThrowWithWarning(...)` describes the nature of the message shown to the
  user — it is not the log severity.** The task did what it was supposed to do: it evaluated a
  business rule and told the user the outcome. That is success, not failure.
- `ThrowWithError(...)` throws `InvalidPluginExecutionException` (`ThrowExceptionValidator.cs`), so the
  task ends `Error`. It is not a business rejection — use it only when a failed check means something
  is technically wrong.
- `InvalidPluginExecutionException` is for *unexpected* technical failures, and produces `Error` in
  monitoring. Using it for an expected business rejection pollutes monitoring with false errors —
  which defeats the reason `DataverseValidationException` exists at all.

The usual place for a business rejection is `DoExecute()` — checking the rule is the task's
responsibility, the chain only decides when to check (PF-TASK-008, `ValidateNames` in `/examples`):

```csharp
// ✅ Correct — the task's own rule, a message for the user, the task ends Success + Info
protected override void DoExecute()
{
    if (IsForbidden(ContextEntity.FirstName))
        throw new DataverseValidationException("First name is a forbidden word.");
}
```

`ThrowWithWarning(...)` at the end of the chain is an accepted alternative: same outcome for the
user and in the log. If you use it, mind the predicate:

> [!WARNING]
> **Predicate polarity trap, confirmed live in this repository's own simulation (see
> `docs/ai/analysis-workflow.md`).** The predicate passed to `ThrowWithWarning(...)` /
> `ThrowWithError(...)` means **"is valid"** (`true` = OK, no throw) — the validator throws when the
> predicate is `false`, exactly like `WithValidation(...)` and `WithBreakValidation(...)`. It is easy
> to misread the XML doc ("checks predicate; if it is not valid, throw...") as "predicate = the
> rejection condition" and write it backwards. Written backwards, the task rejects *every* record,
> not just the forbidden ones, and the bug is invisible until the plugin actually executes — a fast
> `dotnet build` or the offline `manifest`/`validate` gate cannot catch it, only a real (or
> deployed-and-tested) execution can.

```csharp
// ✅ Correct — predicate is "is valid"; throws only when it's false
.ThrowWithWarning("First name is a forbidden word.", x => !IsForbidden(x.FirstName))
```

```csharp
// ❌ Wrong — predicate written as "should reject" instead of "is valid" — this rejects
// EVERY record, because the predicate is false whenever the name is NOT forbidden
.ThrowWithWarning("First name is a forbidden word.", x => IsForbidden(x.FirstName))
```

```csharp
// ❌ Wrong — treats an expected business rule as a technical failure (false Error in monitoring)
if (IsForbidden(ContextEntity.FirstName))
    throw new InvalidPluginExecutionException("First name is a forbidden word.");
```

```csharp
// ❌ Wrong — hand-rolled try/catch pipeline duplicates what the framework already does
protected override void DoExecute()
{
    try
    {
        // ...
    }
    catch (Exception ex)
    {
        LogService.Log(ex); // bypasses AddLogMessageLine/AddLogDetail and the framework's own handling
        throw;
    }
}
```

## The user-facing message is product text, not a log line (PF-ERR-004)

The message passed to `ThrowWithWarning(...)` / `DataverseValidationException` is what the end user
reads in the form. Treat it as product copy:

- **Language:** the language of the solution's users, taken verbatim from the requirement
  (`outcome.rejection` in [`analysis-workflow.md`](../analysis-workflow.md)). These rules are in
  English; the users of a Czech customer's CRM are not. Never translate or rephrase a message the
  requirement states.
- **Content:** what is wrong and what the user can do about it. No entity logical names, no
  exception text, no GUIDs.
- **Missing text:** if the requirement gives no message, ask for one — do not invent product copy.

When the solution localizes messages, pass a `Lazy<string>` so the lookup runs only when the
validation actually fails:

```csharp
.ThrowWithWarning(
    new Lazy<string>(() => translations.Get("contact.deactivate.adminOnly", TaskContext.InitiatingUserId)),
    _ => IsAdministrator())
```

## ➡️ Related

- [Error Handling](../../plugins/error-handling.md)
- [Logging](../../plugins/logging.md)
- [30-validation.md](./30-validation.md)
