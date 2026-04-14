# Test Architecture

> [!IMPORTANT]
> The testing stack is designed for real Dataverse integration testing.
> It separates test scenarios, test data creation, runtime infrastructure, and cleanup logic.

---

## 📑 Navigation

- [🔍 What test architecture solves](#-what-test-architecture-solves)
- [🧱 Core building blocks](#-core-building-blocks)
- [🗂️ Recommended project structure](#️-recommended-project-structure)
- [🔄 Runtime model](#-runtime-model)
- [🧹 Cleanup architecture](#-cleanup-architecture)
- [💻 Example responsibility split](#-example-responsibility-split)
- [✅ Design recommendations](#-design-recommendations)
- [➡️ Related documents](#️-related-documents)

---

## 🔍 What test architecture solves

The testing architecture exists to make Dataverse integration tests:

- repeatable
- maintainable
- cleanup-safe
- aligned with plugin logic

It separates:

- test scenario code
- test data creation
- Dataverse connection handling
- cleanup logic
- dependency registration

This keeps test methods focused on business scenarios instead of infrastructure boilerplate.

---

## 🧱 Core building blocks

The main testing building blocks are:

| Building block | Responsibility |
|---|---|
| `TestBase` | Shared base class for test classes |
| `TestFixture` | Shared runtime setup for tests |
| `TestAutofacModule` | Registers the testing stack and repositories |
| `ITestDataService` / `TestDataService` | Creates tracked test data and coordinates cleanup |
| `IAutoRegisteredTestDataRepository` | Marker interface for automatic repository registration |
| repositories | Reusable creation of test data |
| `IDataverseConnectionService` | Provides Dataverse connections |
| `ICleanupDeleteHandler` | Custom cleanup of dependent reference data before entity deletion |

This gives a clear split:

- test classes define scenarios
- repositories build input data
- `TestDataService` creates and tracks entities
- cleanup handlers remove blocking references when needed
- the connection service provides Dataverse access

---

## 🗂️ Recommended project structure

Recommended structure:

~~~
YourSolution.Tests/
├── Tests/
│   ├── Account/
│   ├── Contact/
│   └── TestBase.cs
├── Data/
│   ├── CleanUp/
│   └── Repositories/
├── TestAutofacModule.cs
└── appsettings.json
~~~

Recommended usage:

- `Tests/` mirrors the business or task structure from `Logic`
- `Data/Repositories/` contains reusable test data factories
- `TestAutofacModule.cs` registers the testing stack
- `TestBase.cs` provides the shared base for test classes

> [!IMPORTANT]
> Tests should reference `Logic`, not the merged `Plugins` assembly.

---

## 🔄 Runtime model

The runtime model is:

1. `TestFixture` prepares the shared runtime
2. `TestAutofacModule` registers required services and repositories
3. `TestBase` exposes testing services to test classes
4. repositories prepare test data
5. `TestDataService` creates tracked entities in Dataverse
6. test code performs assertions against real Dataverse results
7. cleanup is handled centrally

The connection layer is provided through `IDataverseConnectionService`.

This service is responsible for creating Dataverse connections and supports both:

- default connection usage
- caller-specific connection usage

This keeps connection handling outside test methods.

---

## 🧹 Cleanup architecture

Cleanup is handled centrally through `TestDataService`.

The important rule is:

- test-created entities should be created through `TestDataService`
- cleanup should not be scattered across test methods

`TestDataService` tracks created entities and deletes them later in reverse cleanup flow.

When cleanup must remove related reference data before deleting the main entity, the testing stack supports:

- `ICleanupDeleteHandler`

`ICleanupDeleteHandler` is used for entity-specific pre-delete cleanup.

It defines:

- `EntityLogicalName`
- `DeleteReferences(...)`

This allows the cleanup pipeline to:

- detect that a specific entity type needs pre-cleanup
- run custom delete logic for related data
- delete the main entity only after blocking references are removed

This is important for scenarios where direct deletion would fail because of:

- dependent records
- relationships
- Dataverse constraints

> [!IMPORTANT]
> Cleanup is not only about deleting the created entity.
> It must also allow controlled cleanup of dependent reference data when needed.

`TestDataService` supports registering cleanup handlers through:

- `AddCleanUpDeleteHandler(...)`
- `AddCleanUpDeleteHandlers(...)`

This keeps custom cleanup logic centralized and reusable.

---

## 💻 Example responsibility split

### Test class
Responsible for:

- defining the scenario
- calling repositories
- performing assertions

### Repository
Responsible for:

- creating reusable base test data
- exposing variants such as `GetNew()` or `GetNewWithAddress()`

### `TestBase`
Responsible for exposing:

- `TestDataService`
- `OrganizationService`
- `ConnectionService`
- shared runtime services

### `TestDataService`
Responsible for:

- creating tracked test entities
- storing entities for cleanup
- providing repositories
- coordinating cleanup
- invoking registered `ICleanupDeleteHandler` implementations before deletion

### `IDataverseConnectionService`
Responsible for:

- providing Dataverse service instances
- handling caller-based access when needed
- keeping connection setup outside test methods

This split keeps the architecture readable as the test project grows.

---

## ✅ Design recommendations

- keep test classes focused on scenarios
- keep reusable test data in repositories
- use `TestBase` for shared runtime access
- create tracked records through `TestDataService`
- keep cleanup centralized
- use `ICleanupDeleteHandler` when dependent reference data must be removed before deleting the main entity
- keep Dataverse connection logic outside test methods
- mirror the business structure from `Logic` where it improves navigation

> [!TIP]
> Good test architecture removes repeated setup and cleanup code from test methods.

---

## ➡️ Related documents

- [Testing Overview](./testing.md)
- [Test Execution Flow](./test-execution-flow.md)
- [Test Data Lifecycle](./test-data-lifecycle.md)
- [Test Data Access](./data-access.md)
- [Plugin Architecture](../plugins/architecture.md)