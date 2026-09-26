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

## The `DataverseValidationException` contract (read this before using `ThrowWithWarning`)

This is the single most important behavioral contract in the framework, and the one most likely to
be described incorrectly by an LLM's general Dataverse knowledge — the common public pattern is
"validation failure = error", which is wrong here.

**Actual contract**, verified against `TaskBase.cs`:

- A task that ends via `DataverseValidationException` (either thrown directly in `DoExecute()`, or
  via `ThrowWithError(...)` / `ThrowWithWarning(...)` in the validation chain) ends with
  `TaskStatus.Success` and logs at `LogSeverity.Info`.
- The word **"warning" in `ThrowWithWarning(...)` describes the nature of the message shown to the
  user — it is not the log severity.** The task did what it was supposed to do: it evaluated a
  business rule and told the user the outcome. That is success, not failure.
- `InvalidPluginExecutionException` is for *unexpected* technical failures, and produces `Error` in
  monitoring. Using it for an expected business rejection pollutes monitoring with false errors —
  which defeats the reason `DataverseValidationException` exists at all.

```csharp
// ✅ Expected business rejection — Success + Info in the log, message shown to the user
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

## ➡️ Related

- [Error Handling](../../plugins/error-handling.md)
- [Logging](../../plugins/logging.md)
- [30-validation.md](./30-validation.md)
