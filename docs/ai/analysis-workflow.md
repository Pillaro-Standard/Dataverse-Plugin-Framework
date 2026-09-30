# Analysis → Plugin → Task Workflow

> [!IMPORTANT]
> Read [`AGENTS.md`](../../AGENTS.md) first. This document is the detail behind its
> "Analysis → plugin → task workflow" section, written so a **consultant or analyst who does not
> write C#** can drive it, with an AI agent doing the breakdown and, later, the implementation.

## Who this is for

A delivery consultant gathering requirements for a Dataverse solution can produce the **intake**
below without touching code. An AI agent (or a developer) turns that intake into a **plan**
(plugins, tasks, tests) for review, and only after the plan is approved does it write code. This
keeps the expensive, hard-to-undo step — implementation — behind a cheap, human-readable checkpoint.

```text
requirement intake  →  plan (plugins + tasks + tests)  →  human review/adjust  →  execute (TDD)
     (consultant)              (AI)                          (consultant/dev)         (AI)
```

## 1. Requirement intake

One entry per business requirement. Plain language, no C#:

```yaml
requirement: R1
title: Keep the account's email in sync with its primary contact
entity: account                      # the record the rule is "about"
trigger: "primary contact is set or changed on an account"
rule: >
  When an account's primary contact has an email address, copy that email address onto the
  account's own email field. If the primary contact changes, use the new contact's email.
outcome:
  success: "Account email address matches the primary contact's email address."
  rejection: null                    # no business rejection case — this rule always applies
```

```yaml
requirement: R2
title: Reject forbidden words in an account name
entity: account
trigger: "account is created or its name is changed"
rule: "Account name must not contain any word from the ForbiddenWords setting."
outcome:
  success: "Account is saved."
  rejection: "User sees: '<Field> is a forbidden word.'"
```

An analyst fills this in from a workshop or a ticket. It intentionally has no mention of stages,
modes, or C# types — that mapping is the AI's job in step 2.

Two fields are copied into the product **verbatim**, so the analyst owns their wording:

- `outcome.rejection` — the exact text the end user sees, **in the users' language** (for a Czech
  customer, Czech). The AI never translates, rephrases, or invents it (PF-ERR-004). If it is missing
  for a rule that rejects, the AI asks.
- `rule` — any concrete values (limits, lists, codes). If a value should be configurable rather than
  fixed, say so here; it then becomes a setting, not a constant in code.

## 2. Plugin breakdown

Group requirements by entity or functional area — one plugin per group (PF-PLUG-004). Two
requirements about the same entity usually share one plugin; a requirement that is really a
cross-entity capability (e.g. "recalculate a summary whenever any related activity changes") gets
its own capability-named plugin instead of being forced onto one entity's plugin.

```text
Plugin breakdown for R1, R2:
- AccountPlugin (entity: account)
    ← both R1 and R2 concern the account entity
```

State this list before writing any task — it is the first thing a reviewer checks.

**Most requirements land in an existing plugin.** Before proposing a new one, look in `Plugins/` for
`<Entity>Plugin`. In a mature solution it already exists and carries many tasks; the plan then says
*extend* it, and names the existing step each new task joins (PF-REG-010):

```text
Plugin breakdown for R3:
- ContactPlugin (existing, 27 tasks)   ← extend
    R3 -> step "contact Update PreOperation Synchronous" (existing)
          + filtering attribute: telephone1
          + pre-image column:    telephone1
          shares the step with: SetNormalizedPhoneNumber, CheckDuplicity, RestrictStatusChange, ...
```

The "shares the step with" line matters for two later decisions: the task's position in the
constructor (PF-REG-011 — a task that reads a value runs after the task that sets it), and the test
data, which must satisfy every task on the step (PF-TEST-013).

## 3. Task breakdown

One task per requirement, unless a requirement clearly bundles two unrelated rules (PF-TASK-002 —
split it). For each task, write the technical contract the validation chain and the tests will both
be generated from:

