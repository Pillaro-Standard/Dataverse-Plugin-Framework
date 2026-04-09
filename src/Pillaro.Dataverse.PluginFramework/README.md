# Pillaro Dataverse Plugin Framework

A lightweight, task-based framework for building predictable, testable and maintainable Microsoft Dataverse (Dynamics 365) plugins in C#.

This README is included in the NuGet package so consumers can read package intent, quick-start steps and core requirements directly from NuGet.org.

---

## What this package provides

- A small, opinionated runtime for Dataverse plugin development that enforces a task-based structure (`Task` = single responsibility).
- A fluent validation pipeline that cleanly separates validation from execution.
- Built-in structured logging and conventions useful for diagnostics and automated testing.
- Utilities to support common plugin scenarios (autonumbering, image handling, consistent registration patterns).

---

## Why use it

- Reduce complexity: keep each business rule in a focused task instead of large plugin classes.
- Improve testability: tasks are independently testable and deterministic.
- Consistent patterns across teams and solutions, reducing onboarding time and bugs.

---

## Quick start

1. Install the package via NuGet:

~~~powershell
Install-Package Pillaro.Dataverse.PluginFramework
~~~

2. Create a solution-level `PluginBase` (one per solution):

~~~csharp
public class PluginBase : PluginFramework.Plugins.PluginBase
{
    public PluginBase(string unsecureConfig, string secureConfig)
        : base(unsecureConfig, secureConfig)
    {
    }

    public override string GetSolutionVersion() => "1.0";
}
~~~

3. Create a plugin class and register tasks (one plugin class per logical area or entity):

~~~csharp
public class ContactPlugin : PluginBase
{
    public ContactPlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
    {
        RegisterTask<ValidateContactTask>(
            PluginStage.Preoperation,
            new[] { "Create", "Update" },
            "contact",
            PluginMode.Synchronous);
    }
}
~~~

4. Implement a `Task` (all business logic belongs here):

~~~csharp
public class ValidateContactTask : TaskBase<Logic.Contact>
{
    public ValidateContactTask(IServiceProvider services, TaskContext ctx)
        : base(services, ctx) { }

    protected override ICompleteValidation AddValidations(IBasicModeValidation v)
    {
        return v
            .WithMode(PluginMode.Synchronous)
            .WithStage(PluginStage.Preoperation)
            .WithMessages(new[] { "Create", "Update" })
            .ForEntity(ContextEntity.LogicalName)
            .EntityWithAtLeastOneAttribute(ContextEntity, nameof(ContextEntity.FirstName), nameof(ContextEntity.LastName));
    }

    protected override void DoExecute()
    {
        // Business logic only
        if (string.IsNullOrWhiteSpace(ContextEntity.FirstName))
            throw new InvalidPluginExecutionException("First name is required.");
    }
}
~~~

---

## Requirements & packaging notes

- Target runtime: ` .NET Framework 4.6.2` (Dataverse sandbox requirement).
- Package depends on `Microsoft.CrmSdk.CoreAssemblies` and `Newtonsoft.Json` (see package metadata).
- Plugin assembly must be signed and packaged as a single assembly for deployment (ILMerge or equivalent is recommended).
- If you use SPKL for early-bound generation, do not upgrade `Microsoft.CrmSdk.CoreTools` beyond `9.1.0.92` (known `CrmSvcUtil.exe` compatibility issue).

---

## Core concepts (short)

- `Plugin` — entry point registered in Dataverse; matches current event to registered tasks.
- `Task` — single unit of work containing two phases:
  - Validation — fluent rules that decide whether task runs
  - Execution — pure business logic that runs only if validations pass
- Fluent validation ordering ensures predictable evaluation and clear flow control (`WithBreakValidation`, `ThrowWithError`, etc.).

---

## Where to find more

- Full documentation and guides: https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/tree/main/docs
- Examples and sample plugins: https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/tree/main/examples
- Report issues or request features: https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/issues

---

## License

This project is published under the Pillaro Community License (PCL) v1.0. See the `LICENSE` file in the repository for full terms. Attribution required when the framework is used in delivered solutions: "This solution is built using Pillaro Dataverse Plugin Framework."