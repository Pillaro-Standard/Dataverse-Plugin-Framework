# generate-task-and-plugin.prompt.md

## Purpose

Generate implementation for a Dataverse task and its corresponding plugin based on the provided assignment.

This prompt defines the required structure, rules, and framework conventions.
All entity-specific behavior, validation rules, messages, and execution details must be taken from the assignment. The framework context and runtime constraints should stay aligned with the repository guidance.  

---

## Instructions for Copilot

Work within the Dataverse-Plugin-Framework and follow its architectural and repository conventions.

Before generating any code, explicitly load and apply:

* `.github/project-setup.md`
* `.github/copilot-instructions.md`
* `.github/instructions/plugins.instructions.md`
* `.github/instructions/task.instructions.md`
* `.github/instructions/coding-best-practices.instructions.md`

Use representative implementations as primary code-shape references:

* Task reference: value from `.github/project-setup.md` (`TaskReferenceFile`)
* Plugin reference: value from `.github/project-setup.md` (`PluginReferenceFile`)

Do not proceed if these instruction files are not loaded.
Do not invent repository-specific paths, namespaces, or project names; resolve them from `.github/project-setup.md`.

---

## Mandatory Pre-Check Before Code Generation

Before generating plugin, task, or feature code:

* inspect the entity configuration file defined in `.github/project-setup.md` (`EntityConfigFile` /
  `EntityConfigPath`) first
* if the required entity is NOT listed in the configured entity list key from `.github/project-setup.md` (`EntitiesConfigKey`), treat the entity as non-existent
* if the required entity IS listed in the configured entity list, inspect the per-entity generated
  file defined in `.github/project-setup.md` (`EarlyBoundEntityFile`, e.g. `EarlyBound/Entities/contact.cs`)
  ONLY in the single implementation project where the task will be generated — early-bound classes
  are one file per entity, there is no single combined file
* determine the target project from the task output location and namespace
* do NOT use another project's `EarlyBound/Entities/<entity>.cs` for this validation, including test
  projects, sample projects, or unrelated logic projects
* verify that `EarlyBound/Entities/<entity>.cs` exists and defines class `<Entity>` for every required entity listed in the configured entity list

Validation scope is strict:

* source of truth for configured entities = value from `.github/project-setup.md`
* source of truth for early-bound validation = the single target implementation project only
* if entity is missing from configured entity list, treat it as non-existent
* if entity exists in the configured entity list but matching class is missing in implementation project early bounds, treat it as not yet generated
* if entity exists in any non-target project, still treat it as missing
* if multiple early bound files exist, use only the one belonging to the target implementation project namespace/output

If any required entity is missing from configured entity list or missing from implementation project early bound classes:

* do NOT continue with final code generation
* do NOT generate plugin code
* do NOT generate task code
* do NOT generate partial scaffolding, placeholders, or proposed code snippets
* inform the user whether the problem is:
  * entity missing in configured entity list, or
  * entity present in configured entity list but missing in configured early-bound file
* if missing in configured entity list, recommend adding the entity to the setup-configured entity file
* if present in the configured entity list but missing in early bound classes, recommend regenerating early bound classes
* continue with code generation only after the entity is available

Suggested user-facing message:

`The required entity check failed. If the entity is missing in the configured entity list, add it to the configured entity file. If it is already configured but missing in the configured early-bound file, regenerate early bound classes. Then continue with plugin/task generation.`

If the environment supports command/script execution, you may automate regeneration by running:

```text
Tools\EarlyBound\GenerateEarlyBound.bat
```

If script execution is not available, do not pretend it was executed.
Instead, explicitly instruct the user to run the batch file manually.

---

## General Rules

* Every assignment results in:

  * plugin implementation or plugin modification
  * task implementation
  * additional feature-specific classes when needed

* Do NOT hardcode:

  * message names
  * business rules
  * validation rules
  * execution conditions
  * attribute names unless explicitly defined in the assignment

* Always derive all functional behavior from the assignment.

* Keep a single responsibility per task and per feature scope.

* Generated code must compile with zero warnings/messages (compiler + analyzers).

* Analyzer compliance is mandatory. Avoid at least:
  * SYSLIB1045 (do not solve this by introducing `GeneratedRegex` in plugin/task code)
  * IDE0057 (simplify `Substring` usage)
  * CA1862 (use `string.Equals(..., StringComparison...)`)
  * CA1861 (avoid repeated constant array allocations)
  * CA1822 (mark member `static` when possible)
  * IDE0028 (prefer collection expressions/initializers)
  * CA1307/CA1309 (always specify correct `StringComparison`)
  * IDE0005 (remove unnecessary `using` directives)
  * IDE0059/IDE0060 (avoid unnecessary assignments/unused parameters)

---

## Scope

* A task may be implemented only for the **Task** entity if that is what the assignment requires.
* Do not generalize entity scope unless the assignment explicitly requires broader reuse.
* If the assignment requires more than a simple plugin/task pair, add supporting services, helpers, or other feature-specific classes.

---

## Structure

Use feature-based structure under:

```text
Features/<TaskName>/
```

