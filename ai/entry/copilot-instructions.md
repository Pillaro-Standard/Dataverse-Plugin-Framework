# GitHub Copilot instructions — Pillaro Dataverse Plugin Framework

This solution follows [`AGENTS.md`](./AGENTS.md): the hard boundaries, the workflow and the rule
catalog. The files below apply the same rules in Copilot's format; if one ever disagrees with the
rule files, the rule files win.

## Hard boundaries

No request justifies breaking these. Details and reasons: [`AGENTS.md`](./AGENTS.md).

1. Business logic lives in `Logic` (tasks and features). Plugin classes only register tasks.
2. Never write or edit files under `EarlyBound/`. A missing entity or attribute means stop and tell
   the developer what to generate — no hand-written partial class, no late-bound workaround.
3. Never invent a step or image GUID, and never copy one from documentation or another solution. Ask.
4. An expected business rejection is `DataverseValidationException` or `ThrowWithWarning(...)` —
   never `ThrowWithError(...)` or `InvalidPluginExecutionException`, which end as `Error`.
5. Never commit a connection string, a secret or `key.snk` content. Never run `deploy`.
6. Integration tests run only when you are told to, and only against a dedicated dev environment.
7. Tests run against real Dataverse. Never mock `IOrganizationService`.

## How to work: plan → tests → implementation

A requirement becomes code in three steps. Each is a skill — type `/` in chat — and ends with a
result a person reviews before the next step starts.

| Step | Skill | Input | Result |
|---|---|---|---|
| 1 | `/pillaro-plan` | requirements in plain language | plan: plugins, tasks, steps, tests — no code |
| 2 | `/pillaro-tests` | one approved task from the plan | integration tests that fail without the task |
| 3 | `/pillaro-implement` | the same task | task, plugin registration, clean build and validation |

After step 3 a person deploys the build. Then the task's tests pass, and so do the tests of every task
sharing its step. The full method, with the intake format and worked examples:
[`analysis-workflow.md`](../../docs/ai/analysis-workflow.md).

## Instructions Copilot applies automatically

| File | Applies to | Rules |
|---|---|---|
| [`plugins`](../../.github/instructions/plugins.instructions.md) | `Plugins/` of the Logic project | [10-plugin](../../docs/ai/rules/10-plugin.md), [60-registration](../../docs/ai/rules/60-registration.md) |
| [`tasks`](../../.github/instructions/tasks.instructions.md) | `Tasks/` and `Features/` of the Logic project | [20-task](../../docs/ai/rules/20-task.md), [30-validation](../../docs/ai/rules/30-validation.md), [40-data-access](../../docs/ai/rules/40-data-access.md), [50-logging-errors](../../docs/ai/rules/50-logging-errors.md) |
| [`tests`](../../.github/instructions/tests.instructions.md) | the test project | [70-testing](../../docs/ai/rules/70-testing.md) |
| [`build-quality`](../../.github/instructions/build-quality.instructions.md) | all C# code | [80-build-quality](../../docs/ai/rules/80-build-quality.md) |

Project paths, namespaces and the reference files: [`project-setup.md`](../project-setup.md).

Before relying on a framework feature, check the version table in
[`AGENTS.md`](./AGENTS.md#the-framework-version-decides-what-you-may-use).
