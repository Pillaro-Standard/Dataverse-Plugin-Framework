---
name: pillaro-plan
description: "Step 1 of 3 in the Pillaro Dataverse Plugin Framework workflow. Use when someone brings business requirements for Dataverse plugins — a consultant's analysis, a ticket, a change request — and wants them turned into a reviewable plan of plugins, tasks, plug-in steps and tests. Writes no code."
argument-hint: "Paste the requirements, or the path to a file with them"
---

# Step 1 — Analyze and plan

Turn business requirements written in plain language into a plan a consultant can review without
reading C#. **Write no code in this step.** The plan is the cheap checkpoint: changing it is a text
edit, changing code later costs more.

Follow [`analysis-workflow.md`](../../../docs/ai/analysis-workflow.md) sections 1–3 and use its
formats. Paths and names of this solution: [`project-setup.md`](../../project-setup.md). The rules
are indexed in [`AGENTS.md`](../../entry/AGENTS.md).

## What to do

1. **Restate each requirement** in the intake shape of `analysis-workflow.md` §1 (`requirement`,
   `entity`, `trigger`, `rule`, `outcome`). If something the plan needs is not stated — the trigger,
   a value, the rejection text — ask. Never invent it.
2. **Check the framework version** in the `Logic` project file against the version table in
   `AGENTS.md`. A feature the plan needs but the version does not have goes into the plan as a finding.
3. **Plugin breakdown** (§2): group the requirements by entity or capability. Look in `Plugins/` of
   the `Logic` project for an existing `<Entity>Plugin` first — usually the plan extends it.
4. **Find the step for each task**: entity × message × stage × mode. Read the plugin's `Register(...)`
   to see whether that step exists. If it does, the task joins it: list the filtering attributes and
   image columns it adds and the tasks already on it (PF-REG-010).
5. **Task breakdown** (§3): one task per requirement, split when a requirement bundles two unrelated
   rules. Write the YAML contract for each task, including `registration:`, `sharesStepWith:` and
   `tests:`. Name tasks by the verb vocabulary in
   [`20-task.md`](../../../docs/ai/rules/20-task.md#naming-vocabulary-pf-task-007).
6. **Stop and present the plan.** Wait for approval or changes; fold changes in and present it again.

## Output

For each requirement, the block from "What the plan looks like when it is done" in
`analysis-workflow.md`:

```text
R3 — Normalize the customer's phone number
  Plugin:      ContactPlugin (existing)                      — extend
  Task:        SetNormalizedPhoneNumber (new)                 Tasks/Contact/SetNormalizedPhoneNumber.cs
  Step:        contact Create+Update PreOperation Sync        existing; + filtering attribute telephone1
  Runs after:  SetMandatoryFields    Runs before: CheckDuplicity (reads the normalized number)
  Rejects:     —
  Tests:       Tests/Contact/SetNormalizedPhoneNumberTests.cs
               CreateContact_WithLocalNumber_StoresInternationalFormat         success
               UpdateContact_ChangingPhone_RenormalizesNumber                  success
               + re-run: CheckDuplicityTests, RestrictStatusChangeTests       (shares the step)
```

Then the YAML contract of each task, then a list of open questions and findings: missing values,
entities without early-bound types, framework version gaps, step or image GUIDs that will be needed.

## Never

- Invent a business rule, a value, a trigger or a rejection message. A rejection message is copied
  verbatim, in the users' language (PF-ERR-004).
- Propose a step or image GUID (PF-REG-002/003).
- Skip the review and continue to tests or code "to save a round trip".

Next step, after the plan is approved: `pillaro-tests` for one task.
