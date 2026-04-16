# Test Execution Flow

> [!IMPORTANT]
> Tests run against a real Dataverse environment.
> The execution flow covers setup, data creation, plugin execution, assertion, and cleanup.

---

## 📑 Navigation

- [🔍 What this document covers](#-what-this-document-covers)
- [🔄 Execution flow](#-execution-flow)
- [🧹 Cleanup flow](#-cleanup-flow)
- [✅ Recommendations](#-recommendations)
- [➡️ Related documents](#️-related-documents)

---

## 🔍 What this document covers

This document explains the normal runtime flow of an integration test.

It focuses on:

- how test execution starts
- how test data is created
- how plugin execution is triggered
- how results are verified
- how cleanup is performed

---

## 🔄 Execution flow

The usual flow is:

1. `TestFixture` prepares the shared runtime
2. `TestBase` exposes testing services
3. the test prepares data through repositories
4. the test creates or updates data in Dataverse
5. Dataverse plugins execute
6. the test reads data back and verifies the result
7. cleanup is performed

In practice, the test usually follows:

- **Arrange**
- **Act**
- **Assert**

### Arrange

Prepare test data using repositories and test services.

Typical tools:

- `TestDataService.GetRepository<T>()`
- repository methods such as `GetNew()`

### Act

Trigger real Dataverse behavior.

Typical actions:

- `TestDataService.CreateTestEntity(...)`
- `OrganizationService.Update(...)`

### Assert

Read data back and verify the real result.

Typical tools:

- `TestDataService.Query<TEntity>()...`
- assertions in xUnit

> [!NOTE]
> Tests do not simulate plugin execution.
> They trigger real Dataverse behavior and verify the real outcome.

---

## 🧹 Cleanup flow

Cleanup is handled centrally through `TestDataService`.

The standard flow is:

1. created test entities are tracked
2. cleanup starts after test execution
3. cleanup handlers run when registered
4. tracked entities are deleted

When dependent reference data must be removed first, the cleanup flow can use:

- `ICleanupDeleteHandler`

This allows pre-delete cleanup before the main entity is removed.

> [!IMPORTANT]
> Create tracked test data through `TestDataService`.
> Do not scatter cleanup logic across test methods.

---

## ✅ Recommendations

- keep tests in Arrange-Act-Assert structure
- create tracked entities through `TestDataService`
- use repositories for reusable test data
- keep assertions focused on business outcome
- use cleanup handlers only when reference cleanup is required

---

## ➡️ Related documents

- [Testing Overview](./testing.md)
- [Test Architecture](./test-architecture.md)
- [Test Data Lifecycle](./test-data-lifecycle.md)
- [Test Data Access](./data-access.md)