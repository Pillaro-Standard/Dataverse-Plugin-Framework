# Pillaro Dataverse Plugin Framework

A task-based framework for building predictable and testable Microsoft Dataverse plugins in C#.

This README is included in the NuGet package so consumers can understand package intent, quick-start usage and key constraints directly from NuGet.org.

---

## What this package provides

- A small, opinionated runtime for Dataverse plugin development based on tasks (`Task` = single responsibility).
- A fluent validation pipeline that strictly separates validation from execution.
- Deterministic execution model — a task either clearly runs or clearly does not run.
- Validation-level logging — each validation rule produces a clear log message explaining why the task did or did not execute.
- Structured logging and conventions suitable for diagnostics and automated testing.
- Opinionated helpers for common scenarios (e.g. autonumbering, entity images, deterministic execution patterns).
---

## Why use it

- Reduce complexity — replace large plugin classes with focused, isolated tasks.
- Improve testability — each task is independently testable and deterministic.
- Enforce consistency — shared patterns across teams reduce onboarding time and bugs.
- Make behavior predictable — no hidden execution paths or implicit logic.
- Designed for long-term maintainability — clear task pipeline, enforced project structure and programmatic testing support reduce complexity and cost of future changes.

---

## Platform constraints (important)

This framework is designed specifically for Microsoft Dataverse plugin runtime:

- Only `.NET Framework 4.6.2` is supported by the platform
- Plugin must be deployed as a single assembly (DLL)
- All dependencies must be merged (ILMerge or equivalent)
- Assemblies should be strong-name signed

These constraints are reflected in the framework design.

---

## Quick start

1. Install the package via NuGet:

~~~powershell
Install-Package Pillaro.Dataverse.PluginFramework
~~~

2. Create a solution-level `PluginBase` (one per solution, used as a common entry point and configuration root):

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

3. Create a plugin class and register tasks (one plugin per logical area or entity):

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
    }
}
~~~

---

## Core concepts

- **Plugin**  
  Entry point registered in Dataverse. Matches incoming event to registered tasks.

- **Task**  
  Single unit of work with two explicit phases:
  - **Validation** — defines when the task should run
  - **Execution** — pure business logic

- **Validation model**  
  Designed to guarantee deterministic execution — a task either clearly runs or clearly does not run.  
  Each validation rule logs a message explaining its decision, making it easy to understand why a task was skipped or executed.  
  This significantly improves debugging, observability and automated testing.

---

## Requirements & packaging notes

- Target runtime: `.NET Framework 4.6.2`
- Dependencies: `Microsoft.CrmSdk.CoreAssemblies`, `Newtonsoft.Json`
- Plugin must be deployed as a single merged assembly (ILMerge or equivalent)
- Strong-name signing is recommended for all assemblies

---

## Where to find more

- GitHub: https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework

---

## License

This project is published under the Pillaro Community License (PCL) v1.0.

Attribution is required when the framework is used in delivered solutions:

> "This solution is built using Pillaro Dataverse Plugin Framework."