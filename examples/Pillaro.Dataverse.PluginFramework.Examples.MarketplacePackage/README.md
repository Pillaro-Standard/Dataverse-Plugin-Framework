# Pillaro Dataverse Plugin Framework Marketplace Package

This project creates the Microsoft Marketplace submission package for the Pillaro Dataverse Plugin Framework example experience.

The package contains only the existing managed solutions; it does not export solutions or import sample/configuration data:

1. `PillaroFramework_1_0_0_2_managed.zip`
2. `PillaroPluginFrameworkExamples_1_0_0_1_managed.zip`

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

Marketplace metadata is stored under `MarketplaceAssets`:

| File | Ships in the package | Used for |
|---|---|---|
| `Input.xml` | yes | Provider, solution anchor, availability window, supported countries |
| `logo32x32.png` | yes | Package logo shown during installation |
| `TermsOfUse.html` | yes | Terms the customer accepts when installing |
| `OfferListing.md` | no | Partner Center offer listing content: name, description, links, media plan |
| `E2E-FunctionalDocument.md` | no | The document certification tests the offer against; convert to PDF |
| `KeyUsageScenarios.md` | no | Supplemental content in Partner Center; convert to PDF |

Before submitting a new release, review:

- availability dates and supported countries in `Input.xml`, and check `StartDate` is not in the past;
- the managed solution filenames and versions in the project and `Input.xml`;
- the license terms, the 32×32 package logo, and the version line in `E2E-FunctionalDocument.md`.

The package structure follows the Microsoft Learn guidance for [creating a Marketplace package](https://learn.microsoft.com/power-platform/developer/marketplace/create-package-app).

The full submission process and the Partner Center checklist are in
[AppSource Submission](../../docs/marketplace-appsource.md).
