# Framework Comparison

> [!IMPORTANT]
> This page compares approaches to Dataverse plug-in development, not products or companies.
> It is written and maintained by the Pillaro Dataverse Plugin Framework team, so treat it as one perspective, not a neutral third-party review. Corrections and outdated claims can be reported via [Discussions](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/discussions) or [Issues](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/issues).

> [!NOTE]
> Last verified: 2026-09. Ecosystem projects evolve — check the linked repositories for their current state before making a decision.

---

## 📑 Navigation

- [🎯 Scope](#-scope)
- [📊 Comparison table](#-comparison-table)
- [🔍 vs. Vanilla Dataverse SDK](#-vs-vanilla-dataverse-sdk)
- [🔍 vs. spkl](#-vs-spkl)
- [🔍 vs. XrmBedrock](#-vs-xrmbedrock)
- [🧭 When to choose what](#-when-to-choose-what)
- [➡️ Related documents](#️-related-documents)

---

## 🎯 Scope

This comparison covers **plug-in structure, registration, and deployment** — the part of the Dataverse developer ecosystem the Pillaro Dataverse Plugin Framework itself addresses.

It compares:

- **[Vanilla Dataverse SDK](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/plug-ins)** — writing plug-ins directly against `IPlugin` and the CRM SDK, with no additional framework
- **[spkl](https://github.com/scottdurow/SparkleXrm/wiki/spkl)** (part of SparkleXrm) — a JSON-driven command-line deployment tool for plugins, web resources, and early-bound types
- **[XrmBedrock](https://github.com/context-and-oss/XrmBedrock)** — a project template combining Dataverse plug-in/Custom API development with Azure infrastructure and DevOps pipelines

> [!NOTE]
> Testing frameworks (for example FakeXrmEasy, XrmMockup) are intentionally out of scope here. They solve a different problem — mocking or simulating Dataverse for unit tests — while the Pillaro testing package runs integration tests against a real Dataverse environment. A dedicated comparison may be added later.
>
> DLaB.Xrm and Boruto/Kipon Solid Plugin were considered and left out: DLaB.Xrm is a utility/extension-method library rather than an execution framework, and Boruto currently has effectively no adoption to compare against.

---

## 📊 Comparison table

| Dimension | Vanilla SDK | [spkl](https://github.com/scottdurow/SparkleXrm) | [XrmBedrock](https://github.com/context-and-oss/XrmBedrock) | Pillaro Dataverse Plugin Framework |
|---|---|---|---|---|
| Plug-in execution structure | Single `Execute` method per class; structure is up to the developer | Same as vanilla SDK — spkl only handles deployment, not execution structure | Plug-ins/Custom APIs with dependency injection; structure defined per project | Task-based: each plug-in composed of independent, single-responsibility tasks |
| Validation vs. execution | Not a distinct concept | Not a distinct concept | Not documented as a distinct pipeline phase | Explicit fail-fast validation phase, separate from execution, per task |
| Step registration | Manual, via the Plug-in Registration Tool (PRT) GUI or hand-written registration XML | Reflection-based: registration read from attributes on the plug-in assembly, defined in `spkl.json` | Defined in project structure, deployed via DAXIF (F#) scripts | Fluent, code-defined registration API, source of truth alongside the plug-in code |
| Deployment idempotency (no duplicate steps) | Developer's responsibility | Depends on how the deployment task is used; not a stated design goal | Not independently verified for this comparison | Deterministic sync: creates/updates the intended steps, images, and filtering attributes without duplicates |
| Dependency injection | Not built in | Not built in | Built in | Built in (Autofac-based) |
| Early-bound entity generation | Manual (`CrmSvcUtil` or `pac modelbuilder`) | Built-in task via `spkl.json` | Generated as part of the Dataverse context (F# Daxif scripts) | Built-in local tooling wrapping `pac modelbuilder` |
| Runtime diagnostic logging | `ITracingService` only | Not provided | Not a stated focus area | Built-in structured logging (task-level messages and details) with a companion model-driven app |
| Runtime configuration / feature flags | Not provided | Not provided | Not a stated focus area | Built-in runtime settings service, editable without redeployment |
| Integration testing story | Not provided | Not provided | Not a stated focus area (see Scope note on testing frameworks) | Built-in xUnit integration testing package against a real Dataverse environment |
| Project scope | N/A | Narrow: build/deploy task runner only | Broad: full project template incl. Azure infrastructure (Bicep), DevOps pipelines, TypeScript generation | Focused: plug-in framework + deployment + testing + diagnostics, no Azure/DevOps scaffolding |
| License | N/A | MIT | MIT | Apache-2.0 |
| Activity (as of 2026-09) | N/A | Low — no commits since mid-2024 | Active | Active |

---

## 🔍 vs. Vanilla Dataverse SDK

Writing plug-ins directly against `IPlugin` gives full control and zero dependencies, but every project ends up re-solving the same problems: where validation lives, how to avoid duplicate step registrations after repeated deployments, and how to get useful diagnostics out of a sandboxed plugin process. The framework exists to provide default answers to those problems without removing access to the underlying SDK.

## 🔍 vs. spkl

spkl is a lightweight, well-known deployment tool — a JSON config plus a command-line task runner for pushing plugins, web resources, and early-bound types to Dataverse. It does not prescribe an execution or validation structure for the plug-in code itself, so it can be paired with any coding style, including this framework's. The overlap is mainly in early-bound generation and deployment; the framework additionally provides the task/validation execution model, runtime logging, runtime configuration, and integration testing that spkl does not address.

## 🔍 vs. XrmBedrock

XrmBedrock is a broader project template: it scaffolds an entire Dataverse + Azure solution, including infrastructure as code, DevOps pipeline configuration, and TypeScript client generation, with dependency injection for plug-ins and Custom APIs. It is a good fit for teams that want an opinionated, end-to-end template spanning Dataverse and Azure. The Pillaro framework is narrower in scope by design — it focuses on the plug-in layer (task structure, validation, deployment, logging, testing) and does not prescribe Azure infrastructure or pipeline tooling, so it can be adopted incrementally inside an existing project structure, including one built on XrmBedrock.

---

## 🧭 When to choose what

- **Full control, minimal dependencies, and an existing team convention** → vanilla SDK
- **A simple, proven deployment/build task runner, keeping your own plug-in architecture** → spkl
- **A complete, opinionated Dataverse + Azure project template with DevOps pipelines built in** → XrmBedrock
- **A structured task-based plug-in architecture with deterministic deployment, built-in diagnostics, runtime configuration, and integration testing, without prescribing your Azure/DevOps setup** → Pillaro Dataverse Plugin Framework

These aren't mutually exclusive. spkl and the Pillaro framework can coexist, and the framework can be adopted inside a project structured like XrmBedrock.

---

## ➡️ Related documents

- [Architecture](./plugins/architecture.md) — the framework's own plug-in architecture
- [Deployment Plugins](./plugins/deployment-plugins.md) — deterministic deployment in detail
- [Testing Overview](./tests/testing.md) — integration testing against Dataverse
- [Logging](./plugins/logging.md) — runtime diagnostic logging
