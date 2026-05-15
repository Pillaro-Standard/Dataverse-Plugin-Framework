# Pillaro Dataverse Patching Project Template

Creates a customer-ready Dataverse plugin solution with three projects:

- `Contoso.Dataverse.PatchingProject.Logic` - plugin orchestration and task logic
- `Contoso.Dataverse.PatchingProject.Plugins` - deployable Dataverse plugin assembly
- `Contoso.Dataverse.PatchingProject.Tests` - xUnit-based test project

The template is designed for customer projects. Project names and namespaces are based on the project name selected during template creation.

## Install locally

From the repository root:

```powershell
dotnet new install .\templates\Pillaro.Dataverse.PatchingProject
```

## Create a project

```powershell
dotnet new pillaro-dv-patching `
  -n Contoso.Dataverse.PatchingProject `
  --dataverseSolutionName ContosoSolution
```

`pillaro-dv-patching` is only the `dotnet new` short name. It is not the deployment CLI. Deployment tooling will be distributed separately as a NuGet package later.

## Parameters

| Parameter | Description | Default |
|---|---|---|
| `-n` / `--name` | Root project name and namespace source. | Required by `dotnet new` usage. |
| `--dataverseSolutionName` | Dataverse solution name used by future deployment tooling. | Empty |
| `--testTargetFramework` | Target framework for the test project. | `net8.0` |
| `--frameworkVersion` | Version of `Pillaro.Dataverse.PluginFramework` and `Pillaro.Dataverse.PluginFramework.Testing`. | `1.0.0` |
| `--includeSamplePlugin` | Includes a minimal contact plugin and task. | `true` |
| `--includeSampleTests` | Includes a minimal smoke test. | `true` |

## Generated structure

```text
Contoso.Dataverse.PatchingProject/
  Contoso.Dataverse.PatchingProject.sln
  Contoso.Dataverse.PatchingProject.Logic/
  Contoso.Dataverse.PatchingProject.Plugins/
  Contoso.Dataverse.PatchingProject.Tests/
```

No extra `src` or `tests` folder level is created.

## Signing

The generated Plugins project uses strong-name signing and expects `key.snk` in the Plugins project root.

If `key.snk` does not exist, the project runs:

```powershell
sn -k key.snk
```

before build.

## Build behavior

The generated Plugins project targets `.NET Framework 4.6.2`, signs the assembly and merges the deployable plugin DLL using ILMerge after build.

The template expects ILMerge tooling to be available from the `Pillaro.Dataverse.PluginFramework` NuGet package.

## Early-bound and deployment tooling

SPKL, early-bound generation and deployment CLI files are intentionally not part of this template.

Those parts will be handled by separate tooling/NuGet packages.
