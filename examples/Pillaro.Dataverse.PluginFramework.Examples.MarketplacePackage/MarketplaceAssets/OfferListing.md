# AppSource offer listing content

Source of truth for the **Offer listing** page of the *Dynamics 365 apps on Dataverse and
Power Apps* offer in Partner Center. Paste the fields below into Partner Center; keep this
file in sync when the listing changes, because Partner Center is not versioned.

Lines marked **TODO** are the ones nobody can fill in from this repository.

## Marketplace details

| Field | Value |
|---|---|
| Offer alias (internal) | `pillaro-dataverse-plugin-framework` |
| Name (max 200 chars) | Pillaro Dataverse Plugin Framework |
| Search results summary (max 100 chars) | Task-based framework for scalable, testable Dataverse plug-ins with exact deployment. |
| Search keywords (max 3) | `plug-in framework`, `Dataverse development`, `plugin logging` |
| Products your app works with (max 3) | Microsoft Dataverse, Power Apps, Dynamics 365 Sales |

The summary is 85 characters. Partner Center rejects anything over 100.

## Description

Up to 5,000 characters, HTML. Only the tags listed in [Supported HTML
tags](https://learn.microsoft.com/partner-center/marketplace-offers/supported-html-tags) are
allowed. The text below is 2,900 characters including markup.

```html
<p>
  The Pillaro Dataverse Plugin Framework gives development teams a consistent way to build,
  deploy and operate Microsoft Dataverse plug-ins. It replaces hand-registered steps and
  large single-purpose plug-in classes with a task-based model in which every unit of
  behaviour is declared, validated and executed the same way, and in which the registration
  metadata lives next to the code it registers.
</p>

<p><b>What you get after installing</b></p>

<ul>
  <li>
    <b>Runtime settings</b> &mdash; plug-in behaviour driven by Dataverse records, changed
    without a redeployment and cached so the lookup costs nothing at runtime.
  </li>
  <li>
    <b>Autonumbering</b> &mdash; concurrency-safe sequences with configurable format, digit
    count and parent-based numbering.
  </li>
  <li>
    <b>Plugin logs</b> &mdash; the full execution flow of every plug-in and task, with
    context data, execution time and nesting depth, stored in Dataverse and readable from
    the included model-driven app. Most troubleshooting stops needing a debugger.
  </li>
  <li>
    <b>The Pillaro Plugin Framework app</b> &mdash; a model-driven app for administrators to
    manage the settings, sequences and logs above, with security roles for read-only and
    full access.
  </li>
  <li>
    <b>Worked examples</b> &mdash; example plug-ins on the standard Contact and Task tables
    that demonstrate validation, runtime settings, autonumbering, related-record sync and
    logging end to end.
  </li>
</ul>

<p><b>Who it is for</b></p>

<p>
  Development teams and partners who deliver Dataverse and Dynamics 365 customisations in
  C# and need deployments to be reproducible across environments. The framework itself is
  consumed as a NuGet package in the plug-in project; this offer installs the Dataverse-side
  runtime that the package depends on.
</p>

<p><b>Deterministic deployment</b></p>

<p>
  Plug-in assemblies, steps, filtering attributes, images, rank, configuration and solution
  membership are declared in code and synchronised by the deployment tooling. Running a
  deployment twice produces the same environment, and repeated deployments do not leave
  duplicate steps behind. Developers deploy locally with the same source of truth that an
  Azure DevOps pipeline uses.
</p>

<p><b>Testing</b></p>

<p>
  A companion testing package runs functional tests against a live Dataverse environment,
  creating the data each test needs, isolating test runs from each other and cleaning up
  afterwards. The framework's own test suite runs nightly against a live environment.
</p>

<p><b>Licence and cost</b></p>

<p>
  Apache-2.0, free of charge including for commercial use. The source, the documentation and
  the issue tracker are public on GitHub.
</p>

<p><b>Before you install</b></p>

<p>
  The Pillaro Framework solution is the runtime layer and is installed first. The examples
  solution is installed on top of it and is intended for learning, demos and validating the
  framework &mdash; not for production environments.
</p>
```

## Links and contacts

| Field | Value |
|---|---|
| Help link for your app | `https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/tree/main/docs` |
| Support URL (must differ from Help) | `https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/issues` |
| Privacy policy link | **TODO** &mdash; a published privacy policy URL. Partner Center requires one and certification checks that it resolves. |
| Support contact | **TODO** &mdash; name, e-mail, phone (digits and spaces only, no dashes) |
| Engineering contact | **TODO** &mdash; name, e-mail, phone (digits and spaces only, no dashes) |

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

## Availability

Countries and the availability window live in [`Input.xml`](Input.xml), not in this file.
`StartDate` there is the date the package becomes available and must not be in the past at
submission time.
