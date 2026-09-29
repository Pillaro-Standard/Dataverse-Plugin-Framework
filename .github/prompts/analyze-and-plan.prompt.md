# analyze-and-plan.prompt.md

## Purpose

Turn a plain-language business requirement (written by a consultant/analyst, no C#) into a reviewable
implementation plan: a plugin breakdown, a task breakdown with a technical contract per task, and a
test list — **before any code is written**.

Full method and worked examples: `docs/ai/analysis-workflow.md`. Load and follow it — do not
improvise a different format.

## Instructions for Copilot

1. Load `docs/ai/analysis-workflow.md`, `.github/project-setup.md`, and `.github/copilot-instructions.md`.
2. Ask for (or accept, if already given) one or more requirements in the "Requirement intake" shape
   from `docs/ai/analysis-workflow.md` §1. If the user gives you a free-text requirement instead,
   restate it in that shape first and confirm it captures the rule correctly before continuing —
   do not guess at a rejection message or a trigger condition that wasn't stated.
3. Produce the **plugin breakdown** (§2): group requirements by entity/functional area, name each
   plugin, and say which requirements go in which plugin group.
4. Produce the **task breakdown** (§3): one task per requirement unless a requirement clearly bundles
   two unrelated rules, using the YAML contract shape from §3 (entity, messages, stage, mode, trigger,
   preconditions, rules, dataAccess, logging, tests).
5. Stop here and present the plan. Do not generate plugin, task, or test code in this step — that is
   `generate-task-and-plugin.prompt.md` and `generate-task-tests.prompt.md`, run only after the human
   reviewing the plan approves it (or asks for changes, which you fold back into the plan and
   re-present).

## What this prompt must never do

- Never invent a business rule, message text, or trigger condition that the requirement did not
  state — ask instead.
- Never skip straight to code generation "to save a round trip" — the review checkpoint is the point
  of this step.
- Never invent a step ID, image ID, or entity that isn't confirmed to exist (see the pre-check in
  `generate-task-and-plugin.prompt.md`) — that check still applies once execution starts.

## Output format

```text
## Plugin breakdown
- <PluginName> (entity: <entity>) — existing: extend | new
    ← requirements: R1, R2
    R1 -> step "<entity> <Message> <Stage> <Mode>" (existing | new)
          + filtering attributes / image columns this adds
          shares the step with: <existing tasks on that step>

## Task breakdown
### <TaskName> (plugin: <PluginName>)
<the YAML contract from docs/ai/analysis-workflow.md §3, including registration: and sharesStepWith:>

## Tests to write first
- <test name> — expect: <success | rejected + message text>
- re-run after deploy: <tests of every task in sharesStepWith>
...
```

Before proposing a new plugin, check `Plugins/` for an existing `<Entity>Plugin` — in a mature
solution the usual change is a new task on an existing step (PF-REG-010 in
`docs/ai/rules/60-registration.md`). A rejection message is copied verbatim from the requirement, in
the users' language — never translated or invented (PF-ERR-004).
