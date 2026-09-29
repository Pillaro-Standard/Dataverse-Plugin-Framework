---
description: "Step 3 of 3 — implement one approved task and its plugin registration, then build and validate."
argument-hint: "Name of the approved task from the plan"
agent: agent
---

# Step 3 — Implement the task and its registration

Implement **one task from the approved plan** so that its tests from step 2 pass after deployment.

Rules: [`tasks.instructions.md`](../instructions/tasks.instructions.md),
[`plugins.instructions.md`](../instructions/plugins.instructions.md),
[`build-quality.instructions.md`](../instructions/build-quality.instructions.md). Paths and the
reference files: [`project-setup.md`](../project-setup.md).

## Before you write code

Check that every entity the task uses has an early-bound type:

1. The entity is listed in `entityNamesFilter` in `EarlyBoundSettingsFile`.
2. `EarlyBoundEntityFile` for it exists in **this** `Logic` project, with the attributes the task
   needs. Another project's early-bound files do not count.

If either is missing, stop. Tell the developer to add the entity to `EarlyBoundSettings.json` and run
`EarlyBoundGenerator`. Never edit `EarlyBound/` and never write late-bound code to get around it
(PF-DATA-005–007).

## What to do

1. **Task** — replace the stub from step 2 in `Tasks/<Entity>/<TaskName>.cs`:
   - `AddValidations()` from the contract's `trigger`, `preconditions` and `rules`,
   - `DoExecute()` from the rule descriptions and `dataAccess`,
   - supporting logic in `Features/<FeatureName>/` only when it is large or shared.
2. **Plugin** — in the plugin named by the plan:
   - add `RegisterTask<TaskName>(...)` at the position the plan gives (after the tasks it depends on),
   - in `Register(...)`, extend the existing step or add the new one exactly as the plan says. A new
     step or image needs a GUID from the developer — ask, never invent one (PF-REG-002/003).
3. **Project file** — add every new file to the `Logic` project file (PF-BUILD-007).
4. **Fast loop** from [`verify.md`](../../docs/ai/verify.md): build with zero warnings, then
   `manifest` and `validate`. A non-zero exit code is a stop — fix and repeat. If `validate` fails
   only because a GUID is still an all-zero placeholder, stop and report which step or image needs
   one.
5. **Hand over.** Deployment is done by a person (PF-PROC-005). After it, the task's tests and the
   tests of every task in `sharesStepWith` are run against the dev environment.

## Report

- Files changed, and what the plan said for each.
- The build, `manifest` and `validate` results. If the CLI has only `deploy`, say that `manifest` and
  `validate` were skipped.
- GUIDs still to be supplied, and anything that deviates from the plan, with the reason.

## Never

- Change the tests from step 2 to fit the implementation (PF-ENV-007). If a test is wrong, say so
  and ask.
- Run `deploy`, or change registration beyond what the plan says.
