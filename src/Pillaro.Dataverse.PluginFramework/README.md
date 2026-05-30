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

## Logging (recommended)

The framework includes structured logging at the task and validation level.

Plugins work without any additional setup, however it is strongly recommended to install the **Pillaro Framework application** into your Dataverse environment.

This application allows you to:

- view logs from individual task executions
- understand why a task was skipped or executed
- troubleshoot issues without debugging the plugin directly

The application can be found in the project repository under the `power-platform-solutions/framework` folder.

To enable logging, configure the following setting in Dataverse:

- In the **Runtime Setting** entity, set `MinimalSeverityLevel` to `0`(int)

Without this configuration, logs may not be recorded.

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

    public override string GetVersion() => "1.0";
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

    protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
    {
        return validator
            .WithMode(PluginMode.Synchronous)
            .WithStage(PluginStage.Preoperation)
            .WithMessages(new[] { "Create", "Update" })
            .ForEntity(ContextEntity.LogicalName);
    }

    protected override void DoExecute()
    {
        // Business logic only
    }
}
~~~

5. Enable assembly signing using a strong-name key file:

~~~text
key.snk
~~~

The key file should be placed in the plugin project root and used during build and merge.

6. Configure post-build action for assembly merge.

Post-build actions are generated after rebuild and are available in:

~~~text
Tools/ILMerge/
~~~

There are two variants:

- **PostBuildAction-logic_plugin-projects.txt**  
  Use this when your solution contains separate Logic and Plugin projects.  
  This variant merges the plugin assembly together with the Logic assembly.

- **PostBuildAction-single-project.txt**  
  Use this when all logic is implemented in a single plugin project.  
  This variant merges only the plugin assembly and its dependencies.

Choose the variant that matches your project structure.

7. Build the plugin project.

After signing and post-build configuration, the project produces a single merged assembly ready for deployment. The package also generates deployment helpers in `Tools/Deployment/` and a `PillaroSettings.json` file in the plugin project root.

8. Deploy the plugin assembly.

Recommended tools:

- Plugin Registration Tool
- generated `Tools/Deployment/DeployPlugins.bat` or `Tools/Deployment/DeployPlugins.ps1`

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
