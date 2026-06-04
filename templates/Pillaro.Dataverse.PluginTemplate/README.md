# Pillaro Dataverse Plugin Template Template

Creates a customer-ready Dataverse plugin solution with three projects:

- `Pillaro.Dataverse.PluginTemplate.Logic` - plugin orchestration and task logic
- `Pillaro.Dataverse.PluginTemplate.Plugins` - deployable Dataverse plugin assembly
- `Pillaro.Dataverse.PluginTemplate.Tests` - xUnit-based test project

The template is designed for customer projects. Project names and namespaces are based on the project name selected during template creation.

## Install locally

From the repository root:

```powershell
dotnet new install .\templates\Pillaro.Dataverse.PluginTemplate
```

## Build Visual Studio template

From the repository root:

```powershell
.\templates\Pillaro.Dataverse.PluginTemplate\visual-studio\build-template.ps1
```

The script creates:

```text
artifacts\templates\Pillaro.Dataverse.PluginTemplate.zip
```

Copy the `.zip` file to:

```text
%USERPROFILE%\Documents\Visual Studio 2022\Templates\ProjectTemplates
```

Restart Visual Studio and create a project named `Pillaro Dataverse Plugin Template`.

## Create a project

```powershell
dotnet new pillaro-dv-plugin `
  -n Pillaro.Dataverse.PluginTemplate `
  --dataverseSolutionName MyDataverseSolution
```

`pillaro-dv-plugin` is only the `dotnet new` short name. It is not the deployment CLI. Deployment tooling will be distributed separately as a NuGet package later.

## Parameters

| Parameter | Description | Default |
|---|---|---|
| `-n` / `--name` | Root project name and namespace source. | Required by `dotnet new` usage. |
| `--dataverseSolutionName` | Dataverse solution name used by future deployment tooling. | Empty |
| `--testTargetFramework` | Target framework for the test project. | `net8.0` |
| `--frameworkVersion` | Version of `Pillaro.Dataverse.PluginFramework` and `Pillaro.Dataverse.PluginFramework.Testing`. | `1.0.2` |

## Generated structure

```text
Pillaro.Dataverse.PluginTemplate/
  Pillaro.Dataverse.PluginTemplate.sln
  Pillaro.Dataverse.PluginTemplate.Logic/
    Entities/
    Plugins/
    Tasks/
  Pillaro.Dataverse.PluginTemplate.Plugins/
  Pillaro.Dataverse.PluginTemplate.Tests/
    Data/
      CleanupHandlers/
      Repositories/
    Tests/
```

No extra `src` or `tests` folder level is created.

## Signing

The generated Plugins project uses strong-name signing and includes `key.snk` in the Plugins project root.

## Build behavior

The generated Plugins project targets `.NET Framework 4.6.2`, signs the assembly and merges the deployable plugin DLL using the configured post-build event.

The template expects ILMerge tooling to be available from the `Pillaro.Dataverse.PluginFramework` NuGet package.

## Early-bound and deployment tooling

SPKL, early-bound generation and deployment CLI files are intentionally not part of this template.

Those parts will be handled by separate tooling/NuGet packages.

## Visual Studio template notes

The Visual Studio template lives in `visual-studio/`. It creates Logic, Plugins and Tests projects with the same deployment-ready references and signing/merge behavior.

The generated Logic project includes a minimal `ExamplePlugin` and `ExampleTask` registered for `Create` of `account`. Replace the example with project-specific plugin logic after the project is created.

Visual Studio project templates do not expose the same parameter surface as `dotnet new`, so the VS template starts with these defaults:

- Pillaro framework package version: `1.0.2`
- test target framework: `net8.0`

## Visual Studio VSIX package

Build the installable VSIX wrapper:

```powershell
dotnet build .\templates\Pillaro.Dataverse.PluginTemplate\visual-studio-vsix\Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix.csproj
```

The script creates:

```text
artifacts\templates\Pillaro.Dataverse.PluginTemplate.VisualStudio.vsix
```
