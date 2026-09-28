# Pillaro Dataverse Plugin Template (dotnet new)

Generates a Microsoft Dataverse plugin solution — a `.NET Framework 4.6.2` plugin
assembly plus an integration test project — for C# developers building Dataverse
plug-ins with the Pillaro Dataverse Plugin Framework.

## Install and create

```powershell
dotnet new install Pillaro.Dataverse.PluginTemplate.DotNetNew
dotnet new pillaro-dataverse-plugin-dotnet -n MySolution -o MySolution
```

`-n` sets the solution name. It is substituted into namespaces, assembly names,
project file names and the solution file, so `-n MySolution` produces
`MySolution.Logic`, `MySolution.Plugins` and `MySolution.Tests`.

## Generated solution

```text
MySolution/
├── MySolution.slnx
├── LICENSE.txt
├── .vscode/
│   ├── extensions.json          # recommends the C# Dev Kit
│   └── settings.json            # points OmniSharp at MySolution.slnx
├── Logic/                       # net462 – all business logic lives here
│   ├── MySolution.Logic.csproj
│   ├── README.md                # in-solution guide to the task model
│   ├── Plugins/
│   │   ├── PluginBase.cs        # solution-wide plugin base, returns GetVersion()
│   │   └── ExamplePlugin.cs     # registers ExampleTask + declarative step metadata
│   └── Tasks/Example/
│       └── ExampleTask.cs       # validation rules + DoExecute() business logic
├── Plugins/                     # net462 – the assembly deployed to Dataverse
│   ├── MySolution.Plugins.csproj # signed, with an ILMerge post-build event
│   └── key.snk                  # strong-name key (replace before release)
└── Tests/                       # net8.0 – xUnit v3 integration tests
    ├── MySolution.Tests.csproj
    ├── appsettings.json          # Dataverse connection string placeholder
    ├── appsettings.Development.json
    ├── TestAutofacModule.cs      # Autofac registrations for test data + cleanup
    └── Tests/
        ├── TestBase.cs           # shared fixture, settings, cleanup handlers
        └── ConnectionTests.cs    # WhoAmI smoke test against your environment
```

`Logic` holds the plugins and tasks; `Plugins` produces the single merged,
strong-name signed DLL that Dataverse requires; `Tests` runs against a real
Dataverse environment and cleans up the data it creates.

The first build of `Logic` and `Plugins` scaffolds package-managed tooling into
each of those projects — `Tools/ILMerge/` (merge tooling and two post-build
script variants), `Tools/Deployment/` (`DeployPlugins.bat`, `DeployPlugins.ps1`),
`Tools/EarlyBound/` (`GenerateEarlyBound.bat`, `EarlyBoundSettings.json`) and a
`PillaroSettings.json`. These are regenerated on build; configure behaviour in
`PillaroSettings.json` rather than editing them.

## What to do next

1. Build the solution. This restores the framework package and writes the
   deployment and ILMerge tooling described above.

   ```powershell
   dotnet build MySolution.slnx
   ```

2. Point the deployment at your environment. Edit `Plugins/PillaroSettings.json`
   and set `solution` to your Dataverse solution's unique name. It reads the
   connection string from the environment variable named in
   `dataverse.connectionStringEnvironmentVariable` (`DV_CONN` by default):

   ```powershell
   $env:DV_CONN = "Url=https://yourorg.crm4.dynamics.com/;AuthType=ClientSecret;ClientId=...;ClientSecret=..."
   ```

3. Replace `Plugins/key.snk` with your own strong-name key before you ship.

4. Import the Pillaro framework solution into your Dataverse environment. It ships in
   the framework repository under `power-platform-solutions/framework`. Without it the
   framework's runtime features — settings, logging and their supporting components —
   do not work. See
   [Getting started](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/getting-started.md#1-import-the-framework-solution).

5. Deploy the assembly and synchronize the registered steps:

   ```powershell
   .\Plugins\Tools\Deployment\DeployPlugins.bat
   ```

   The wrapper uses the `debug` profile; pass `release` for the release profile.

6. To run the tests, put your connection string into
   `Tests/appsettings.Development.json` (or set `ConnectionStrings__Dataverse`),
   then `dotnet test`. `ConnectionTests` verifies the connection with `WhoAmI`.

7. Replace `ExamplePlugin` and `ExampleTask` with your own. The example registers
   a synchronous PreValidation step on `contact` Create/Update; the GUIDs in
   `Register(...)` are step identifiers you should regenerate for your own steps.

## Prerequisites

- .NET SDK 8.0 or later, to run `dotnet new` and build the test project
- .NET Framework 4.6.2 targeting pack, for the `Logic` and `Plugins` projects
  (Dataverse plug-ins run only on `.NET Framework 4.6.2`)
- A Dataverse environment and an application user, to deploy and to run the tests
- Power Platform CLI (`pac`), only if you use the early-bound entity generation
  in `Tools/EarlyBound/`

Windows is required for deployment and ILMerge: both tools are Windows-only.

## Links

- [Framework repository](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework)
- [Pillaro.Dataverse.PluginFramework on NuGet](https://www.nuget.org/packages/Pillaro.Dataverse.PluginFramework)
- [Pillaro.Dataverse.PluginFramework.Testing on NuGet](https://www.nuget.org/packages/Pillaro.Dataverse.PluginFramework.Testing)
- [Documentation](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/tree/main/docs)
- [Getting started](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/getting-started.md)
- [Deploying plugins](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/deployment-plugins.md)
- [Visual Studio version of this template](https://marketplace.visualstudio.com/items?itemName=Pillaro.PillaroDataversePluginVisualStudioTemplate)
- License: Apache-2.0 — [LICENSE](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/LICENSE)
