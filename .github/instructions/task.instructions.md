---
applyTo: "**/Tasks/**/*.cs"
---

# Task Generation

> [!IMPORTANT]
> Canonical rules and rationale live in `AGENTS.md` and `docs/ai/rules/` in this repository. This
> file is the GitHub Copilot-native mechanism (`applyTo` path scoping) for applying those same
> rules — if the two ever disagree, `docs/ai/rules/` wins and this file should be corrected to match.

## Location & Namespace

- **Path**: `{TasksPath}/<Entity>` from project-setup.md
- **Namespace**: `{TaskNamespace}` → `{LogicNamespace}.Tasks.<Entity>`
- **Features**: `{FeaturesPath}/<TaskName>` when additional classes needed

## Structure

~~~
public class <FunctionalityName> : TaskBase<Logic.Entity>
{
    public <FunctionalityName>(IServiceProvider sp, TaskContext ctx) : base(sp, ctx) { }
    protected override ICompleteValidation AddValidations(IBasicModeValidation v)
    {
        return v
            .HasPreImageWhen(/* condition */)
            .EntityWithAtLeastOneAttribute(/* attrs */);
    }

    protected override void DoExecute()
    {
        // Business logic only - no validation here
    }
}

~~~

Tasks MUST:
- inherit from value defined in `.github/project-setup.md` (`TaskBaseType`)
- use primary constructor only for framework-required parameters (never for injected dependencies)
- implement single responsibility
- be composable
- be deterministic

Dependency injection is not supported in this project.

Do NOT use:
- constructor injection
- method injection
- property injection

Required pattern:
- create/resolve dependencies locally where used
- pass required dependencies explicitly from the calling task method

Tasks execute inside Dataverse Sandbox plug-ins and MUST respect sandbox/runtime constraints.

Do NOT use:
- GeneratedRegexAttribute
- source generators
- modern runtime features unavailable or unreliable in .NET Framework 4.6.2 sandbox plug-ins
- Web API calls from task code

Prefer simple string APIs over regex-based solutions. If regex would otherwise be considered, first rewrite the logic without regex.

---

## Task Naming

- Naming: <FunctionalityName>
- Do NOT include:
  - entity name
  - suffix "Task"
- Include entity name ONLY if omission would make the name ambiguous
- Assume entity context is already defined by folder (Tasks/<EntityFolder>)

Names MUST be:
- concise
- action-oriented
- unambiguous within the entity scope

The same task name MUST be used as the feature folder name when additional logic is extracted.

---

## Execution Rules

DoExecute() MUST:
- contain ONLY business logic
- be free of validation logic
- be deterministic
- perform an actual state-changing or business action when executed

DoExecute() MUST NOT:
- throw validation exceptions
- contain guard clauses
- validate required attributes
- validate image availability
- short-circuit on invalid states (move these checks to AddValidations)
- return early because of unsupported entity type or missing required values
- implement idempotency checks as validation-like guards

---

## Validation Rules

All validation MUST be implemented in:

AddValidations(...)

Use FluentValidation features whenever possible.

Validation MUST include explicit logging of invalid state and reason.

Prefer built-in validators whenever applicable, for example:
- EntityWithAtLeastOneAttribute(...)

Typical states that MUST be validated (not handled in DoExecute):
- missing required attribute values (e.g. null regarding)
- unsupported logical names/types
- no-op/idempotent states where execution would do nothing

---

## Fluent Validation Order

Validation MUST follow this order:

1) Context filters  
2) Entity scope  
3) Images  
4) Attributes  
5) Custom validation  
6) Flow control  

Put cheaper validations first.

---

## PreImage and PostImage Usage (CRITICAL)

Images MUST be used only when valid.

### Rules

- GetPreImage() ONLY when PreImage is available
- GetPostImage() ONLY when PostImage is available

If image is required, MUST validate using:
- HasPreImage()
- HasPreImageWhen(...)
- HasPostImage()
- HasPostImageWhen(...)

Do NOT access images without validation.

---

## Partial Update Merge Pattern (CRITICAL)

Do NOT assume all attributes are present in ContextEntity.

On Update:
- ContextEntity contains only changed attributes

Value resolution MUST follow:

1) ContextEntity  
2) PreImage  
3) null  

