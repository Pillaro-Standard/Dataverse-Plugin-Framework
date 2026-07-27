# Pillaro Dataverse Plugin Framework Marketplace Package

This project creates the Microsoft Marketplace submission package for the Pillaro Dataverse Plugin Framework example experience.

The package contains only the existing managed solutions; it does not export solutions or import sample/configuration data:

1. `PillaroFramework_1_0_0_1_managed.zip`
2. `PillaroPluginFrameworkExamples_1_0_0_0_managed.zip`

The Framework solution is imported first and is also used as the Marketplace solution anchor.

## Build

From the repository root, run:

```powershell
dotnet publish examples/Pillaro.Dataverse.PluginFramework.Examples.MarketplacePackage/Pillaro.Dataverse.PluginFramework.Examples.MarketplacePackage.csproj --configuration Release --configfile NuGet.config
```

The publish produces two ZIP files in the project's `bin/Release` directory:

- `package.zip` — Package Deployer package containing the two managed solutions.
- `Pillaro_Dataverse_Plugin_Framework.zip` — final Marketplace submission package. Upload this file to Azure Blob Storage and provide its read-only SAS URL in Partner Center.

## Marketplace metadata

Marketplace metadata is stored under `MarketplaceAssets`. Before submitting a new release, review:

- availability dates and supported countries in `Input.xml`;
- the managed solution filenames and versions in the project and `Input.xml`;
- the English license terms and 32×32 package logo.

The package structure follows the Microsoft Learn guidance for [creating a Marketplace package](https://learn.microsoft.com/power-platform/developer/marketplace/create-package-app).
