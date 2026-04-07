# Getting Started

> A step-by-step guide to setting up your first Pillaro Dataverse Plugin Framework project from scratch.

**Prerequisites:**
- .NET Framework 4.6.2 SDK installed
- .NET 8 or later SDK installed (for tests)
- Microsoft Dataverse environment
- Visual Studio 2022 or later
- Basic knowledge of Dataverse plugin development and C#

---

## Table of Contents

1. [Environment Setup](#1-environment-setup)
2. [Project Structure](#2-project-structure)
3. [Creating the Logic Project](#3-creating-the-logic-project)
4. [Creating the Plugins Project](#4-creating-the-plugins-project)
5. [Creating the Tests Project](#5-creating-the-tests-project)
6. [Implementing Your First Plugin](#6-implementing-your-first-plugin)
7. [Building and Deployment](#7-building-and-deployment)
8. [Testing](#8-testing)
9. [Next Steps](#9-next-steps)

---

## 1. Environment Setup

### Import Framework Solution

Before you can use the framework, you must import the required Dataverse solution:

1. Navigate to `power-platform-solutions/framework/` in the repository
2. Import `PillaroFramework_managed.zip` into your Dataverse environment
3. This solution contains required entities (`pl_setting`, `pl_log`, `pl_autonumber`) and dependencies

> [!NOTE]
> The framework requires this solution to be installed for runtime configuration and logging capabilities.

---

## 2. Project Structure

The framework uses a three-project structure:

~~~
YourSolution/
├── YourSolution.Logic/          # .NET Framework 4.6.2 - Business logic and tasks
├── YourSolution.Plugins/        # .NET Framework 4.6.2 - Deployment wrapper (ILMerge)
└── YourSolution.Tests/          # .NET 8+ - xUnit integration tests
~~~

**Why this structure?**
- **Logic**: Contains all business logic (tasks, plugins, validators). Can be referenced by test projects.
- **Plugins**: Minimal wrapper that merges all dependencies into a single assembly for Dataverse deployment.
- **Tests**: Modern .NET for programmatic integration testing without ILMerge conflicts.

---

## 3. Creating the Logic Project

### Step 1: Create Class Library Project

~~~bash
# Using Visual Studio or CLI
dotnet new classlib -n YourSolution.Logic -f net462
~~~

### Step 2: Install NuGet Packages

Install the framework package and optionally SPKL for early-bound generation and deployment tooling:

~~~xml
<PackageReference Include="Pillaro.Dataverse.PluginFramework" Version="1.0.0" />
<PackageReference Include="spkl" Version="1.0.640" />
~~~

> [!NOTE]
> The framework package already includes `Microsoft.CrmSdk.CoreAssemblies` and `Newtonsoft.Json` as dependencies.

> [!TIP]
> **SPKL (Optional but Recommended):** Installing SPKL provides:
> - Early-bound entity generation tools
> - Plugin deployment automation
> - Plugin code instrumentation (stable IDs)
> - Pre-configured batch files for common tasks
>
> If you prefer alternative tools (PAC CLI, Plugin Registration Tool), you can skip SPKL installation.

> [!WARNING]
> When using SPKL for early-bound generation, do NOT upgrade `Microsoft.CrmSdk.CoreTools` beyond version **9.1.0.92**. See [Known Limitations](./README.md#-known-limitations).

### Step 3: Create Folder Structure

~~~
YourSolution.Logic/
├── Plugins/
│   └── PluginBase.cs
├── Tasks/
│   └── Task (task folders by entity or feature)
├── Features/
│   └── Feature (shared services and feature-specific code)
├── spkl/
│   ├── earlybound.bat (created by SPKL package)
│   └── instrument-plugin-code.bat (created by SPKL package)
├── spkl.json (created by SPKL package)
└── (generated early-bound entities)
~~~

**Folder Purpose:**
- **Plugins**: Plugin registration classes
- **Tasks**: Business logic tasks organized by entity or capability
- **Features**: Shared services and feature-specific code
- **spkl**: SPKL tooling scripts (auto-generated when SPKL package is installed)

### Step 4: Configure SPKL for Early-Bound Generation

If you installed SPKL, modify `spkl.json` (auto-created by SPKL NuGet package) in the Logic project root:

~~~json
{
  "earlyboundtypes": [
    {
      "entities": "task",
      "generateOptionsetEnums": true,
      "generateGlobalOptionsets": true,
      "generateStateEnums": true,
      "filename": "EarlyBoundTypes.cs",
      "classNamespace": "YourSolution.Logic",
      "serviceContextName": "ServiceContext",
      "oneTypePerFile": false
    }
  ]
}
~~~

> [!NOTE]
> The SPKL package automatically creates:
> - `spkl.json` (configuration file)
> - `spkl/earlybound.bat` (entity generation script)
> - `spkl/instrument-plugin-code.bat` (ID injection script)
>
> You only need to **modify** `spkl.json` to specify which entities to generate.

### Step 5: Generate Early-Bound Entities

Before implementing tasks, generate the early-bound entities:

~~~bash
# Navigate to the spkl folder
cd YourSolution.Logic/spkl

# Run early-bound generation (SPKL wizard will prompt for connection)
earlybound.bat
~~~

The SPKL wizard will guide you through:
- Selecting connection profile or creating a new one
- Authenticating to Dataverse
- Generating entity classes based on `spkl.json`

This generates `EarlyBoundTypes.cs` with strongly typed entity classes.

### Step 6: Create Your PluginBase

Create `Plugins/PluginBase.cs`:

~~~csharp
namespace YourSolution.Logic.Plugins
{
    public class PluginBase : PluginFramework.Plugins.PluginBase
    {
        public PluginBase(string unsecureConfig, string secureConfig) 
            : base(unsecureConfig, secureConfig)
        {
        }

        public override string GetSolutionVersion()
        {
            return "1.0.0"; // Update this with each release
        }
    }
}
~~~

> [!TIP]
> Update `GetSolutionVersion()` with each release. This version appears in logs for traceability.

---

## 4. Creating the Plugins Project

### Step 1: Create Class Library Project

~~~bash
dotnet new classlib -n YourSolution.Plugins -f net462
~~~

### Step 2: Add Project Reference

Reference your Logic project:

~~~xml
<ProjectReference Include="..\YourSolution.Logic\YourSolution.Logic.csproj" />
~~~

### Step 3: Enable Assembly Signing

Add to `.csproj`:

~~~xml
<PropertyGroup>
  <SignAssembly>true</SignAssembly>
  <AssemblyOriginatorKeyFile>key.snk</AssemblyOriginatorKeyFile>
</PropertyGroup>
~~~

Generate a signing key for the Plugins project root:

~~~bash
sn -k key.snk
~~~

Or use Visual Studio: __Project Properties > Signing > Create Strong Name Key__

> [!IMPORTANT]
> The post-build ILMerge step expects `key.snk` in the **root of the Plugins project** because it is referenced from:
>
> `$(ProjectDir)key.snk`

> [!NOTE]
> You do **not** have to use the same `.snk` file for both Logic and Plugins assemblies.
>
> What matters is:
> - the final merged Plugins assembly is signed
> - the Plugins project has `key.snk` available in its root for the post-build ILMerge step
>
> In many projects, using the same key is simpler, but it is not a technical requirement.

### Step 4: Install NuGet Packages

Install the Plugins framework package and optionally SPKL:

~~~xml
<PackageReference Include="Pillaro.Dataverse.PluginFramework.Plugins" Version="1.0.0" />
<PackageReference Include="spkl" Version="1.0.640" />
~~~

> [!NOTE]
> The `Pillaro.Dataverse.PluginFramework.Plugins` NuGet package automatically includes:
> - ILMerge.exe in `Tools/ILMerge/` folder
> - Post-build script template
> - All required build targets
>
> You do NOT need to manually download or configure ILMerge.

> [!TIP]
> **SPKL (Optional but Recommended):** Installing SPKL in the Plugins project provides:
> - `spkl/deploy-plugins.bat` for automated deployment
> - Plugin registration metadata handling

### Step 5: Configure Post-Build Event

The framework NuGet package includes ILMerge tooling. Add the post-build event to merge assemblies:

~~~xml
<PropertyGroup>
  <PostBuildEvent>
    REM Rename original assembly
    rename "$(TargetDir)$(TargetFileName)" "ForMerge$(TargetFileName)"

    REM Merge all dependencies into single DLL
    "$(ProjectDir)Tools\ILMerge\ILMerge.exe" ^
    /keyfile:"$(ProjectDir)key.snk" ^
    /target:library ^
    /targetplatform:v4,"C:\Windows\Microsoft.NET\Framework\v4.0.30319" ^
    /out:"$(TargetDir)$(TargetFileName)" ^
    "$(TargetDir)ForMerge$(TargetFileName)" ^
    "$(TargetDir)Pillaro.Dataverse.PluginFramework.dll" ^
    "$(TargetDir)Newtonsoft.Json.dll" ^
    "$(TargetDir)YourSolution.Logic.dll"

    REM Cleanup
    del "$(TargetDir)ForMerge$(TargetFileName)"
    del "$(TargetDir)YourSolution.Logic.dll"
  </PostBuildEvent>
</PropertyGroup>
~~~

> [!IMPORTANT]
> Replace `YourSolution.Logic.dll` with your actual Logic project assembly name. Keep the Logic assembly **at the end** of the merge list.

### Step 6: Configure SPKL for Deployment (Optional)

If you installed SPKL, the package automatically creates:
- `spkl.json` (can be customized for plugin registration metadata)
- `spkl/deploy-plugins.bat` (deployment script)

To customize deployment, modify the auto-generated `spkl.json` in the Plugins project root. For most scenarios, the default configuration is sufficient.

> [!NOTE]
> The SPKL NuGet package automatically creates all necessary files and scripts. You only need to modify `spkl.json` if you require custom plugin registration metadata.

---

## 5. Creating the Tests Project

### Step 1: Create xUnit Test Project

~~~bash
dotnet new xunit -n YourSolution.Tests -f net8.0
~~~

### Step 2: Add Project Reference

~~~xml
<ProjectReference Include="..\YourSolution.Logic\YourSolution.Logic.csproj" />
~~~

### Step 3: Install Framework Testing Package

~~~xml
<PackageReference Include="Pillaro.Dataverse.PluginFramework.Testing" Version="1.0.0" />
~~~

### Step 4: Configure Connection String for Tests

Tests require a Dataverse connection. Add the connection string to `appsettings.Development.json`:

~~~json
{
  "ConnectionStrings": {
    "Dataverse": "AuthType=ClientSecret;Url=https://your-org.crm.dynamics.com;ClientId=...;ClientSecret=...;TenantId=..."
  }
}
~~~

> [!IMPORTANT]
> Without a valid connection string in `appsettings.Development.json`, integration tests will not run.

> [!NOTE]
> Detailed testing setup is covered in the [Testing](./testing.md) documentation.

---

## 6. Implementing Your First Plugin

Let's create a simple autonumbering task for the `task` entity.

### Step 1: Create Task Class

Create `Tasks/Task/TaskAutoNumbering.cs` in your Logic project:

~~~csharp
using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.AutoNumbering;
using Pillaro.Dataverse.PluginFramework.Tasks;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using System;

namespace YourSolution.Logic.Tasks.Task
{
    public class TaskAutoNumbering(IServiceProvider serviceProvider, TaskContext taskContext) 
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
            AutoNumberingService autonumService = new(OrganizationServiceProvider.Admin);
            var response = autonumService.GetTransactionAutoNumber(
                TaskContext.PrimaryEntityName, 
                ContextEntity.Id, 
                null, 
                null);

            AddLogMessageLine($"AutoNumbering response: {response.Number}");

            DataServiceProvider.Admin.UpdateOutsideTransaction(response.Request.Target);

            ContextEntity.Subject = $"{response.Number}: {ContextEntity.Subject}";
        }
    }
}
~~~

### Step 2: Create Plugin Class

Create `Plugins/TaskPlugin.cs` in your Logic project:

~~~csharp
using Pillaro.Dataverse.PluginFramework.Plugins;
using YourSolution.Logic.Tasks.Task;

namespace YourSolution.Logic.Plugins
{
    [CrmPluginRegistration("Create", 
        "task", StageEnum.PreOperation, ExecutionModeEnum.Synchronous,
        "subject", "YourSolution Pre Create Task", 1, 
        IsolationModeEnum.Sandbox,
        Id = "YOUR-GUID-HERE")]
    public class TaskPlugin : PluginBase
    {
        public TaskPlugin(string unsecureConfig, string secureConfig) 
            : base(unsecureConfig, secureConfig)
        {
            RegisterTask<TaskAutoNumbering>(
                PluginStage.Preoperation, 
                ["Create"], 
                Task.EntityLogicalName, 
                PluginMode.Synchronous);
        }
    }
}
~~~

> [!IMPORTANT]
> `PluginBase` is the shared base class for your solution.
>
> `CrmPluginRegistration` belongs on the concrete plugin class, such as `TaskPlugin`.

### Step 3: Configure AutoNumbering Data in Dataverse

For the sample to work, you must configure autonumbering data in Dataverse.

At minimum:
1. Open the `pl_autonumber` table
2. Create a record for the `task` entity
3. Configure the numbering pattern according to your environment

Example values:
- **Entity logical name**: `task`
- **Prefix**: `TSK`
- **Next number / counter**: starting value for your sequence

> [!IMPORTANT]
> The `TaskAutoNumbering` sample does not work by code alone. It depends on autonumbering configuration data stored in Dataverse.

### Step 4: Build Projects

1. Build the Logic project first
2. Build the Plugins project (ILMerge will run automatically via post-build event)
3. Verify output: `YourSolution.Plugins.dll` should be a single merged assembly

---

## 7. Building and Deployment

### Build Configuration

Always build in this order:
1. **Logic project** — compiles business logic and tasks
2. **Plugins project** — triggers ILMerge post-build to create single deployment assembly

After successful build, you'll have:
- `YourSolution.Plugins.dll` — single merged assembly ready for deployment

### Generate Early-Bound Entities

Before first deployment, generate entity classes for all entities used in your tasks:

~~~bash
# Navigate to Logic project spkl folder
cd YourSolution.Logic/spkl

# Run early-bound generation (wizard will prompt for connection)
earlybound.bat
~~~

The SPKL wizard will guide you through connection setup and entity generation.

### Deploy Plugins

Use SPKL to deploy the plugin assembly to Dataverse:

~~~bash
# Navigate to Plugins project spkl folder
cd YourSolution.Plugins/spkl

# Deploy plugins (wizard will prompt for connection)
deploy-plugins.bat
~~~

The SPKL wizard will:
- Prompt for Dataverse connection (or use saved profile)
- Upload the plugin assembly
- Register plugin steps based on `CrmPluginRegistration` attributes
- Update existing steps or create new ones

### Safe Deployment with Stable IDs

For predictable, safe deployments:

**Option 1: Specify IDs upfront (recommended)**

Always include `Id` in your `CrmPluginRegistration` attributes:

~~~csharp
[CrmPluginRegistration("Create", 
    "task", StageEnum.PreOperation, ExecutionModeEnum.Synchronous,
    "subject", "YourSolution Pre Create Task", 1, 
    IsolationModeEnum.Sandbox,
    Id = "f94d984d-0f31-f111-88b4-000d3ab2695d")]
public class TaskPlugin : PluginBase { ... }
~~~

**Option 2: Instrument after first deployment**

If you deployed without IDs, run the instrument script to add them:

~~~bash
# Navigate to Logic project spkl folder
cd YourSolution.Logic/spkl

# Generate and inject stable GUIDs (wizard will prompt for connection)
instrument-plugin-code.bat
~~~

This reads deployed plugin metadata from Dataverse and updates your source code with stable IDs.

> [!WARNING]
> Without stable IDs, SPKL may not correctly identify existing plugin steps across environments. This can result in duplicated steps or incorrect step mapping.

### Deployment Checklist

- [ ] Generate early-bound entities (`earlybound.bat`)
- [ ] Build Logic project
- [ ] Build Plugins project (ILMerge runs automatically)
- [ ] Deploy plugins (`deploy-plugins.bat`)
- [ ] If first deployment without IDs, run `instrument-plugin-code.bat` and redeploy
- [ ] Verify plugin registration in __Settings > Customizations > Plug-in Assemblies__
- [ ] Verify that autonumbering data exists in `pl_autonumber`

### Alternative Deployment Tools

If you prefer not to use SPKL, you can deploy using:

**Power Platform CLI (PAC)**
~~~bash
pac plugin push --solution-name YourSolution --path YourSolution.Plugins.dll
~~~

**Plugin Registration Tool**
- Use the classic Plugin Registration Tool GUI
- Manually register assembly, plugins, and steps

> [!NOTE]
> When using alternative tools, you'll need to manually manage plugin step IDs and registration metadata.

---

## 8. Testing

### Create Your First Test

Create `TaskAutoNumberingTest.cs` in your Tests project:

~~~csharp
using Xunit;
using YourSolution.Logic.Tasks.Task;

[Trait("Owner", "...")]
[Trait("Category", nameof(TaskAutoNumbering))]
public class AutoNumberingTest(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output) : TestBase(testFixture, output)
{
    [Fact]
    public void Should_PrefixSubjectWithAutoNumber_When_TaskIsCreated()
    {
        var task = DataService.GetRepository<TaskRepository>().GetNew();
        task.Subject = "Follow up call";
        task.Id = DataService.CreateTestEntity(task);

        var loaded = LoadTask(task.Id);

        Assert.NotNull(loaded.Subject);
        Assert.NotEqual(task.Subject, loaded.Subject);
        Assert.Contains(": Follow up call", loaded.Subject);
    }

    private Task LoadTask(Guid id)
    {
        return DataService
            .Query<Task>()
            .Where(x => x.Id == id)
            .Select(x => new Task { Subject = x.Subject })
            .First();
    }
}
~~~

> [!IMPORTANT]
> For this test to pass:
> - the plugin must be deployed
> - the Dataverse connection string must be configured
> - autonumbering data must exist in `pl_autonumber`

> [!NOTE]
> Detailed testing patterns and `TestBase` setup are covered in the [Testing](./testing.md) documentation.

---

## 9. Next Steps

Now that you have a working project, explore:

- **[Task Model](./task-model.md)** — Learn about task lifecycle and best practices
- **[Validation Model](./validation.md)** — Master the fluent validation API
- **[Autonumbering](./autonumbering.md)** — Configure sequence generation
- **[Testing](./testing.md)** — Write comprehensive integration tests
- **[Examples](../examples/)** — Study real-world implementations

### Example Tasks to Explore

Study these tasks in the `examples/` folder:

| Task | Complexity | What It Demonstrates |
|---|---|---|
| `TaskAutoNumbering` | ⭐ Simple | Basic validation, autonumbering service |
| `UpdateAddressLabel` | ⭐⭐ Medium | PreImage usage, attribute change detection |
| `ValidateContactNamesTask` | ⭐⭐ Medium | Custom validation, error throwing, Features usage |
| `TaskSummarySync` | ⭐⭐⭐ Complex | PreImage/PostImage comparison, related entity queries |

---

## Common Issues

### ILMerge Fails

- Ensure all referenced assemblies exist in `$(TargetDir)`
- Verify the Logic assembly is **last** in the merge list
- Check that `key.snk` exists in the Plugins project root
- Verify `Tools/ILMerge/ILMerge.exe` was installed by the NuGet package

### Plugin Not Triggering

- Verify the framework solution is imported
- Check plugin registration (stage, message, entity)
- Review validation rules in `AddValidations()`
- Ensure plugin was deployed successfully

### AutoNumbering Does Not Work

- Verify the `pl_autonumber` record exists for the target entity
- Check prefix, counter, and numbering configuration
- Confirm the plugin runs on the expected message and stage

### Tests Cannot Reference Logic Project

- Ensure you're referencing the **Logic** project, not the Plugins project
- The Plugins project (after ILMerge) contains duplicate types and cannot be referenced

### Early-Bound Generation Fails

- Ensure `Microsoft.CrmSdk.CoreTools` is version **9.1.0.92** or lower
- Check SPKL connection wizard completed successfully
- Verify entity names in `spkl.json` match Dataverse schema names

### SPKL Deployment Creates Duplicate Steps

- Add `Id` attributes to all `CrmPluginRegistration` declarations
- Or run `instrument-plugin-code.bat` after first deployment to inject stable IDs

### SPKL Batch Files Not Found

- Ensure SPKL NuGet package is installed in the project
- The batch files are created automatically by SPKL in the `spkl/` folder
- If missing, reinstall the SPKL package

---

## Quick Reference

### Project Templates

| Project | Framework | Purpose |
|---|---|---|
| Logic | .NET Framework 4.6.2 | Business logic and tasks |
| Plugins | .NET Framework 4.6.2 | ILMerge wrapper for deployment |
| Tests | .NET 8+ | xUnit integration tests |

### Key Files

| File | Location | Purpose |
|---|---|---|
| `PluginBase.cs` | Logic/Plugins/ | Solution-wide configuration |
| `TaskPlugin.cs` | Logic/Plugins/ | Entity-specific plugin |
| `TaskAutoNumbering.cs` | Logic/Tasks/Task/ | Business logic task |
| `CustomerService.cs` | Logic/Features/Feature/ | Shared feature service |
| `spkl.json` | Logic/ | Early-bound generation config (auto-created by SPKL) |
| `earlybound.bat` | Logic/spkl/ | Generate entity classes (auto-created by SPKL) |
| `instrument-plugin-code.bat` | Logic/spkl/ | Inject stable plugin IDs (auto-created by SPKL) |
| `key.snk` | Plugins/ | Signing key used by the ILMerge post-build step |
| `spkl.json` | Plugins/ | Plugin deployment metadata (auto-created by SPKL) |
| `deploy-plugins.bat` | Plugins/spkl/ | Deploy plugins to Dataverse (auto-created by SPKL) |

### Common Commands

~~~bash
# Generate early-bound entities
cd YourSolution.Logic/spkl
earlybound.bat

# Deploy plugins
cd YourSolution.Plugins/spkl
deploy-plugins.bat

# Inject stable IDs (after first deployment without IDs)
cd YourSolution.Logic/spkl
instrument-plugin-code.bat
~~~

> [!NOTE]
> All SPKL commands use an interactive wizard - no need to specify connection strings on the command line.

---

**Need Help?**
- Review [examples](../examples/) for complete implementations
- Check [docs](./README.md) for detailed architecture and patterns
- See [Known Limitations](./README.md#-known-limitations) for compatibility notes