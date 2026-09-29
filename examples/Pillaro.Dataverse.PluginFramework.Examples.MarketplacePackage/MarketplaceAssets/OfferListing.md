# AppSource offer listing content

Source of truth for the **Offer listing** page of the *Dynamics 365 apps on Dataverse and
Power Apps* offer in Partner Center. Paste the fields below into Partner Center; keep this
file in sync when the listing changes, because Partner Center is not versioned.

Lines marked **TODO** are the ones nobody can fill in from this repository.

The offer is **live on AppSource**. It passed certification on 28 September 2026 and was
published the same day. Every page is complete; what is below is what it says.

Partner Center keeps no history and shows no diff, so this file is the only record of what
the listing said and why.

## Marketplace details

| Field | Value |
|---|---|
| Offer alias (internal) | `pillaro-dataverse-plugin-framework` |
| Name (max 200 chars) | Pillaro Dataverse Plugin Framework — Demo |
| Search results summary (max 100 chars) | Task-based framework for scalable, testable Dataverse plug-ins with exact deployment. |
| Search keywords (max 3) | `plug-in framework`, `Dataverse development`, `plugin logging` |
| Products your app works with (max 3) | Microsoft Dataverse, Power Apps, Dynamics 365 Sales |

The summary is 85 characters. Partner Center rejects anything over 100.

## Description

