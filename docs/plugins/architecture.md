# Architecture

> [!IMPORTANT]
> The framework uses a deliberate multi-project architecture for development and a single-assembly model for Dataverse deployment.
> This separation is intentional and solves a real platform constraint.

---

## 📑 Navigation

- [🔍 What this architecture solves](#-what-this-architecture-solves)
- [🧱 Core architecture model](#-core-architecture-model)
- [📦 Recommended solution structure](#-recommended-solution-structure)
- [🗺️ Architecture overview](#️-architecture-overview)
- [🚀 `Logic` project](#-logic-project)
- [🧩 `Plugins` project](#-plugins-project)
- [🧪 `Tests` project](#-tests-project)
- [🔄 Development model vs deployment model](#-development-model-vs-deployment-model)
- [🧠 Core runtime building blocks](#-core-runtime-building-blocks)
- [🏗️ Runtime layering](#️-runtime-layering)
- [🪜 Typical execution layering](#-typical-execution-layering)
- [📌 Key architectural rules](#-key-architectural-rules)
- [✅ Design recommendations](#-design-recommendations)
- [➡️ Related documents](#️-related-documents)

---

## 🔍 What this architecture solves

The framework solves two different needs at the same time:

- a maintainable structure for plugin development
- a Dataverse-compatible deployment model

These two needs are not the same.

For development, you want:

- clear separation of responsibilities
- business logic that is readable and testable
- reusable services and features
- isolated task execution units

For deployment, Dataverse expects:

- a deployable plugin assembly
- a structure that works inside the plugin runtime
- predictable execution behavior

The framework architecture separates development concerns from deployment concerns, while keeping both consistent.

A key practical reason for splitting `Logic` and `Plugins` is testability.

The final `Plugins` assembly is a merged deployment output.
If that merged assembly is referenced from a test project, it introduces duplicate references and type conflicts that make further work with references difficult and unreliable.

Because of that:

- business logic is kept in `Logic`
- the deployable assembly is produced in `Plugins`
- tests reference `Logic`, not the merged plugin output

> [!IMPORTANT]
> The `Logic` project is not separated only for cleaner structure.
> It is also separated so that test projects can reference the real implementation directly without the reference conflicts caused by the merged deployment assembly.

---

## 🧱 Core architecture model

The framework uses three architectural layers in practice:

- **Plugin orchestration**
- **Task execution**
- **Supporting services and framework infrastructure**

The most important mental model is:

- **Plugin** = orchestration layer
- **Task** = business unit
- **Validation** = execution precondition layer
- **Services / Features / Data access** = supporting infrastructure

This keeps the codebase readable as it grows.

A plugin should tell you:

- what execution boundary exists
- which tasks belong to it

A task should tell you:

- what business behavior is being executed

Supporting services should tell you:

- what reusable infrastructure or business helper behavior exists

---

## 📦 Recommended solution structure

    YourSolution/
    ├── YourSolution.Logic/
    ├── YourSolution.Plugins/
    └── YourSolution.Tests/

---

## 🗺️ Architecture overview

~~~mermaid
flowchart TD
    A[Dataverse Deployment Model] --> B[YourSolution.Plugins]
    B --> C[Final Deployable Plugin Assembly]

    D[YourSolution.Logic] --> B
    D --> E[Business Logic]
    D --> F[Plugins]
    D --> G[Tasks]

    I[YourSolution.Tests] --> D
    I --> J[Programmatic Tests]

    B --> K[Signing]
    B --> L[Post-build Merge]
~~~

This diagram shows the core architectural split:

- `Logic` is the implementation project
- `Plugins` is the deployment project
- `Tests` reference `Logic`
- `Plugins` produces the final deployable assembly

---

## 🚀 `Logic` project

The `Logic` project is the main development project.

This is where you keep:

- plugin classes
- task implementations
- validation logic
- shared features and services
- business rules
- framework-based runtime logic

Typical structure:

    YourSolution.Logic/
    ├── Plugins/
    ├── Tasks/
    └── Features/

This project should contain the real implementation logic.

It exists so that:

- business logic stays separate from deployment packaging
- task code remains referenceable
- tests can reference clean source logic instead of merged plugin output

> [!NOTE]
> In practice, this is the most important project for day-to-day development.

---

## 🧩 `Plugins` project

The `Plugins` project is the deployment project.

It is a shell project.

Its job is to:

- reference the `Logic` project
- produce the final deployable assembly
- handle signing and merge preparation
- fit the Dataverse deployment model

It must not contain business implementation.

That means:

- no CRM business logic
- no task implementation
- no reusable C# business logic
- no feature implementation
- no domain behavior

It exists only to package, sign, merge, and deploy the logic implemented elsewhere.

Keep it focused on:

- deployment packaging
- assembly preparation
- final plugin output

> [!IMPORTANT]
> `Plugins` is only a shell for packaging and deployment.
> It must not become an implementation project.

---

## 🧪 `Tests` project

The `Tests` project exists so that the solution can be verified programmatically without relying on merged deployment output.

This matters because:

- business logic should be testable in a clean form
- merged plugin assemblies are not a good target for test references
- test architecture has different concerns than plugin deployment

The `Tests` project is optional in the strict technical sense, but strongly recommended in the practical architectural sense.

Use it when you want:

- repeatable verification
- regression safety
- cleaner long-term maintenance
- task-level testing without relying only on manual testing

> [!TIP]
> For most growing solutions, the value of the `Tests` project becomes obvious very quickly.

---

## 🔄 Development model vs deployment model

This is one of the most important architectural distinctions.

### Development model

During development, the solution is intentionally split:

- `Logic`
- `Plugins`
- `Tests`

This improves:

- clarity
- maintainability
- testability
- separation of concerns

### Deployment model

For Dataverse deployment, the final output must be a deployable plugin assembly produced by the `Plugins` project.

That means the solution is split for development, but unified for deployment.

This is why the prepared post-build resources exist.

> [!IMPORTANT]
> The framework architecture is designed around this exact tension:
> multiple clean development units, one deployable plugin output.

This is also why the `Logic` project matters so much:

- it keeps business logic referenceable
- it keeps test references clean
- it prevents the merged deployable assembly from becoming the source of truth for development

---

## 🧠 Core runtime building blocks

The main runtime building blocks are:

| Building block | Responsibility |
|---|---|
| `PluginBase` | Orchestrates registered tasks inside Dataverse plugin execution |
| `TaskBase<TEntity>` | Provides the task execution model and prepared runtime surface |
| `TaskContext` | Shared execution context across the pipeline |
| Validation chain | Controls task applicability and execution preconditions |
| Prepared providers and services | Give access to data, settings, tracing, and logging helpers |

This structure gives the framework a predictable execution model while keeping business logic modular.

---

## 🏗️ Runtime layering

~~~mermaid
flowchart TD
    A[Dataverse Event] --> B[Plugin]
    B --> C[Task]
    C --> D[Validation]
    C --> E[Execution]

    E --> F[TaskContext]
    E --> G[Data Access]
    E --> H[Runtime Settings]
    E --> I[Task Logging]

    G --> J[OrganizationServiceProvider]
    G --> K[DataServiceProvider]
~~~

This diagram shows the runtime layering inside one plugin execution:

- Dataverse triggers the plugin
- the plugin orchestrates tasks
- each task validates and executes
- task execution uses prepared runtime services
- data access is available through prepared providers

---

## 🪜 Typical execution layering

A typical framework-based solution follows this layering:

### 1. Dataverse triggers the plugin
The plugin is the Dataverse entry point.

### 2. The plugin resolves matching tasks
The plugin selects tasks registered for the current execution context.

### 3. Each task validates itself
Validation determines whether the task should run.

### 4. Valid tasks execute business logic
Execution happens inside the task.

### 5. Supporting services are used where needed
This includes areas such as:

- data access
- settings
- task logging helpers
- tracing
- reusable feature logic

This layering is one of the main reasons the framework stays readable as the solution grows.

---

## 📌 Key architectural rules

Use these rules as the default architecture:

- keep plugins thin
- keep business logic in tasks
- keep reusable logic in features or supporting services
- keep deployment responsibilities in the `Plugins` project
- keep tests separate from merged deployment output
- keep execution deterministic and explicit
- keep context usage visible and intentional

If these rules start breaking down, the architecture usually becomes harder to maintain.

---

## ✅ Design recommendations

Use these recommendations by default:

- treat `Logic` as the implementation source of truth
- treat `Plugins` as the deployment output project
- treat `Tests` as the clean verification layer
- do not mix deployment packaging with business implementation
- do not push business logic back into plugin classes
- keep reusable infrastructure outside task bodies when it clearly belongs in shared services
- keep the project structure stable as the solution grows

When reviewing architecture, ask:

- is business logic still in the right place?
- is the deployment project still only a shell project?
- are tasks still focused?
- can tests reference the real implementation cleanly?
- does the structure still reflect responsibilities clearly?

> [!TIP]
> Good architecture in this framework is not about adding more layers.
> It is about keeping the existing layers clean and purposeful.

---

## ➡️ Related documents

Continue with:

- [Getting Started](./getting-started.md)
- [Plugin Model](./plugin-model.md)
- [Task Model](./task-model.md)
- [Validation Model](./validation.md)
- [Execution Pipeline](./execution-pipeline.md)
- [Data Access](./data-access.md)
- [DataService](./data-service.md)