Within that structure, create the corresponding C# classes needed by the assignment, for example:

* plugin class
* task class
* services
* validators
* feature-specific supporting classes

Prefer naming and organization that clearly reflect the task/feature responsibility.

---

## Plugin Rules

* Use **PluginBase**
* Plugin is responsible only for orchestration
* Business behavior belongs in task / service classes
* Validation must be explicit and separated from execution logic
* Always create or modify the relevant plugin file as part of the assignment
* Always follow the plugin instruction patterns defined in the repository instructions file
* Always register runtime task dispatch via **RegisterTask<TTask>(...)** in the plugin constructor
* Always register deployment metadata (step id, image id, filtering attributes, `WithName(...)`) by
  overriding **`Register(IPluginRegistration registration)`** and using the fluent registration API
  (`.OnCreate<T>(...)`, `.OnUpdate<T>(...)`, `.PreOperation()`, `.WhenChanged(...)`, etc. — see
  `docs/plugins/plugin-registration-api.md`). There is no `PluginRegistrationAttribute` in this
  framework — do not invent one.
* `RegisterTask<TTask>(...)` (constructor) and `Register(...)` (deployment metadata) describe the
  *same* step from two places and MUST stay aligned (stage, message, entity, mode) — PF-REG-001
* Never introduce custom registration wrappers (e.g. RegisterXxx methods)
* Do not define step filtering attributes outside the `Register(...)` fluent chain / `RegisterTask` arguments
* Never invent a step ID or image ID GUID, and never copy one from `/docs` or `/examples` — ask a
  human for a freshly generated one (PF-REG-002/003)
* Prefer collection expressions for message lists over array initialization
* Include namespace for plugin mode/stage enums: **Pillaro.Dataverse.PluginFramework.Plugins**

The plugin must be designed according to the assignment, including:

* target entity
* message
* stage
* filtering attributes
* execution conditions

Possible messages may include, for example:

* Create
* Update
* Delete
* Assign
* SetState
* SetStateDynamicEntity
* Associate
* Disassociate
* custom messages

These must always come from the assignment, not from assumptions.

---

## Validation Rules

* Always use the **AddValidations** method
* Always use the built-in validator
* Keep validations explicit and separated from execution logic
* Validation rules must be based on the assignment
* Invalid/no-op states must be rejected in validation, not in DoExecute
* Validator must log invalid state and reason
* Prefer built-in validators such as **EntityWithAtLeastOneAttribute(...)** when applicable

Typical validation can include:

* expected entity
* expected message
* required input fields
* required pre/post image data
* execution prerequisites

---

## Execution Rules

* Execution logic must be driven strictly by the assignment
* Keep the plugin thin
* Put task logic into task-specific classes
* Keep small helper methods inside the task when complexity is low
* Add services only when logic is genuinely complex or reused
* Ensure the implementation remains clean and maintainable within the feature folder
* DoExecute must not contain validation guards or early returns for invalid states
* If task executes, it should perform the intended action

Task runtime constraints:

* Do NOT use constructor, method, or property injection
* Do NOT inject IOrganizationService
* Use **OrganizationServiceProvider** for organization services (InitiatingUserService/UserService/AdminService)
* Use **DataServiceProvider** for data services (UserDataService/AdminDataService)
* Select service variant according to execution context
* For organization operations, use **OrganizationServiceProvider.<Context>.Execute(...)**
* Do not use **AdminDataService.Retrieve(...)** as a substitute for organization service execution
* Assume Dataverse Sandbox compatibility is mandatory
* Do NOT use `GeneratedRegexAttribute`
* Do NOT use source generators or runtime features not suitable for .NET Framework 4.6.2 sandbox plug-ins
* Prefer simple string APIs over regex in task logic
* Do NOT use Web API from plug-ins/tasks; use Organization Service only
* Keep plugin/task execution stateless and lightweight

If external access is explicitly required by the assignment:

* allow only HTTP/HTTPS
* no localhost/loopback
* no raw IP addresses
* set timeout explicitly
* disable KeepAlive
* keep calls synchronous and minimal

Entity logical name comparisons:

* Use early bound constants (e.g. `regardingId.LogicalName.Equals(Logic.Contact.EntityLogicalName, StringComparison.InvariantCultureIgnoreCase)`)
* Do not compare against hardcoded logical name literals (e.g. `"contact"`)

---

## Design Principles

* Respect single responsibility within one task and within the given feature
* Do not mix orchestration, validation, and business logic in one class
* Prefer clear feature ownership over unnecessary abstraction
* Build only what is required by the assignment and framework conventions

---

## Testing Note

* Do not optimize the design around unit-test-specific patterns
* Tests are integration tests executed directly against Dataverse
* Focus primarily on correct feature structure, plugin/task separation, and maintainable execution logic

---

## Runtime Constraints

Respect repository/runtime constraints:

* .NET Framework 4.6.2
* single assembly deployment
* ILMerge-based output
* strong-name signing 

---

## Expected Output

Produce:

* proposed feature structure
* plugin class design
* task class design
* additional feature-specific classes if needed
* validation design
* brief explanation of design decisions
