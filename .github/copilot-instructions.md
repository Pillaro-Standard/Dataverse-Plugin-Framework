# GitHub Copilot instructions

This repository follows [`AGENTS.md`](../AGENTS.md): the hard boundaries, the workflow, and the rule
catalog in [`docs/ai/rules/`](../docs/ai/rules/). The files under `.github/` apply the same rules in
Copilot's format. If anything here disagrees with `docs/ai/rules/`, the rules win — correct the file
here, not the rules.

## Hard boundaries

No request justifies breaking these. Details and reasons: [`AGENTS.md`](../AGENTS.md).

1. Business logic lives in `Logic` (tasks and features). Plugin classes only register tasks.
2. Never write or edit files under `EarlyBound/`. A missing entity or attribute means stop and tell
   the developer what to generate — no hand-written partial class, no late-bound workaround.
3. Never invent a step or image GUID, and never copy one from `/docs` or `/examples`. Ask a human.
4. An expected business rejection is `ThrowWithWarning(...)` or `DataverseValidationException` —
   never `ThrowWithError(...)` or `InvalidPluginExecutionException`, which end as `Error`.
5. Never commit a connection string, a secret or `key.snk` content. Never run `deploy`.
6. Integration tests run only when you are told to, and only against a dedicated dev environment.
7. Tests run against real Dataverse. Never mock `IOrganizationService`.

## How to work: plan → tests → implementation

A requirement becomes code in three steps, each with its own prompt. Every step ends with a result a
person reviews before the next one starts.

| Step | Prompt | Input | Result |
|---|---|---|---|
| 1 | `/analyze-and-plan` | requirements in plain language | plan: plugins, tasks, steps, tests — no code |
| 2 | `/generate-task-tests` | one approved task from the plan | integration tests that fail without the task |
| 3 | `/generate-task-and-plugin` | the same task | task, plugin registration, clean build and validation |

After step 3 a person deploys the build. Then the task's tests pass, and so do the tests of every task
sharing its step. The full method, with the intake format and worked examples, is in
[`docs/ai/analysis-workflow.md`](../docs/ai/analysis-workflow.md).

## Instructions Copilot applies automatically

| File | Applies to | Rules |
|---|---|---|
| [`plugins.instructions.md`](./instructions/plugins.instructions.md) | `Plugins/` of the Logic project | [10-plugin](../docs/ai/rules/10-plugin.md), [60-registration](../docs/ai/rules/60-registration.md) |
| [`tasks.instructions.md`](./instructions/tasks.instructions.md) | `Tasks/` and `Features/` of the Logic project | [20-task](../docs/ai/rules/20-task.md), [30-validation](../docs/ai/rules/30-validation.md), [40-data-access](../docs/ai/rules/40-data-access.md), [50-logging-errors](../docs/ai/rules/50-logging-errors.md) |
| [`tests.instructions.md`](./instructions/tests.instructions.md) | the test project | [70-testing](../docs/ai/rules/70-testing.md) |
| [`build-quality.instructions.md`](./instructions/build-quality.instructions.md) | all C# code | [80-build-quality](../docs/ai/rules/80-build-quality.md) |

They are written for a solution built on the framework (in this repository: `/examples`). The
framework's own code in `src/` and its offline tests in `tests/` follow
[`CONTRIBUTING.md`](../docs/CONTRIBUTING.md) instead.

Project paths, namespaces and the reference files to copy the code shape from:
[`project-setup.md`](./project-setup.md).

## Before you rely on a framework feature

Read the `Pillaro.Dataverse.PluginFramework` version in the `Logic` project file and check the table
in [`AGENTS.md` — The framework version decides what you may use](../AGENTS.md#-the-framework-version-decides-what-you-may-use).
If a feature the plan needs is missing, say so in the plan.