```yaml
task: SyncPrimaryContactEmail
plugin: AccountPlugin
entity: account
messages: [Create, Update]
stage: Postoperation
mode: Synchronous
trigger:
  filteringAttributes: [primarycontactid]
  requiredImages: []
registration:                        # PF-REG-010 — join an existing step or add one
  steps:
    - "account Create PostOperation Synchronous"   # new
    - "account Update PostOperation Synchronous"   # new
  sharesStepWith: []                 # other tasks on the same steps
preconditions:
  - "primarycontactid is present on Create, or changed on Update"
rules:
  - id: R1
    description: "copy the primary contact's email address onto the account"
    onFailure: none                  # no rejection case for this task
dataAccess:
  context: User
  reads: [contact.emailaddress1]
  writes: [account.emailaddress1]
logging:
  messageLines: ["Synced email from primary contact {contactId}."]
tests:
  - name: CreateAccount_WithPrimaryContactEmail_CopiesEmailToAccount
    expect: success
  - name: UpdateAccount_ChangingPrimaryContact_CopiesNewContactEmail
    expect: success
  - name: CreateAccount_WithoutPrimaryContact_LeavesEmailUnset
    expect: success
```

```yaml
task: ValidateAccountName
plugin: AccountPlugin
entity: account
messages: [Create, Update]
stage: Prevalidation
mode: Synchronous
trigger:
  filteringAttributes: [name]
  requiredImages: []
registration:
  steps:
    - "account Create PreValidation Synchronous"   # new
    - "account Update PreValidation Synchronous"   # new
  sharesStepWith: []
preconditions:
  - "name is present in the target entity"
rules:
  - id: R2
    description: "name must not contain a word from the ForbiddenWords setting"
    onFailure: userMessage           # → DataverseValidationException in DoExecute()
    message: "Name is a forbidden word."   # verbatim from the intake, users' language
dataAccess:
  context: User
  reads: [pl_setting.ForbiddenWords]
logging:
  messageLines: []
tests:
  - name: CreateAccount_WithAllowedName_Succeeds
    expect: success
  - name: CreateAccount_WithForbiddenName_ShouldBeRejected
    expect: rejected                 # test sees FaultException<OrganizationServiceFault> (PF-TEST-008)
    message: "is a forbidden word"
```

This is the review artifact: a developer or the consultant who wrote the intake can check it against
the original requirement without reading any code. **Adjust the plan here** — add a missing
precondition, split a task, change a message list — before anything is implemented. Changing the
plan after code exists is possible but costs more; changing it now costs a text edit.

## 4. Execute (TDD, per task)

Once the plan for a task is approved:

1. Write its tests from the `tests:` list — happy path(s) and the rejection path if `onFailure` is
   set. Run them: they fail (task class doesn't exist yet, or is a stub).
2. Implement the task: `AddValidations()` from `trigger` and `preconditions` — when the task runs;
   `DoExecute()` from `rules` and `dataAccess` — what it does, including the rejection when a rule
   is broken (PF-TASK-008).
3. Implement or extend the plugin's `RegisterTask<T>(...)` (runtime) and `Register(...)` (deployment
   metadata) from the `registration:` block — extend the existing step where the plan says so, place
   the `RegisterTask<T>(...)` call in execution order — see
   [`docs/ai/rules/60-registration.md`](./rules/60-registration.md).
4. Run the fast loop ([`docs/ai/verify.md`](./verify.md)): build, `manifest`, `validate`.
5. Hand off to a human for deployment (PF-PROC-005 — never run `deploy` yourself).
6. After deployment, run the new task's tests **and the tests of every task in `sharesStepWith`**
   against the dev environment (PF-ENV-*). A new task on a shared step can break its neighbours —
   a wider filtering attribute set, a stricter rejection, or a changed value makes their tests fail
   even though their code did not change.

## What the plan looks like when it is done

The review artifact the consultant signs off, one block per requirement:

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

## Where this lives per tool

- Claude Code / any `AGENTS.md`-reading agent: this file plus
  [`docs/ai/rules/`](./rules/) and [`docs/ai/verify.md`](./verify.md).
- GitHub Copilot: three prompts in [`.github/prompts/`](../../.github/prompts/), one per review
  checkpoint — `/analyze-and-plan` for steps 1–3 of this document, then, per approved task,
  `/generate-task-tests` (step 4.1) and `/generate-task-and-plugin` (steps 4.2–4.5). The
  instructions in [`.github/instructions/`](../../.github/instructions/) apply the rules
  automatically by file path. [`.github/copilot-instructions.md`](../../.github/copilot-instructions.md)
  is the overview. None of these may contradict this file or `docs/ai/rules/`.

## ➡️ Related

- [AGENTS.md](../../AGENTS.md)
- [Rule catalog](./rules/)
- [AI instructions analysis §8](./ai-instructions-analysis.md#8-pipeline-analýza--task-hlavní-konkurenční-výhoda) — the original design note this workflow implements
