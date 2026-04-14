# Test Data Access

> [!IMPORTANT]
> Tests run against a real Dataverse environment.
> Data access should stay explicit, predictable, and aligned with the testing stack.

---

## 📑 Navigation

- [🔍 What this document covers](#-what-this-document-covers)
- [🧱 Main access services](#-main-access-services)
- [⚙️ Connection model](#️-connection-model)
- [👤 Access under different users](#-access-under-different-users)
- [🗝️ Keyed test services](#️-keyed-test-services)
- [🧭 Query and write patterns](#-query-and-write-patterns)
- [✅ Recommendations](#-recommendations)
- [➡️ Related documents](#️-related-documents)

---

## 🔍 What this document covers

This document explains how tests access Dataverse data in the testing stack.

It focuses on:

- the default Dataverse connection
- named Dataverse connections
- access through `TestDataService`
- access under different user identities
- keyed registration for additional test services

---

## 🧱 Main access services

The main data access services in tests are:

- `ITestDataService`
- `IDataverseConnectionService`
- `OrganizationService`

Typical usage:

- use `ITestDataService` for test data creation, tracking, cleanup, and querying
- use `OrganizationService` when direct Dataverse operations are needed
- use `IDataverseConnectionService` when a specific connection or caller identity must be resolved

> [!IMPORTANT]
> In normal test code, `ITestDataService` should be the primary access point.

---

## ⚙️ Connection model

The testing stack supports:

- the default connection string
- named connection strings
- caller-based access on top of both

Examples:

~~~csharp
// Default "Dataverse"
var service = connectionService.GetOrganizationService();

// Named connection string
var customService = connectionService.GetOrganizationService("CustomDataverse");

// Caller + named connection string
var serviceWithCaller = connectionService.GetOrganizationService(callerId, "CustomDataverse");
~~~

This makes it possible to scale test access across:

- one default environment
- multiple configured Dataverse connections
- different Dataverse user identities

> [!NOTE]
> Connection scaling and caller-based scaling are separate concerns.
> The testing stack now supports both.

---

## 👤 Access under different users

Tests sometimes need to run under different Dataverse users.

This is useful when you need to verify:

- security behavior
- ownership behavior
- caller-specific plugin behavior
- different runtime permissions

Caller-based access can be resolved through the connection service.

This allows the same test stack to run:

- under the default user
- under a specific caller
- under a specific caller on a named connection

This should be used when identity matters for the tested scenario.

---

## 🗝️ Keyed test services

When tests need reusable access services for predefined users or scenarios, the recommended pattern is keyed registration.

Example registration:

~~~csharp
builder.Register(ctx =>
{
    var scope = ctx.Resolve<ILifetimeScope>();
    var connectionService = ctx.Resolve<IDataverseConnectionService>();

    var service = connectionService.GetOrganizationService("SalesDataverse");

    return (ITestDataService)new TestDataService(service, scope);
})
.Keyed<ITestDataService>("sales");
~~~

Example usage:

~~~csharp
var salesService = _lifetimeScope.ResolveKeyed<ITestDataService>("sales");
~~~

This is useful when:

- a test base or helper needs a predefined user-specific data service
- the same test project works with several known user identities
- access should be resolved by key instead of rebuilt manually in each test

> [!IMPORTANT]
> If different user-specific access is reused often, register keyed `ITestDataService` instances instead of repeating connection setup in test methods.

---

## 🧭 Query and write patterns

Preferred patterns in tests:

### Query

Use:

- `TestDataService.Query<TEntity>()`

This keeps test querying aligned with the framework data access model.

### Create tracked test data

Use:

- `TestDataService.CreateTestEntity(...)`

This ensures the entity is tracked for cleanup.

### Add existing entity to cleanup

Use:

- `AddTestEntityToDelete(...)`

This is useful when the entity was not created through the normal tracked creation flow.

### Direct Dataverse operations

Use `OrganizationService` when a direct Dataverse operation is needed and the test does not benefit from the higher-level helper flow.

---

## ✅ Recommendations

- use `ITestDataService` as the default test access layer
- query data through `TestDataService.Query<TEntity>()`
- create tracked entities through `CreateTestEntity(...)`
- use `AddTestEntityToDelete(...)` when cleanup tracking must be added manually
- use caller-based access when tests must run under different users
- use named connection strings when tests must run against different configured environments
- use keyed `ITestDataService` registrations when the same user-specific services are reused often
- keep connection setup out of individual test methods

> [!TIP]
> Prefer one clear access pattern per test.
> Most tests should not mix several connection and data access styles unless there is a real reason.

---

## ➡️ Related documents

- [Testing Overview](./testing.md)
- [Test Architecture](./test-architecture.md)
- [Test Execution Flow](./test-execution-flow.md)
- [Test Data Lifecycle](./test-data-lifecycle.md)