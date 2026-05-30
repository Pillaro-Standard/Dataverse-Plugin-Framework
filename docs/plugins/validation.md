# Validation Model

> [!IMPORTANT]
> Validation is a core part of the framework.
> It is designed to keep task execution predictable, readable, and efficient.
> The intended order goes from the cheapest checks to the more expensive ones.

---

## 📑 Navigation

- [🔍 What validation is](#-what-validation-is)
- [🎯 Why validation matters](#-why-validation-matters)
- [🧭 Validation chain order](#-validation-chain-order)
- [📋 Validator classes overview](#-validator-classes-overview)
- [⚙️ Performance model](#️-performance-model)
- [📊 Validation and logging outcomes](#-validation-and-logging-outcomes)
- [💻 Example](#-example)
- [✅ Design recommendations](#-design-recommendations)
- [➡️ Related documents](#️-related-documents)

---

## 🔍 What validation is

In this framework, validation defines the preconditions that must be met before a task runs its business logic.

Validation lives in:

    AddValidations()

Execution lives in:

    DoExecute()

This separation is intentional.

It keeps the task readable and makes it easier to understand:

- when the task should run
- when the task should be skipped
- when execution should stop with an error or validation failure shown to the user

---

## 🎯 Why validation matters

Validation is not only about correctness.
It is also about execution cost and runtime clarity.

A good validation chain should:

- filter out invalid cases as early as possible
- use cheap checks before expensive checks
- avoid unnecessary calls to Dataverse
- keep business rules readable
- make the execution intent explicit

> [!NOTE]
> The validation model is designed so that low-cost checks are performed first and more expensive checks can be deferred until earlier conditions already passed.

---

## 🧭 Validation chain order

The fluent API is intentionally structured in a fixed order.

The expected order is:

1. `WithMode(...)`
2. `WithStage(...)`
3. `WithMessage(...)` or `WithMessages(...)`
4. `ForEntity(...)` or `ForEntities(...)`
5. image checks
6. attribute checks
7. cheap custom validations
8. more expensive validations or stop/throw validations

In practice, the available chain moves through these interfaces:

- `IBasicModeValidation` → `WithMode(...)`
- `IBasicStageValidation` → `WithStage(...)`
- `IBasicMessageValidation` → `WithMessage(...)` / `WithMessages(...)`
- `IBasicPrimaryEntityValidation` → `ForEntity(...)` / `ForEntities(...)`
- `IBasicImageValidation` → image checks
- `IBasicAttributeValidation` → attribute checks
- `ICustomValidation` → `WithValidation(...)` for low-cost custom validations
- `IBreakValidation` → `WithBreakValidation(...)`, `ThrowWithError(...)`, `ThrowWithWarning(...)` for validations that may stop later steps or raise user-facing exceptions

> [!IMPORTANT]
> The order is not random.
> It reflects the expected cost model:
> start with registration and context checks, continue with runtime context checks, then perform custom logic, and only then move to heavier validations.

---

## 📋 Validator classes overview

The fluent validation API builds its validation chain on top of dedicated validator classes.

| Validator | Purpose | Typical use | Relative cost | Notes |
|---|---|---|---|---|
| `ModeValidator` | Checks plugin mode | Sync vs async separation | Very low | First execution filter |
| `StageValidator` | Checks plugin stage | PreValidation / PreOperation / PostOperation | Very low | Cheap execution filter |
| `MessageValidator` | Checks message name | Create / Update / Delete / custom message filtering | Very low | Supports single or multiple messages |
| `EntityValidator` | Checks primary entity name | Entity-specific task or multi-entity capability task | Very low | Useful for both single-entity and multi-entity registration |
| `ImageValidator` | Checks required pre/post image presence | Update comparison logic, post-operation logic | Low | Used when image presence is always required |
| `ImageWithConditionValidator` | Checks required image only when a predicate applies | Update-only or branch-specific image requirements | Low | Conditional image validation |
| `EntityAttributesValidator` | Checks whether required attributes are present | Update-trigger optimization, required input fields | Low | Supports both “at least one” and “all attributes” checks |
| `EntityAttributesWithConditionValidator` | Checks required attributes only when a predicate applies | Branch-specific attribute requirements | Low | Conditional attribute validation |
| `CustomValidator` | Runs custom predicate validation without breaking later custom validations | Cheap in-memory business rules | Medium to low | Intended for validations without meaningful performance impact |
| `CustomBreakValidator` | Runs custom predicate validation and stops later break validations after first failure | Expensive rules, Dataverse-dependent checks, guarded business conditions | Medium to high | Intended for validations with performance impact |
| `ThrowExceptionValidator` | Stops execution by throwing a user-facing exception when validation fails | Hard stop or user-facing validation stop | Medium to high | Used behind `ThrowWithError(...)` and `ThrowWithWarning(...)` |

> [!IMPORTANT]
> The fluent methods are the public authoring model.
> Internally, the framework composes that chain from these validator classes.

> [!NOTE]
> The goal is still the same:
> run the cheapest validators first, then move toward more expensive and more execution-sensitive validations.

---

## ⚙️ Performance model

The intended performance model is simple:

### 1. Start with execution filters

Use these first:

- mode
- stage
- message
- primary entity

These checks are cheap and eliminate irrelevant executions early.

### 2. Continue with runtime context checks

Then use:

- image presence
- attribute presence

These are still relatively cheap and help avoid unnecessary business execution.

### 3. Add cheap custom logic

Use `WithValidation(...)` for validations that:

- do not require Dataverse queries
- use already available runtime context
- do not have meaningful performance cost

The framework explicitly describes this area as intended for validations without performance impact.

### 4. Put more expensive checks later

Use `WithBreakValidation(...)`, `ThrowWithError(...)`, and `ThrowWithWarning(...)` for checks that:

- are more expensive
- may require Dataverse reads
- should not run if earlier validations already failed
- should stop later validation flow when invalid

The framework explicitly describes this area as the place for validations with performance impact or validations that should not continue after earlier failure.

> [!IMPORTANT]
> If a validation needs Dataverse queries, design the chain so that all cheaper filters run first.
> This keeps the task efficient and avoids paying query cost for executions that should have been filtered out earlier.

> [!TIP]
> Expensive validation logic can be split into several steps.
> That is often better than one large validation block, because it makes the decision path clearer and lets you stop sooner.

### Practical guideline

A good order usually looks like this:

- execution metadata filters
- entity and message filters
- image checks
- attribute checks
- cheap in-memory rules
- expensive Dataverse-dependent rules
- user-facing throw behavior if required

---

## 📊 Validation and logging outcomes

Validation does not only control whether a task runs.
It also affects how task behavior appears in framework logging and reporting.

Each validation also leaves a clear message in the framework logs when the task does not continue because validation failed.

This makes it easier to:

- understand why a task did not run
- diagnose incorrect registrations or overly broad triggers
- speed up troubleshooting and fixes
- support user-focused testing and verification of system behavior
- explain why a specific interaction with the system produced a given result

In practice, task execution can typically appear in outcomes such as:

- `Success`
- `NotValid`
- `Error`

This is important for more than diagnostics.

These outcomes help you:

- see which tasks are executed successfully
- identify tasks that are triggered too often but end as `NotValid`
- detect registrations that are too broad or poorly targeted
- find opportunities for performance optimization
- evaluate long-term task behavior through prepared reporting and model-driven application statistics

> [!IMPORTANT]
> A task that is frequently logged as `NotValid` is not necessarily incorrect.
> But it is often a sign that the registration scope, validation order, or trigger design should be reviewed.

---

## 💻 Example

Example of a validation chain:

    protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
    {
        return validator
            .WithMode(PluginMode.Synchronous)
            .WithStage(PluginStage.Preoperation)
            .WithMessages(["Create", "Update"])
            .ForEntity(Contact.EntityLogicalName)
            .HasPreImageWhen(x => x.PluginContext.MessageName == "Update")
            .EntityWithAtLeastOneAttribute(ContextEntity, "firstname", "lastname")
            .WithValidation("First name or last name must be present.", x =>
                ContextEntity.Attributes.Contains("firstname") || ContextEntity.Attributes.Contains("lastname"))
            .WithBreakValidation("The contact already violates a cross-record rule.", x =>
            {
                // Expensive check, potentially using Dataverse query logic
                return true;
            });
    }

This example reflects the intended order:

- cheap execution filters first
- runtime context checks next
- cheap custom rule after that
- expensive validation later

---

## ✅ Design recommendations

Use these rules by default:

- start with the cheapest validations
- validate execution context before business conditions
- use image and attribute checks before custom logic
- use `WithValidation(...)` for low-cost rules
- use `WithBreakValidation(...)` for expensive validations
- use `ThrowWithError(...)` only when execution must stop with a user-facing error
- use `ThrowWithWarning(...)` when the goal is to stop with an expected user-facing business validation message instead of treating the situation as a technical failure
- avoid mixing all validation logic into one large predicate

When validation requires Dataverse access:

- keep those checks late in the chain
- split them into smaller validations if that improves clarity
- make sure cheaper validations already filtered obvious invalid cases

When deciding between `WithValidation(...)` and `WithBreakValidation(...)`:

- use `WithValidation(...)` for cheap checks you are fine to run every time
- use `WithBreakValidation(...)` for more expensive checks or checks that should stop further validation flow after failure

> [!WARNING]
> Poor validation order increases cost quickly.
> If expensive Dataverse-dependent logic runs before simple context filtering, the framework loses one of its key practical benefits.

---

## ➡️ Related documents

Continue with:

- [Getting Started](./getting-started.md)
- [Plugin Model](./plugin-model.md)
- [Task Model](./task-model.md)
- [Data Access](./data-access.md)
- [Execution Pipeline](../execution-pipeline.md)
- [Architecture](./architecture.md)
