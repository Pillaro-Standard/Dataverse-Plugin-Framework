# Error Handling

> [!IMPORTANT]
> The framework already provides the main error-handling flow.
> Tasks should not build their own exception-handling pipeline.

---

## 📑 Navigation

- [🔍 What error handling covers](#-what-error-handling-covers)
- [🧱 Framework error-handling model](#-framework-error-handling-model)
- [⚠️ `DataverseValidationException`](#️-dataversevalidationexception)
- [❌ How exceptions are handled](#-how-exceptions-are-handled)
- [🧭 How to work with exceptions in tasks](#-how-to-work-with-exceptions-in-tasks)
- [💻 Examples](#-examples)
- [✅ Design recommendations](#-design-recommendations)
- [➡️ Related documents](#️-related-documents)

---

## 🔍 What error handling covers

The framework distinguishes between:

- expected business stop
- technical execution failure
- unexpected exception

These outcomes are handled by the framework runtime.

The main error-handling flow is already implemented in:

- `PluginBase`
- `TaskBase<TEntity>`

---

## 🧱 Framework error-handling model

At runtime:

- `TaskBase` handles task-level execution and logging
- `PluginBase` handles plugin-level exception propagation

That means the framework already controls:

- task failure logging
- exception propagation
- final translation to Dataverse plugin exception behavior

> [!IMPORTANT]
> Tasks should express intent.
> The framework handles the pipeline.

---

## ⚠️ `DataverseValidationException`

`DataverseValidationException` exists for expected business situations where the user should see a message, but the task should not be treated as a technical error.

This is important mainly because of logging behavior.

If a task ends through `DataverseValidationException`:

- the user still gets a meaningful message
- the framework continues with the correct plugin-visible exception behavior
- the task log does not end as a technical error

This is useful when:

- the situation is valid business behavior
- execution should stop
- the user should see a message
- the task should not produce a false error signal in logs or monitoring

If the same situation is handled through `InvalidPluginExecutionException`, the task log is treated as `Error`.

That may be undesirable when:

- the situation is not really a failure
- logs are consumed by monitoring
- error states trigger unnecessary alerts or follow-up actions

> [!IMPORTANT]
> Use `DataverseValidationException` when the user should see a message but the task should not be treated as an error outcome.

---

## ❌ How exceptions are handled

In normal framework usage, a task can throw:

- a normal `Exception`
- `DataverseValidationException`
- or another exception type that fits the situation

The framework then handles the pipeline and converts the final plugin-visible failure into `InvalidPluginExecutionException`.

This means:

- users get a proper Dataverse plugin error message
- task execution is logged correctly
- plugin exception behavior stays consistent

`InvalidPluginExecutionException` can still be thrown directly, but in normal framework usage it is usually not required.

> [!NOTE]
> The important point is not which exception type you throw inside every task.
> The important point is that the framework converts the final plugin-visible failure to the correct Dataverse exception behavior.

---

## 🧭 How to work with exceptions in tasks

Use these rules by default:

- use `DataverseValidationException` for expected business messages that should not become task errors
- use a normal exception for technical failures
- do not build your own exception pipeline inside the task
- do not catch and rewrap everything without reason
- let the framework handle final propagation

In practice:

- expected business stop with user message → `DataverseValidationException`
- technical failure → normal exception
- framework → handles logging and final translation

> [!IMPORTANT]
> You do not need to throw `InvalidPluginExecutionException` everywhere just to make the framework behave correctly.

---

## 💻 Examples

### Business stop without task error state

    protected override void DoExecute()
    {
        if (ContextEntity.Contains("firstname") && ContextEntity["firstname"]?.ToString() == "Admin")
        {
            throw new DataverseValidationException("The value 'Admin' is not allowed.");
        }
    }

Use this when the user should get a message, but the situation should not be treated as a technical error in task logging.

### Technical failure

    protected override void DoExecute()
    {
        if (!HasPreImage())
        {
            throw new Exception("Required pre-image is missing.");
        }
    }

Use this when execution cannot continue and the situation is a real technical failure.

### Explicit Dataverse plugin exception

    protected override void DoExecute()
    {
        if (!HasPreImage())
        {
            throw new InvalidPluginExecutionException("Required pre-image is missing.");
        }
    }

This is still allowed, but usually not necessary in normal framework usage.

---

## ✅ Design recommendations

- use `DataverseValidationException` for expected business messages that should not become task errors
- use normal exceptions for technical failures
- let the framework handle propagation
- do not duplicate framework infrastructure in task code
- use exceptions to express failure, not to rebuild pipeline control

A good task should make it clear:

- whether the user should see a message
- whether the outcome is business behavior or a real error
- whether the task should end as a normal stop or as an error

> [!TIP]
> Choose exception type mainly by intended outcome in task logging and runtime behavior, not only by message content.

---

## ➡️ Related documents

- [Task Model](./task-model.md)
- [Validation Model](./validation.md)
- [Execution Pipeline](./execution-pipeline.md)
- [Logging](./logging.md)