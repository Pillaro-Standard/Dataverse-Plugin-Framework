# AppSource Submission

How the Pillaro Dataverse Plugin Framework is published to Microsoft AppSource as a
*Dynamics 365 apps on Dataverse and Power Apps* offer, and what has to be in place before a
submission is sent.

> [!NOTE]
> This document is for maintainers. Customers install the offer from AppSource or import the
> managed solutions directly from [`power-platform-solutions`](../power-platform-solutions).

---

## 📖 Table of Contents

| Section | Description |
|---|---|
| [What gets submitted](#what-gets-submitted) | The submission package and how it is built |
| [Offer content](#offer-content) | Where the listing text and certification documents live |
| [Pre-submission checklist](#pre-submission-checklist) | Everything Partner Center asks for |
| [Submission steps](#submission-steps) | The order of operations in Partner Center |
| [Certification](#certification) | What Microsoft validates and what can fail |
| [Publishing a new version](#publishing-a-new-version) | What to update when the solutions change |

---

## What gets submitted

One file: `Pillaro_Dataverse_Plugin_Framework.zip`, produced by the
[MarketplacePackage](../examples/Pillaro.Dataverse.PluginFramework.Examples.MarketplacePackage)
project. Build it from the repository root:

```powershell
dotnet publish examples/Pillaro.Dataverse.PluginFramework.Examples.MarketplacePackage/Pillaro.Dataverse.PluginFramework.Examples.MarketplacePackage.csproj --configuration Release --configfile NuGet.config
```

The result in `bin/Release` contains:

| Entry | Content |
|---|---|
| `package.zip` | Package Deployer package: both managed solutions, the generated `ImportConfig.xml`, and the `PackageImportExtension` assembly |
| `Input.xml` | Provider name, solution anchor, availability window, supported countries |
| `logo32x32.png` | The 32x32 package logo shown during installation |
| `TermsOfUse.html` | The terms every customer accepts when installing |

The build fails if a named solution zip is missing from `power-platform-solutions`, or if
`SolutionAnchorName` in `Input.xml` does not match the framework solution being imported.
Both file names are MSBuild properties in the project and are the only place to change them.

> [!IMPORTANT]
> `StartDate` in `Input.xml` is the date the package becomes available. Nothing in the build
> checks it — verify by hand that it is not in the past before each submission.

## Offer content

Everything Partner Center needs that is not part of the package lives in
[`MarketplaceAssets`](../examples/Pillaro.Dataverse.PluginFramework.Examples.MarketplacePackage/MarketplaceAssets):

| File | Used for |
|---|---|
| `OfferListing.md` | Offer name, search summary, HTML description, keywords, links, contacts, media plan. Paste into the **Offer listing** page. |
| `E2E-FunctionalDocument.md` | The end-to-end functional document the certification team tests against. Convert to PDF. |
| `KeyUsageScenarios.md` | The key usage scenario list. Convert to PDF and upload on the **Supplemental content** page. |
| `CertificationNotes.md` | The **Notes for certification** text. Partner Center does not keep it across resubmissions, so it lives here. |
| `Input.xml`, `TermsOfUse.html`, `logo32x32.png` | Part of the package, described above. |

Partner Center keeps no history, so these files are the source of truth. Change them here
first and paste afterwards.

Partner Center accepts the two certification documents as PDF only. Nothing in the build
produces them — render the Markdown with whatever converter is at hand and keep the Markdown
as the version-controlled original.

## Pre-submission checklist

| Item | Where | Notes |
|---|---|---|
| Microsoft Marketplace account, verified | Partner Center | Enrolment in the Microsoft Marketplace program is a prerequisite for the offer type |
| Submission package built from the current solutions | this repository | See [What gets submitted](#what-gets-submitted) |
| Package uploaded to Azure Blob Storage | Azure | Technical configuration needs a read-only **SAS URL**; set the expiry at least a month out, an expired SAS blocks publishing |
| `StartDate` not in the past | `Input.xml` | |
| Offer name and description | `OfferListing.md` | Description is HTML, 5,000 characters maximum, [supported tags](https://learn.microsoft.com/partner-center/marketplace-offers/supported-html-tags) only |
| Search results summary | `OfferListing.md` | 100 characters maximum |
| Large logo, PNG | — | Partner Center derives the other sizes; no text on the logo, no gradients |
| 1–5 screenshots, exactly 1280x720 PNG, each captioned | — | Blurry images are a rejection reason |
| 1–3 customer-facing marketing PDFs | — | White paper, brochure or checklist |
| Key usage scenario PDF | `KeyUsageScenarios.md` | Supplemental content page |
| End-to-end functional document PDF | `E2E-FunctionalDocument.md` | Certification tests the offer against it |
| Privacy policy URL | — | Must resolve; Partner Center requires one |
| Help URL and Support URL, different from each other | `OfferListing.md` | |
| Support and engineering contacts | — | Phone numbers take digits and spaces only, no dashes |
| Power Apps Checker run against the Marketplace ruleset | Power Platform | Certification runs it; running it first avoids a round trip |

## Submission steps

1. Build the submission package and verify its contents.
2. Upload `Pillaro_Dataverse_Plugin_Framework.zip` to Azure Blob Storage and generate a
   read-only SAS URL.
3. In Partner Center, open **Marketplace offers** and create a
   **Dynamics 365 apps on Dataverse and Power Apps** offer.
4. **Offer setup** — list only, no selling through Microsoft, no app license management,
   listing option **Get it now (free)**.
5. **Properties** — categories, applicable products, app version and legal terms, all in
   `OfferListing.md`. No industries: the offer is not industry-specific, and Microsoft
   Clouds for Industry is only for managed partners and fails certification otherwise.
6. **Offer listing** — paste from `OfferListing.md` and upload the media. The description
   field ignores a value set any way other than typing into it, and appending to what is
   already there scrambles the markup; type the whole HTML in one go into an empty editor,
   save, and reload to confirm it comes back rendered.
7. **Availability** — all 252 markets, matching the country list in `Input.xml`. The package
   availability window itself comes from `Input.xml`.
8. **Technical configuration** — paste the SAS URL from step 2. Base license model:
   **Resource**. Leave the S2S / Secure Store box, the more-than-one-package box and the
   application configuration URL empty; the package needs none of them. Under CRM package
   availability add the public regions only — the sovereign clouds need their own validation.
   The `CredentialsDetectedInUrlField` warning against the SAS URL is expected.
9. **Supplemental content** — upload the key usage scenario PDF.
10. **Review and submit** — paste the notes from `CertificationNotes.md` into **Notes for
    certification**, which takes 2,500 characters at most and is discarded on every
    resubmission. Leave **Marketing only change** off whenever the package content changed,
    even if it sits at the same blob URL.

> [!IMPORTANT]
> Edits made after an offer is live only reach AppSource when the offer is republished.

## Certification

Microsoft validates the offer in five passes. What each one means for this offer:

| Pass | What is checked | Relevant here |
|---|---|---|
| Sanity | Registration type, package artifacts, functional document | The package carries all four required entries |
| Code | Power Apps Checker against the Marketplace ruleset | Run it before submitting; findings come back by e-mail and can be argued as false positives |
| Deployment | Install with Package Deployer, find the components, uninstall cleanly | Both solutions uninstall in reverse order, see section 7 of the functional document |
| Functionality | Every scenario in the functional document must pass | Five scenarios on standard Contact and Task records |
| Security | Custom Package Deployer code, external connections, service accounts, security roles | The package's only deployment code creates the three example configuration records, and it opens no outbound connection and creates no service account; documented in section 2 of the functional document |

Published customisations must not change or remove any out-of-the-box site map. Neither
solution does.

The offer installs a developer framework rather than an end-user business application, and
the examples solution is explicitly not meant for production. The functional document says
so directly in section 8 rather than leaving the certification team to discover it.

## Publishing a new version

When either managed solution is re-exported, its file name carries the new version and the
package stops building until it is updated. In order:

1. Export the solutions into `power-platform-solutions` and archive the previous ones.
2. Update `FrameworkSolutionFile` and `ExamplesSolutionFile` in the project.
3. Update `SolutionAnchorName` in `Input.xml` to the new framework solution file name.
4. Update `StartDate`, and the version line in `E2E-FunctionalDocument.md`.
5. Rebuild, upload the new zip, and update the SAS URL in **Technical configuration**.
6. Republish the offer. Certification runs again.

---

## ➡️ Related documents

- [Versioning](./versioning.md) - Versioning strategy and release model.
- [CI/CD Pipelines](./ci-cd-pipelines.md) - Automated testing, building, and packaging.
- [Model-Driven Application](./solution/model-driven-application.md) - The app the offer installs.