Up to 5,000 characters, HTML. Only the tags listed in [Supported HTML
tags](https://learn.microsoft.com/partner-center/marketplace-offers/supported-html-tags) are
allowed. The text below is 4396 characters once the indentation is collapsed to single spaces,
which is how Partner Center counts it.

The listing opens by saying the offer is a demonstration and that production use means the
NuGet package. That is deliberate: the package installs the framework runtime *and* worked
examples, and a customer who installs it expecting the production path would be surprised.

*Try it in five minutes* exists because a reviewer and a customer both need to know what to
do after the install finishes. It is the same three scenarios as the functional document,
cut down to what fits on a listing page.

```html
<p>The Pillaro Dataverse Plugin Framework gives development teams a consistent way to build, deploy and operate Microsoft Dataverse plug-ins. It replaces hand-registered steps and large single-purpose classes with a task-based model in which every unit of behaviour is declared, validated and executed the same way, and in which the registration metadata lives next to the code it registers.</p>

<p>This offer is a <strong>demonstration</strong>. It installs the framework runtime together with worked examples so you can see the framework running on real records before you write any code. For production you consume the framework as a <a href="https://www.nuget.org/profiles/Pillaro">NuGet package</a> in your own plug-in project. The framework is listed on <a href="https://learn.microsoft.com/en-us/power-apps/developer/data-platform/community-tools#pillaro-dataverse-plugin-framework">Microsoft Learn among Community Tools for Microsoft Dataverse</a>.</p>

<h2>What happens when you install</h2>

<ul>
<li>Two managed solutions are imported: the framework runtime, then the examples.</li>
<li>A new model-driven app, <strong>Pillaro Plugin Framework</strong>, appears in your app list. It is where you manage Runtime Settings and Autonumbering and read the Plugin Logs.</li>
<li>The configuration the examples need is created for you, so there is nothing to set up by hand.</li>
<li>Example plug-in steps are registered on the standard <strong>Contact</strong> and <strong>Task</strong> tables. No other table, form or site map is touched.</li>
</ul>

<h2>Try it in five minutes</h2>

<ol>
<li><strong>Create or update a Contact.</strong> Set the first or last name to <em>Admin</em> or <em>Test</em> and the save is blocked — the forbidden words come from a Runtime Setting, not from compiled code, so changing the setting changes the rule with no redeployment. Fill in the address fields and <em>Address 1: Name</em> is generated for you.</li>
<li><strong>Create a Task on that Contact.</strong> Its subject is prefixed with a generated number such as 26-09-21-001000, and the Contact description picks up the activity date from the Task.</li>
<li><strong>Open the Pillaro Plugin Framework app and read the Plugin Logs.</strong> Every plug-in and task that ran is there, with its messages, the input context, execution time and nesting depth — including the reason the blocked save failed.</li>
</ol>

<h2>What the framework gives you</h2>

<ul>
<li><strong>Runtime settings</strong> — behaviour driven by Dataverse records, changed without a redeployment and cached so the lookup costs nothing.</li>
<li><strong>Autonumbering</strong> — concurrency-safe sequences with configurable format, digit count and parent-based numbering.</li>
<li><strong>Plugin logs</strong> — the full execution flow of every plug-in and task. Most troubleshooting stops needing a debugger.</li>
<li><strong>Deterministic deployment</strong> — assemblies, steps, filtering attributes, images, rank and solution membership are declared in code and synchronised by the deployment tooling. Running a deployment twice produces the same environment and leaves no duplicate steps.</li>
<li><strong>Testing</strong> — a companion package runs functional tests against a live Dataverse environment, creating the data each test needs and cleaning up afterwards.</li>
</ul>

<h2>Who it is for</h2>

<p>Development teams and partners who deliver Dataverse and Dynamics 365 customisations in C# and need deployments to be reproducible across environments.</p>

<h2>Developer tooling</h2>

<ul>
<li><strong>Project templates</strong> — start a new Pillaro-based project with the templates for <a href="https://marketplace.visualstudio.com/items?itemName=Pillaro.PillaroDataversePluginVisualStudioTemplate">Visual Studio</a> or <a href="https://www.nuget.org/packages/Pillaro.Dataverse.PluginTemplate.DotNetNew">Visual Studio Code and the .NET CLI</a>.</li>
<li><strong>NuGet packages</strong> — add the framework and its testing support to an existing C# project.</li>
</ul>

<p>Installation instructions, examples and full documentation are in the <a href="https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework">Pillaro Dataverse Plugin Framework repository on GitHub</a>.</p>

<h2>Licence and cost</h2>

<p>Apache-2.0, free of charge including for commercial use. Uninstalling both solutions removes everything they added.</p>
```

## Links and contacts

| Field | Value |
|---|---|
| Help link for your app | `https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/tree/main/docs` |
| Support URL (must differ from Help) | `https://pillaro.cz/kontakty` &mdash; a page with a phone number, an email address and a contact form. The issue tracker was rejected under policy 100.5.13: a support link has to lead to contact details, and the report named this URL. |
| Privacy policy link | `https://pillaro.cz/privacy-policy---dataverse-plugin-framework` |
| Support contact | Ján Mucha, `support@pillaro.cz`, `+420 604 646 526` |
| Engineering contact | Ján Mucha, `support@pillaro.cz`, `+420 604 646 526` |
| Product information link | `GitHub repository` → `https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework` |

AppSource has no equivalent of the Visual Studio Marketplace *Project Details* panel, which
builds itself from the `repo` field in the VSIX manifest. On AppSource a repository link has
to be added by hand under *Supplemental product information for customers*.

## Media

All images must be PNG. Blurry assets are a rejection reason.

| Asset | Requirement | Status |
|---|---|---|
| Large logo | PNG, Partner Center derives the other sizes from it. No text on the logo, no white/black/blue on a transparent background, no gradients. | **TODO** |
| Screenshots | 1 to 5, exactly 1280 x 720 PNG, each with a caption. | **TODO** |
| Videos | Optional, up to 4, externally hosted, each with a 1280 x 720 PNG thumbnail. | Optional |
| Supporting documents | 1 to 3 customer-facing PDFs (white paper, brochure, checklist). | **TODO** |

Suggested screenshots, in order, all from the Pillaro Plugin Framework app:

1. Plugin Logs list with records from an example run &mdash; caption: *Every plug-in and task
   execution is logged with context, duration and nesting depth.*
2. An open Plugin Log record showing the execution messages &mdash; caption: *Step-by-step
   execution detail replaces attaching a debugger.*
3. Runtime Settings list with `MinimalSeverityLevel` and `ForbiddenWords` &mdash; caption:
   *Behaviour is driven by Dataverse records and changes without a redeployment.*
4. Autonumberings record for the Task table &mdash; caption: *Concurrency-safe sequences with
   a configurable format.*
5. A Contact save blocked by the validation example &mdash; caption: *Validation runs before
   execution and fails fast with a traceable reason.*

## Offer setup

| Field | Value |
|---|---|
| Sell through Microsoft | No &mdash; list only |
| App license management through Microsoft | Off |
| Listing option | Get it now (free) |
| Customer leads | Not required for a free listing |

## Properties

| Field | Value |
|---|---|
| Primary category | IT & Management Tools |
| Subcategories | Business Applications, Management Solutions |
| Industries | none &mdash; the offer is not industry-specific |
| Microsoft Clouds for Industry | off &mdash; only for managed partners, selecting it fails certification |
| Applicable products | Power Apps |
| App version | 1.0.0.3, tracking the framework solution |
| Legal | Terms and conditions **text**, the plain-text rendering of [`TermsOfUse.html`](TermsOfUse.html) |

Only Power Apps is claimed under applicable products. The plug-ins run on core Dataverse and
work under Sales, Customer Service and the rest, but certification validates functionality
per product claimed, and the scenarios in the functional document are the Contact and Task
tables rather than anything specific to those apps.

## Availability

| Field | Value |
|---|---|
| Markets | all 252 |
| Preview audience hide key | auto-generated by Partner Center |

The package availability window lives in [`Input.xml`](Input.xml), not in Partner Center.
`StartDate` there is the date the package becomes available and must not be in the past at
submission time.

## Technical configuration

| Field | Value |
|---|---|
| Base license model | Resource |
| Requires S2S outbound and CRM Secure Store Access | **off** |
| Application configuration URL | empty |
| URL of your package location | the Azure Blob SAS URL of `Pillaro_Dataverse_Plugin_Framework.zip` |
| There is more than one CRM package in my package file | **off** &mdash; the zip carries one `package.zip` |
| CRM package availability | the 20 public regions; none of the six sovereign clouds |

Partner Center raises a `CredentialsDetectedInUrlField` warning against the package URL. A
SAS token is a credential in a URL by construction and Microsoft's own instructions ask for
one, so the warning is expected and does not block publishing.

The sovereign clouds &mdash; US Gov, US Gov High, US DoD, China, Germany Sovereign and Test in
production &mdash; are left unselected. They need their own validation and the framework has
not been tested there.
