# Pillaro Dataverse Plugin Template

A Visual Studio project template that generates a Microsoft Dataverse plugin
solution — a `.NET Framework 4.6.2` plugin assembly plus an integration test
project — built on the Pillaro Dataverse Plugin Framework.

Licensed under the Apache License, Version 2.0.

## What it generates

Three projects:

- **Logic** (`net462`) — plugins and tasks. All business logic lives here. Ships
  with an example plugin, an example task and an in-solution guide to the task
  model.
- **Plugins** (`net462`) — the assembly deployed to Dataverse. Strong-name
  signed, with an ILMerge post-build event that produces the single merged DLL
  the platform requires.
- **Tests** (`net8.0`) — xUnit v3 integration tests that run against a real
  Dataverse environment and clean up the data they create. Includes a `WhoAmI`
  connection smoke test.

The first build scaffolds package-managed tooling into the `Logic` and `Plugins`
projects: ILMerge tooling, deployment wrappers (`DeployPlugins.bat` /
`DeployPlugins.ps1`), early-bound entity generation helpers and a
`PillaroSettings.json` you point at your environment.

## Requirements

- Visual Studio 2022 (17.x)
- .NET Framework 4.6.2 targeting pack
- .NET SDK 8.0 or later, for the test project
- A Dataverse environment and an application user, to deploy and to run tests
- Power Platform CLI (`pac`), only for early-bound entity generation

## Also available as a dotnet new template

For CLI-first and Visual Studio Code workflows, install
[Pillaro.Dataverse.PluginTemplate.DotNetNew](https://www.nuget.org/packages/Pillaro.Dataverse.PluginTemplate.DotNetNew)
instead:

```
dotnet new install Pillaro.Dataverse.PluginTemplate.DotNetNew
dotnet new pillaro-dataverse-plugin-dotnet -n MySolution
```

## Links

- [Framework repository](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework)
- [Pillaro.Dataverse.PluginFramework on NuGet](https://www.nuget.org/packages/Pillaro.Dataverse.PluginFramework)
- [Pillaro.Dataverse.PluginFramework.Testing on NuGet](https://www.nuget.org/packages/Pillaro.Dataverse.PluginFramework.Testing)
- [Documentation](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/tree/main/docs)
- [Getting started](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/getting-started.md)
- [License (Apache-2.0)](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/LICENSE)
