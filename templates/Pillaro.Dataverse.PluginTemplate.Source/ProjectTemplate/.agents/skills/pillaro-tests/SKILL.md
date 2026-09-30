---
name: pillaro-tests
description: "Step 2 of 3 in the Pillaro Dataverse Plugin Framework workflow. Use when a task from an approved plan needs its integration tests, written before the task is implemented, so they fail first. Takes one task at a time."
argument-hint: "Name of the approved task from the plan"
---

<!-- Managed by pillaro-dv ai-sync (Pillaro Dataverse Plugin Framework). Do not edit: the next sync overwrites this file. Your own rules belong in AGENTS.md, outside the managed block. -->

# Step 2 — Tests first

Write the integration tests for **one task from the approved plan**, before the task is implemented.
The tests describe the behavior the requirement asks for; step 3 then makes them pass.

Rules: [`tests.instructions.md`](../../../.github/instructions/pillaro-tests.instructions.md) and
[`70-testing.md`](../../../.pillaro/ai/rules/70-testing.md). Paths and the reference test:
[`project-setup.md`](../../../.pillaro/project-setup.md).

## What to do

1. Take the task's `tests:` list from the approved plan. Each entry becomes one test method with
   exactly that name. If the task has no plan yet, stop and run `pillaro-plan` first.
2. Create `Tests/<Entity>/<TaskName>Tests.cs` in the shape of the reference test, with the `Owner`
   and `Category` traits. Ask for the owner's initials if you do not know them.
3. Use the entity's repository for test data. If the repository's default record would fail the new
   task, or any other task on the same step, fix the default record in the repository, not in the test.
4. So that the tests compile, create the task class as a stub in `Tasks/<Entity>/<TaskName>.cs` whose
   `AddValidations()` and `DoExecute()` throw `NotImplementedException`. Do not register it in the
   plugin yet — that is step 3.
5. Add every new file (test class, repository, stub) to its project file — otherwise it is silently
   left out of the build (PF-BUILD-007).
6. Build the test project with zero warnings (see [`verify.md`](../../../.pillaro/ai/verify.md)).
7. If you were told to run the tests against the dev environment, first check that the test settings
   set `ExpectedEnvironmentUrl` (PF-ENV-006) — if not, ask. Then run them and report which ones fail
   and why. Failing here is the expected result.

## Report

- The test file and the methods, each with the expected result from the plan.
- Which tests fail before implementation, and which can pass without it. A test such as "a valid
  record is saved" for a task that rejects passes before implementation — that is correct; say so
  instead of changing the test.
- Changes to the repositories, and why.

## Never

- Mock Dataverse or `IOrganizationService` (PF-TEST-001).
- Weaken an assertion so that it passes, or leave out a test from the plan.
- Implement the task in this step.

Next step: `pillaro-implement` for the same task.
