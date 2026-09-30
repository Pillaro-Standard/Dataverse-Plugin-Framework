# AI instructions — Pillaro Dataverse Plugin Framework

This solution is built on the Pillaro Dataverse Plugin Framework. These instructions apply to every
AI coding agent working in it — Claude Code, GitHub Copilot and Codex read the same rules.

Rules have stable IDs (`PF-XXX-NNN`); reference them in commits, reviews and plans. The rule files
are in [`rules/`](../../docs/ai/rules/). Read the rule file for the area you are touching before you
write code there. Paths and reference files of this solution: [`project-setup.md`](../project-setup.md).

## Hard boundaries — read first

No task justifies breaking these.

1. **PF-ARCH-001/002** — business logic lives in the `Logic` project (tasks and features). Plugin
   classes only register tasks; the `Plugins` project is the deployment shell.
2. **PF-DATA-005/006/007** — never write or edit files under `EarlyBound/`; `pac modelbuilder`
   generates them. A missing type or attribute means stop and tell the developer what to generate —
   never a hand-written partial class, never a late-bound workaround.
3. **PF-REG-002/003** — never invent a step or image GUID, and never copy one from documentation or
   another solution. Ask the developer for a new one.
4. **PF-ERR-001** — an expected business rejection is `DataverseValidationException` (or
   `ThrowWithWarning(...)`): the task ends `Success` with `Info` in the log. Never
   `InvalidPluginExecutionException` or `ThrowWithError(...)`, which end as `Error` in monitoring.
5. **PF-PROC-004/005** — never commit a connection string, a secret or `key.snk` content. Never run
   `deploy`; deployment is done by a person, through the team's pipeline.
6. **PF-ENV-\*** — integration tests run only when you are told to, and only against a dedicated dev
   environment — never test, UAT or production.
7. **PF-TEST-001** — tests run against real Dataverse. Never mock `IOrganizationService` or
   Dataverse behavior.

## Rule catalog

| File | Covers |
|---|---|
| [`00-architecture.md`](../../docs/ai/rules/00-architecture.md) | Where code lives: `Logic` vs `Plugins`, folder layout |
| [`10-plugin.md`](../../docs/ai/rules/10-plugin.md) | Plugin classes, constructor, `Register(...)` |
| [`20-task.md`](../../docs/ai/rules/20-task.md) | Tasks: one responsibility, `AddValidations` vs `DoExecute` |
| [`30-validation.md`](../../docs/ai/rules/30-validation.md) | The validation chain |
| [`40-data-access.md`](../../docs/ai/rules/40-data-access.md) | Data providers, execution context, attribute names, early-bound |
| [`50-logging-errors.md`](../../docs/ai/rules/50-logging-errors.md) | Logging and the `DataverseValidationException` contract |
| [`60-registration.md`](../../docs/ai/rules/60-registration.md) | Steps, images, GUIDs, step naming |
| [`70-testing.md`](../../docs/ai/rules/70-testing.md) | Integration tests, test data, naming, running them |
| [`80-build-quality.md`](../../docs/ai/rules/80-build-quality.md) | Zero-warning build, forbidden patterns |
| [`90-process-security.md`](../../docs/ai/rules/90-process-security.md) | Secrets, deployment, changelog |

## Workflow: requirement → plan → tests → implementation

One requirement maps to one task file. A consultant who does not write C# can drive this; the full
method, the intake format and worked examples are in
[`analysis-workflow.md`](../../docs/ai/analysis-workflow.md). Each step ends with a result a person
reviews before the next one starts:

| Step | Skill | Result |
|---|---|---|
| 1 | `pillaro-plan` | plan: plugins, tasks, steps, tests — no code |
| 2 | `pillaro-tests` | integration tests for one approved task, failing without it |
| 3 | `pillaro-implement` | the task and its registration, clean build and validation |

Then a person deploys, and the task's tests pass — together with the tests of every task sharing
its step. In Claude Code and GitHub Copilot the skills run as `/pillaro-plan` etc.; in Codex as
`$pillaro-plan`. Every tool also picks them up from a plain request such as "plan these
requirements".

## Fast loop — run it yourself after every change

Exact commands: [`verify.md`](../../docs/ai/verify.md).

```text
1. dotnet build "<Logic project>.csproj" -c Release      # zero warnings (PF-BUILD-001)
2. dotnet <cliDll> manifest --assembly <built Plugins DLL> --output artifacts/plugin-manifest.json
3. dotnet <cliDll> validate --manifest artifacts/plugin-manifest.json
```

`<cliDll>` is the CLI bundled in the framework package; its path is the `$cliDll` line of
`Plugins/Tools/Deployment/DeployPlugins.ps1`. A non-zero exit code is a hard stop. If
`dotnet <cliDll> --help` lists only `deploy`, skip steps 2–3 and say so — never install another CLI.

The fast loop cannot catch a merged `Plugins` DLL without `ProxyTypesAssemblyAttribute`
(PF-BUILD-006): it builds and validates, then every early-bound call fails in Dataverse with "not a
known entity type". If you see that message, fix the assembly — never rewrite the task to late-bound.

## The framework version decides what you may use

Read the `Pillaro.Dataverse.PluginFramework` version in the `Logic` project file before relying on a
feature:

| Feature | Needs |
|---|---|
| Update queue `TaskContext.AddEntityToUpdate(...)` (PF-DATA-010), `WithBothImage(...)`, `ServiceUser` | 1.2.0 — on older versions the queue is never written; update the target directly instead |
| CLI `manifest` / `validate`; `ExpectedEnvironmentUrl` check in the test fixture (PF-ENV-006); early-bound namespace default without `.EarlyBound`; these AI instructions | first release after 1.2.2 |

If a feature the work needs is missing, say so in the plan — never assume it silently works.

## Integration tests against a live environment

Only with a connection to a **dedicated dev environment**:

- Read [`70-testing.md`](../../docs/ai/rules/70-testing.md) and its `PF-ENV-*` rules first.
- Check that the test settings set `ExpectedEnvironmentUrl`; the fixture then refuses any other
  environment. Never bypass that check.
- A failing test is fixed by fixing the task, or reported to a person — never by editing the test.