If PreImage is not available:
→ fallback = ContextEntity

This pattern MUST be used whenever full state is required.

---

## Feature Extraction Rules

If a task requires additional supporting logic outside the task class, that logic MUST be placed under a dedicated `Features` folder.

### Feature Location

value from `.github/project-setup.md` (`FeaturesPath`)

### Feature Structure

- The feature folder name MUST match the task name exactly
- All supporting classes used by that task MUST be placed in that folder

This includes:
- services
- resolvers
- mappers
- calculators
- providers
- policies
- domain helpers

### Rules

- Do NOT place supporting logic in the `Tasks` folder
- Do NOT create top-level folders like:
  - Services
  - Helpers
  - Utils
  - Resolvers
- Do NOT keep complex logic inside the task if it can be extracted
- All non-trivial logic MUST be extracted into Features

Pragmatic extraction rule:
- Keep small helper methods in the task class when logic is simple and local to the task.
- Extract to `Features/<TaskName>/` only when logic is clearly complex, reused, or hard to maintain inline.

### Responsibility Split

- Task = orchestration + business entry point
- Feature classes = reusable or complex logic

---

## Enrichment Rules

Enrichment tasks:
- derive or add values
- execute only when relevant
- MUST NOT overwrite existing values unless explicitly intended
- MUST be deterministic

Allowed:
- DataServiceProvider.UserDataService
- DataServiceProvider.AdminDataService

Forbidden:
- random values
- fake/demo logic
- writing unrelated attributes

---

## Data Access

Use ONLY:
- OrganizationServiceProvider
- DataServiceProvider

Do NOT:
- use DataService directly from FluentTaskBase
- use QueryService directly from FluentTaskBase
- inject IOrganizationService
- inject service providers

Organization service access in tasks MUST go through OrganizationServiceProvider:
- InitiatingUserService
- UserService
- AdminService

For direct Dataverse requests/commands in task logic, use:
- OrganizationServiceProvider.<Context>.Execute(...)

Do NOT use:
- AdminDataService.Retrieve(...)
- UserDataService.Retrieve(...)
when the operation is intended as organization service execution.

Data access in tasks MUST go through DataServiceProvider:
- UserDataService (current user context)
- AdminDataService (administrative context)

Select the service variant explicitly according to the required execution context.

---

## Attribute Rules (PF-DATA-008/009 — corrected, was previously wrong in this file)

`nameof(ContextEntity.Property)` as an attribute name is **forbidden**. It returns the C# property
name, not the Dataverse logical name — they differ for `Id` (`nameof(ContextEntity.Id)` → `"id"`,
never a real attribute) and for every relationship navigation property. The mismatch compiles fine
and fails silently at runtime: the task validates against an attribute that is never present, so it
always ends `NotValid` and looks exactly like a legitimately filtered-out execution in the log.

Two-tier rule, based on whether early-bound types exist for the entity yet:

- **Before early-bound types are generated for the entity** (the default state of a brand-new
  project — `ExampleTask`/`ExamplePlugin` in the template have none): use the Dataverse logical name
  as a plain string literal, e.g. `"firstname"`. Do not call `.ToLower()`/`.ToLowerInvariant()` on it
  — it is already the logical name, not a derived value.
- **Once early-bound types exist** (after `pac modelbuilder` has been run, `emitFieldsClasses: true`):
  use the generated constant, e.g. `Contact.Fields.FirstName`.
- `nameof(...)` is still fine for anything that is *not* an attribute-name argument — a log message
  label or a test category (`[Trait("Category", nameof(SomeTask))]`).

Entity logical name checks MUST use early bound constants, for example:
- `Logic.Contact.EntityLogicalName`

Do NOT compare against hardcoded literals like `"contact"` for the **entity** name — that rule is
unchanged. It is specifically the *attribute*-name rule above that was wrong and is now corrected.

---

## Anti-patterns

Do NOT:
- combine multiple responsibilities
- place validation in DoExecute()
- use unrelated attributes
- generate artificial data
- distribute logic across unrelated locations

---

## Code Style

- Use var whenever possible
- Do NOT initialize attributes in constructors
- Single-line if without braces
- Loops always use braces
- Prefer default assignment + overwrite over if/else
- Minimize branching
- Minimize line count