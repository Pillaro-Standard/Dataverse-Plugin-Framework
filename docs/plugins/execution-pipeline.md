# Execution Pipeline

> [!IMPORTANT]
> The framework executes tasks in a deterministic order inside a single plugin `Execute` method.
> The plugin resolves matching tasks, creates them one by one, validates them, executes them when valid, and records the result for each task.

---

## 📑 Navigation

- [🔍 What the execution pipeline is](#-what-the-execution-pipeline-is)
- [🧱 Core pipeline model](#-core-pipeline-model)
- [🧰 Shared runtime context](#-shared-runtime-context)
- [🔄 Task lifecycle](#-task-lifecycle)
- [✅ Validation outcome](#-validation-outcome)
- [🚀 Execution outcome](#-execution-outcome)
- [💥 Exception outcome](#-exception-outcome)
- [📊 Logging and task result states](#-logging-and-task-result-states)
- [🪵 What is logged automatically](#-what-is-logged-automatically)
- [🧭 Execution flow](#-execution-flow)
- [📌 Key guarantees](#-key-guarantees)
- [✅ Design recommendations](#-design-recommendations)
- [➡️ Related documents](#️-related-documents)

---

## 🔍 What the execution pipeline is

The execution pipeline describes how the framework runs tasks inside the Dataverse plugin runtime.

The framework uses a deterministic execution model where all logic runs inside one plugin `Execute` method.

In practice, the plugin:

- resolves registered tasks that match the current execution context
- creates task instances one by one
- passes the same shared `TaskContext` through the whole plugin execution
- validates each task
- executes each valid task
- records the outcome of each task

This model is designed to keep plugin behavior:

- predictable
- readable
- modular
- diagnosable

---

## 🧱 Core pipeline model

The core runtime model is:

- **Plugin** = orchestration layer
- **Task** = executable business unit
- **Validation** = execution precondition layer
- **TaskContext** = shared execution context for the whole plugin run

Inside `PluginBase`, the framework resolves all registered tasks matching the current:

- stage
- message
- primary entity
- mode

Each matching task is then created and executed in order.

> [!IMPORTANT]
> The pipeline is not based on implicit discovery at runtime.
> It is based on explicit task registration in the plugin and deterministic task execution in sequence.

---

## 🧰 Shared runtime context

All tasks in one plugin execution share the same `TaskContext`.

This shared context contains runtime information such as:

- plugin execution context
- execution metadata
- task counters and ordering
- shared items for cross-task communication

The shared `TaskContext` makes it possible to keep tasks modular while still allowing controlled data sharing across the same execution.

Typical shared context usage includes:

- passing lightweight values between tasks
- preserving execution-wide metadata

> [!NOTE]
> Tasks should use shared context intentionally.
> It is useful for execution coordination, but it should not become a hidden dependency mechanism.

---

## 🔄 Task lifecycle

Each task follows the same lifecycle:

1. task instance is created
2. runtime data is initialized
3. validation is executed
4. execution runs only if validation passed
5. log result is finalized

This lifecycle is enforced by `TaskBase`.

The key rule is simple:

- validation happens first
- execution happens only when validation succeeds

This keeps task behavior predictable and prevents business execution from being mixed with precondition checks.

---

## ✅ Validation outcome

If validation does not pass:

- the task is marked as `NotValid`
- execution is skipped
- validation messages are kept in the task log
- the pipeline continues with the next task

This is a normal and expected pipeline outcome.

It does **not** mean the system failed.
It means the task was not applicable or not allowed to run under the current conditions.

> [!IMPORTANT]
> Validation failure is not an execution error.
> It is a controlled pipeline result.

This is useful because:

- tasks can be registered more broadly and filtered precisely
- the framework can record why a task did not run
- the pipeline can continue without treating this as a technical failure

---

## 🚀 Execution outcome

If validation succeeds:

- `DoExecute()` is called
- execution time is recorded
- execution messages and log details are included in the task log
- the task is marked as `Success` when no failure occurs

This is the normal successful pipeline outcome.

Execution remains task-local:
- one task executes its business logic
- then the pipeline moves to the next registered task

---

## 💥 Exception outcome

If an exception occurs during task execution, the pipeline behavior depends on the type of failure.

### Expected user-facing business failure

If a task raises `DataverseValidationException`:

- the task is marked as `Success`
- the log severity is set to `Info`
- the message is preserved in the task log
- the exception is rethrown and later surfaced to Dataverse as `InvalidPluginExecutionException`

This is useful for business scenarios where:

- the user should see a clear message
- the situation is expected business behavior
- the outcome should not be treated as a technical error

### Technical or unexpected failure

If a task raises `InvalidPluginExecutionException` or another exception:

- the task is marked as `Error`
- the log severity is set to `Error`
- execution details are preserved in the task log
- the exception stops further execution
- the plugin execution fails

At plugin level, the framework preserves logs and rethrows the error through Dataverse plugin exception handling.

> [!IMPORTANT]
> A technical execution failure stops the remaining pipeline.
> A validation failure does not.

---

## 📊 Logging and task result states

Each task produces its own log outcome inside the execution pipeline.

The most important result states are:

- `Success`
- `NotValid`
- `Error`

These states are important because they make pipeline behavior visible.

They help you understand:

- which tasks were executed successfully
- which tasks were skipped because validation did not pass
- which tasks ended with real execution failure
- whether a task is triggered too broadly
- where performance or registration optimizations may be needed

A task that frequently ends as `NotValid` is not automatically wrong.
But it may indicate that:

- the registration scope is too broad
- validation order could be improved
- the trigger design should be reviewed

> [!NOTE]
> This task-level result model is one of the reasons the pipeline is easier to diagnose than a plugin model where all logic is mixed into one execution block.

---

## 🪵 What is logged automatically

The framework automatically records important runtime information during pipeline execution.

At plugin and task level, this includes things such as:

- task result state
- severity
- elapsed time
- validation messages
- execution messages
- input parameters
- output parameters
- pre-images
- post-images

In task code, additional execution detail can be added through task logging helpers such as:

- `AddLogMessageLine(...)`
- `AddLogDetail(...)`

This keeps task-level diagnostics inside the task log context instead of scattering them across unrelated logging mechanisms.

---

## 🧭 Execution flow

~~~mermaid
sequenceDiagram
    participant DV as Dataverse
    participant P as Plugin
    participant T as Task
    participant V as Validation
    participant E as Execution
    participant L as Logging

    DV->>P: Trigger Event
    P->>P: Resolve matching tasks

    loop For each Task
        P->>T: Initialize
        T->>V: Validate

        alt Validation OK
            V-->>T: Valid
            T->>E: Execute (DoExecute)

            alt Execution Success
                E-->>T: Completed
                T->>L: Log (Status = Success)

            else DataverseValidationException
                E-->>T: Validation exception
                T->>L: Log (Status = Success, Severity = Info)
                T-->>P: Rethrow to user

            else Unhandled Exception
                E-->>T: Exception
                T->>L: Log (Status = Error)
                T-->>P: Rethrow exception
                P-->>DV: Rollback transaction
                Note over DV,P: All changes rolled back except logs
            end

        else Validation NOT Valid
            V-->>T: Not Valid
            T->>L: Log (Status = NotValid)
            Note over T: Execution is skipped
        end
    end

    P-->>DV: Return result
~~~

### How the execution works

Execution is driven by the plugin, while the actual logic is delegated to individual tasks. Each task is responsible for validating itself, executing its logic, and producing a structured log.

The flow is as follows:

1. Dataverse triggers the plugin  
2. `PluginBase.Execute(...)` initializes shared execution context  
3. matching tasks are resolved based on registration  
4. tasks are instantiated in deterministic order  
5. each task runs validation (`Validate`)  
6. only valid tasks proceed to execution (`DoExecute`)  
7. each task produces its own log result  
8. logs are collected and persisted  
9. plugin execution completes or fails  

---

### Validation vs execution

The pipeline explicitly separates two types of validation:

#### Pre-execution validation (`Validate`)
- runs before `DoExecute`
- failure results in `TaskStatus.NotValid`
- execution is skipped
- does not affect other tasks

#### Runtime validation (`DataverseValidationException`)
- occurs during execution (`DoExecute`)
- represents a controlled business outcome
- message is propagated to the user
- task is still considered **successfully executed**
- logged as `Status = Success` with `Severity = Info`

This distinction is important — both cases may lead to user-visible feedback, but they represent different execution states.

---

### Flow summary

- task selection is resolved at plugin level  
- validation and execution are handled at task level  
- `NotValid` means the task was intentionally skipped  
- `DataverseValidationException` is a controlled business response, not a failure  
- technical exceptions result in `Error`  
- technical failure stops the pipeline and triggers rollback  
- validation failure does not stop subsequent tasks  

---

## 📌 Key guarantees

The execution pipeline guarantees:

- **Deterministic order** — tasks run in a defined sequence  
- **Shared execution context** — all tasks operate within one execution scope  
- **Clear separation of concerns** — validation and execution are independent phases  
- **Non-blocking validation** — `NotValid` tasks do not interrupt the pipeline  
- **Controlled user feedback** — business validation is surfaced via exceptions without breaking execution semantics  
- **Task-level observability** — each task produces its own independent result  
- **Consistent logging** — execution metadata and outcomes are captured in a structured and predictable way  

---

## ✅ Design recommendations

When designing tasks for this pipeline:

- keep validation and execution separate
- treat validation failure as a normal pipeline result
- do not put business logic into the plugin orchestration layer
- keep task dependencies explicit
- use shared context carefully and intentionally
- use task-level log helpers to make execution behavior understandable
- let expected business failures remain business failures
- let real technical failures fail fast

When reviewing the pipeline behavior of a plugin, you should be able to answer:

- which tasks matched the execution
- which tasks validated successfully
- which tasks were skipped as `NotValid`
- which task failed if execution stopped
- whether the task order still makes sense

---

## ➡️ Related documents

Continue with:

- [Plugin Model](./plugins/plugin-model.md)
- [Task Model](./plugins/task-model.md)
- [Validation Model](./plugins/validation.md)
- [Data Access](./plugins/data-access.md)
- [DataService](./plugins/data-service.md)
- [Getting Started](./plugins/getting-started.md)
- [Architecture](./architecture.md)