# Logging

> [!IMPORTANT]
> The framework provides built-in diagnostic logging for plugin execution, task execution, and detailed runtime diagnostics.

---

## 📑 Navigation

- [🔍 What logging is for](#-what-logging-is-for)
- [🗂️ Logging entities](#️-logging-entities)
- [📊 What is logged](#-what-is-logged)
- [🧱 Task-level logging model](#-task-level-logging-model)
- [🪵 Task logging helpers](#-task-logging-helpers)
- [🔐 Access and security roles](#-access-and-security-roles)
- [⚠️ Production logging recommendations](#️-production-logging-recommendations)
- [💻 Examples](#-examples)
- [✅ Design recommendations](#-design-recommendations)
- [➡️ Related documents](#️-related-documents)

---

## 🔍 What logging is for

The framework logging model is designed to make plugin behavior visible and diagnosable.

Use it to understand:

- what task was executed
- what task was skipped
- why validation failed
- what execution message was produced
- what runtime details were captured
- whether the outcome was `Success`, `NotValid`, or `Error`

This is not only useful for debugging.
It is also useful for:

- runtime diagnostics
- support investigation
- behavior verification
- performance review
- identifying badly targeted task registrations

---

## 🗂️ Logging entities

The framework solution provides these main logging-related entities:

- `Diagnostic Log`
- `Diagnostic Log detail`

The logging model is centered around:

- one main diagnostic log record
- optional detail records linked to that log

This makes it possible to keep both:

- a readable execution summary
- structured diagnostic detail

---

## 📊 What is logged

Based on the framework logging model and UI, diagnostic logs can contain information such as:

- task name
- task status
- entity
- entity id
- message
- mode
- plugin stage
- depth
- elapsed milliseconds
- user
- initiating user
- log severity
- correlation id
- execution detail text

The detailed log view can also contain structured detail entries.

Typical automatically captured details include:

- input parameters
- output parameters
- pre-images
- post-images

> [!NOTE]
> The framework records both summary-level and detail-level information.
> This makes the logs useful for both quick review and deeper diagnostics.

---

## 🧱 Task-level logging model

Each task has its own log context.

This is important because the framework does not treat plugin execution as one opaque block.
Instead, it records task-level outcomes individually.

The most important task result states are:

- `Success`
- `NotValid`
- `Error`

This gives you visibility into:

- which task ran successfully
- which task was skipped because validation did not pass
- which task failed with a real execution error

A task that often ends as `NotValid` is not automatically wrong.
But it may indicate:

- validation is too late
- task registration is too broad
- the trigger scope should be reviewed

This is one of the main reasons the framework logging model is useful for optimization as well as diagnostics.

---

## 🪵 Task logging helpers

Inside `TaskBase<TEntity>`, task-level logging should be done through the prepared task logging helpers.

Use:

- `AddLogMessageLine(...)`
- `AddLogDetail(...)`

### `AddLogMessageLine(...)`

Adds a line into the task execution message.

Use it for:

- short execution notes
- decision points
- explanations of what the task actually did
- business-relevant runtime messages

### `AddLogDetail(...)`

Adds a structured detail record to the task log.

Use it for:

- named values
- serialized objects
- payload snapshots
- diagnostic details that should be visible in the detailed log view

> [!IMPORTANT]
> For normal task execution diagnostics, prefer task logging helpers over ad hoc custom logging patterns.
> Keep task-level diagnostics inside the task log context.

> [!NOTE]
> Input parameters from the plugin context are already logged automatically.
> Use task logging helpers to add the task-specific details that explain task behavior.

---

## 🔐 Access and security roles

Access to logs is controlled through framework security roles.

Typical role:

- `Pillaro Log Reader`

This makes it possible to separate:

- runtime support and diagnostics access
- configuration access
- implementation responsibilities

> [!NOTE]
> Logging is part of framework operations and diagnostics, not only development.

---

## ⚠️ Production logging recommendations

`MinimalSeverityLevel` controls the minimum severity that should be saved.

The value works as a threshold. The configured severity and all higher severities are saved.

| MinimalSeverityLevel | Saved severities | Typical use |
|---:|---|---|
| `1` or lower | `Debug`, `Info`, `Warning`, `Error` | Full diagnostic logging. Useful for development, testing, initial setup, or temporary deep diagnostics. |
| `2` | `Info`, `Warning`, `Error` | Informational logging without Debug details. Useful for test or controlled support scenarios. |
| `3` | `Warning`, `Error` | Recommended default for production environments. |
| `4` | `Error` | Error-only logging. Useful when production log volume must be kept minimal. |

Recommended defaults:

- **Development/Test environments** — use `0` or `1` when full diagnostic visibility is needed.
- **Production environments** — use `3` as the recommended default.
- **High-volume production environments** — use `4` if only errors should be retained.
- **Temporary production diagnostics** — use `0` or `1` only when full diagnostic visibility is required for investigation.

> [!WARNING]
> Full logging (`MinimalSeverityLevel = 0` or `1`) can generate a large amount of diagnostic data.
> In production environments, this may negatively affect performance and increase Dataverse storage usage.
> Full logging is not recommended for normal production operation. Enable it only temporarily when detailed diagnostics are required, and increase the level again after the investigation is finished.

Use only values from `0` to `4` for normal configuration. Values higher than `4` are not recommended because no standard framework severity is higher than `Error`.

---

## 💻 Examples

### Add a task execution message

    protected override void DoExecute()
    {
        AddLogMessageLine("Validation passed.");
        AddLogMessageLine("Contact name was checked.");
    }

### Add a structured detail

    protected override void DoExecute()
    {
        AddLogDetail("ForbiddenWords", new[] { "Admin", "Test" });
        AddLogMessageLine("Forbidden words loaded from runtime configuration.");
    }

### Add a scalar diagnostic value

    protected override void DoExecute()
    {
        var minimalSeverityLevel = SettingService.GetIntegerValue("MinimalSeverityLevel");

        AddLogDetail("MinimalSeverityLevel", minimalSeverityLevel);
        AddLogMessageLine("Runtime severity level loaded.");
    }

---

## ✅ Design recommendations

Use these rules by default:

- log task behavior at the task level
- use `AddLogMessageLine(...)` for short execution flow messages
- use `AddLogDetail(...)` for structured values and payloads
- keep logs useful for support, not only for developers
- do not fill logs with noise that has no diagnostic value
- use logs to understand repeated `NotValid` outcomes
- keep log messages readable and explicit

Good logs should help answer:

- what ran
- what did not run
- why it did not run
- what the task actually did
- what input or detail mattered

> [!TIP]
> Good logging explains behavior.
> It should not force the reader to reconstruct the whole task flow from code.

---

## ➡️ Related documents

- [Task Model](./task-model.md)
- [Validation Model](./validation.md)
- [Execution Pipeline](./execution-pipeline.md)
- [Runtime Configuration](./configuration.md)
- [Error Handling](./error-handling.md)
