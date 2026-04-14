# Getting Started

> [!IMPORTANT]
> This document shows how to build your first Dataverse plugin with the Pillaro Dataverse Plugin Framework.
> It focuses on plugin development only.
> Test documentation is covered separately in the `/docs/tests/` section.

> [!NOTE]
> The root [README](../../README.md) helps you decide whether the framework is relevant for your solution.
> This document is the implementation entry point for building your first plugin.

---

## 📑 Navigation

- [⚙️ Prerequisites](#️-prerequisites)
- [1. Import the framework solution](#1-import-the-framework-solution)
- [2. Create the recommended solution structure](#2-create-the-recommended-solution-structure)
- [3. Create the `Logic` project](#3-create-the-logic-project)
- [4. Create the `Plugins` project](#4-create-the-plugins-project)
- [5. Enable signing](#5-enable-signing)
- [6. Configure the prepared post-build action](#6-configure-the-prepared-post-build-action)
- [7. Create your solution `PluginBase`](#7-create-your-solution-pluginbase)
- [8. Create your first task](#8-create-your-first-task)
- [9. Create your first plugin](#9-create-your-first-plugin)
- [10. Build the final assembly](#10-build-the-final-assembly)
- [11. Deploy to Dataverse](#11-deploy-to-dataverse)
- [12. Verify the result](#12-verify-the-result)
- [✅ Recommendations](#-recommendations)
- [➡️ Next steps](#️-next-steps)

---

## ⚙️ Prerequisites

You need:

- a Microsoft Dataverse environment
- Visual Studio 2022 or later
- basic knowledge of Dataverse plugin development
- support for `.NET Framework 4.6.2` in plugin projects

> [!NOTE]
> Standard Dataverse plugin prerequisites remain the same whether you use this framework or not.

---

## 1. Import the framework solution

Before using the framework runtime features, import the framework solution into your Dataverse environment.

Location in the repository:

    power-platform-solutions/framework

This solution provides the Dataverse components required by the framework.

> [!IMPORTANT]
> Without the framework solution installed, framework runtime features such as settings, logging, and related supporting components will not work correctly.

---

## 2. Create the recommended solution structure

Use this structure:

    YourSolution/
    ├── YourSolution.Logic/
    ├── YourSolution.Plugins/
    └── YourSolution.Tests/

Purpose of each project:

- `YourSolution.Logic`  
  Contains plugin business logic, tasks, plugin classes, and shared feature code.

- `YourSolution.Plugins`  
  Produces the final deployable assembly for Dataverse.

- `YourSolution.Tests`  
  Optional, but strongly recommended.
  Used for programmatic tests without referencing the merged plugin assembly.

> [!IMPORTANT]
> The `Logic` project exists mainly to keep business logic separate and referenceable.
> This is especially important for tests.
> After assembly merge, the deployable plugin output is not a good reference target for test code because merged assemblies can introduce duplicate type and dependency problems.

> [!TIP]
> Even if you do not create the test project immediately, the `Logic` + `Plugins` split is still the recommended structure because it keeps business logic clean and gives you a path to add tests later without restructuring the solution.

---

## 3. Create the `Logic` project

### 3.1 Create a Class Library

Create a class library targeting `.NET Framework 4.6.2`.

Example:

    dotnet new classlib -n YourSolution.Logic -f net462

### 3.2 Install the framework package

Add the framework package to the `Logic` project:

- [Pillaro.Dataverse.PluginFramework](https://www.nuget.org/packages/Pillaro.Dataverse.PluginFramework)

### 3.3 Create the basic folder structure

Recommended structure:

    YourSolution.Logic/
    ├── Plugins/
    │   └── PluginBase.cs
    ├── Tasks/
    └── Features/

Recommended purpose:

- `Plugins/` → plugin classes and shared plugin base
- `Tasks/` → business logic tasks
- `Features/` → shared services or business features reused by tasks

> [!NOTE]
> In many solutions, tasks are entity-specific.
> In some cases, a task can represent a shared business capability used across multiple entities.
>
> Typical examples:
> - VAT recalculation for quote, order, and invoice
> - shared business validation across several document types

---

## 4. Create the `Plugins` project

### 4.1 Create a Class Library

Create a class library targeting `.NET Framework 4.6.2`.

Example:

    dotnet new classlib -n YourSolution.Plugins -f net462

### 4.2 Add reference to the `Logic` project

Add a project reference from `YourSolution.Plugins` to `YourSolution.Logic`.

Example:

    <ProjectReference Include="..\YourSolution.Logic\YourSolution.Logic.csproj" />

### 4.3 Install the plugin package

Add the plugin package to the `Plugins` project.

> [!IMPORTANT]
> `YourSolution.Plugins` is the deployment project.
> It should not become the place where your business logic grows.
> Keep business logic in `YourSolution.Logic`.

---

## 5. Enable signing

The `Plugins` project must be configured for assembly signing.

Add signing settings to the `Plugins` project file:

    <PropertyGroup>
      <SignAssembly>true</SignAssembly>
      <AssemblyOriginatorKeyFile>key.snk</AssemblyOriginatorKeyFile>
    </PropertyGroup>

Create the signing key in the root of the `Plugins` project:

    sn -k key.snk

Or use Visual Studio signing support.

> [!IMPORTANT]
> `key.snk` must exist in the root of the `Plugins` project.
> The prepared post-build action expects it there.

> [!NOTE]
> What matters is that the final deployable assembly is strong-name signed.
> The exact internal signing approach can vary, but the prepared setup expects `key.snk` in the `Plugins` project root.

---

## 6. Configure the prepared post-build action

After installing the plugin package into the `Plugins` project, prepared resources are available in that project:

    tools/CrmTools/
    tools/ILMerge/

These resources are added by the package and include:

- prepared post-build action templates in `tools/CrmTools/`
- ILMerge binaries in `tools/ILMerge/`

### 6.1 Choose the correct post-build action

Two prepared post-build actions are available for two different situations:

#### Option A — You use separate `Logic` and `Plugins` projects

Use this when:

- business logic is in `YourSolution.Logic`
- deployment output is produced by `YourSolution.Plugins`
- you want clean separation for maintainability and future testability

Use:

    tools/CrmTools/PostBuildAction-logic_plugin-projects.txt

This action merges:

- the `Plugins` project output
- the referenced `Logic` project output

into one final deployable DLL.

> [!IMPORTANT]
> Do not use the prepared post-build action as-is without checking its placeholders.

> [!NOTE]
> In the logic + plugin project variant, the script contains a placeholder for the referenced Logic assembly name.
> Replace it with the actual DLL name of your Logic project.

Placeholder:

    {LOGIC_ASSEMBLY}

Example of the placeholder usage inside the prepared action:

    del "$(TargetDir){LOGIC_ASSEMBLY}"

This means you must replace `{LOGIC_ASSEMBLY}` with the actual output DLL name, for example:

    del "$(TargetDir)YourSolution.Logic.dll"

#### Option B — You use a single project only

Use this when:

- you do not separate business logic into a dedicated `Logic` project
- you are building everything in one plugin project

Use:

    tools/CrmTools/PostBuildAction-single-project.txt

### 6.2 What the merge step is for

Dataverse requires a single deployable plugin assembly.

The prepared post-build action solves that by creating one final output even when development is split into multiple projects.

> [!IMPORTANT]
> The final deployment artifact is one DLL, even though development may be split into separate projects.

> [!TIP]
> For framework-based solutions, the `Logic` + `Plugins` setup is the recommended default.
> It keeps your business logic referenceable and avoids the problems that appear when other projects start referencing merged outputs.

---

## 7. Create your solution `PluginBase`

Create `Plugins/PluginBase.cs` in the `Logic` project.

Example:

    public class PluginBase : PluginFramework.Plugins.PluginBase
    {
        public PluginBase(string unsecureConfig, string secureConfig)
            : base(unsecureConfig, secureConfig)
        {
        }

        public override string GetVersion()
        {
            return "1.0.0";
        }
    }
    

> [!TIP]
> Update `GetVersion()` on each release so logs clearly identify the solution version.

---

## 8. Create your first task

Create a first task in the `Logic` project.

Recommended location:

    YourSolution.Logic/Tasks/

Example:
   
    public class MyFirstTask(IServiceProvider serviceProvider, TaskContext taskContext)
        : TaskBase<Logic.Task>(serviceProvider, taskContext)
    {
        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Preoperation)
                .WithMessages(["Create"])
                .ForEntity(ContextEntity.LogicalName);
        }

        protected override void DoExecute()
        {
            // Business logic
        }
    }
    

This example shows the basic task pattern:

- inherit from `TaskBase<TEntity>`
- define preconditions in `AddValidations()`
- put business logic into `DoExecute()`

> [!NOTE]
> In real projects, you will often use early-bound entity types and framework services.
> For the first onboarding step, keep the first task simple and focus on the task structure.

---

## 9. Create your first plugin

Create a plugin class in the `Logic` project.

Recommended location:

    YourSolution.Logic/Plugins/

Example:
    
    public class TaskPlugin : PluginBase
    {
        public TaskPlugin(string unsecureConfig, string secureConfig)
            : base(unsecureConfig, secureConfig)
        {
            RegisterTask<MyFirstTask>(
                PluginStage.Preoperation,
                ["Create"],
                Task.EntityLogicalName,
                PluginMode.Synchronous);
        }
    }
    

This example shows the basic plugin pattern:

- inherit from your solution `PluginBase`
- register one or more tasks
- keep plugin classes focused on orchestration

> [!NOTE]
> A plugin usually targets one entity or one functional area.
> The task itself can still represent either entity-specific logic or reusable business logic used across multiple entities.

---

## 10. Build the final assembly

Build in this order:

1. `YourSolution.Logic`
2. `YourSolution.Plugins`

The prepared post-build action in the `Plugins` project should produce the final merged assembly.

Expected result:

- one final deployable plugin DLL
- strong-name signed
- ready for Dataverse registration

> [!IMPORTANT]
> Do not deploy the separate `Logic` project output.
> Deploy the final merged plugin assembly produced by the `Plugins` project.

---

## 11. Deploy to Dataverse

Deploy the final merged plugin assembly using your standard Dataverse deployment process.

> [!NOTE]
> The framework does not require a specific deployment tool.
> Use the deployment process your team already uses.

> [!TIP]
> In this repository, SPKL is used in some places as an implementation choice.
> It is not a required part of the framework usage model.

---

## 12. Verify the result

After deployment, verify:

- the assembly is registered in Dataverse
- the relevant plugin step is active
- the registered task executes as expected
- the expected runtime behavior is visible
- framework logging works as expected

If your first task changes data or validates input, verify the expected business result directly in Dataverse.

---

## ✅ Recommendations

### Write tests for tasks

The `Tests` project is optional, but strongly recommended.

For each task, consider adding a programmatic test.

Why:

- in many cases, writing the test is as fast as manual verification
- the test becomes reusable regression coverage
- the value grows as the solution expands over time
- it helps maintain quality without relying only on repetitive manual checks

> [!TIP]
> A task-based architecture makes programmatic testing more practical because business logic is already split into smaller focused units.

### Keep responsibilities clear

Recommended split:

- `Logic` → business logic
- `Plugins` → deployable assembly
- `Tests` → programmatic verification

This separation reduces confusion and helps the solution scale more cleanly.

---

## ➡️ Next steps

Continue with:

- [Plugin Model](./plugin-model.md)
- [Task Model](./task-model.md)
- [Validation Model](./validation.md)
- [Data Access](./data-access.md)
- [Security Contexts](./security-contexts.md)
- [Architecture](../architecture.md)
- [Packaging and Deployment](../packaging-and-deployment.md)
- [Testing Overview](../tests/testing.md)

> [!NOTE]
> Plugin documentation lives under `/docs/plugins/`.
> Test documentation lives under `/docs/tests/`.