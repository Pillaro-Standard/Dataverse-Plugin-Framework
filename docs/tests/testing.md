# Testing Overview

> [!IMPORTANT]
> This document is the entry point for building integration tests against Dataverse using the Pillaro Dataverse Plugin Framework testing stack.
> Tests run against a real Dataverse environment with deployed plugins to verify end-to-end business logic.
> This is not unit testing — this is integration testing.

> [!NOTE]
> The testing part of the repository is intentionally separate from plugin development.
> You should be able to understand and use the testing stack without mixing it with plugin implementation details.

---

## 📑 Navigation

- [⚙️ Prerequisites](#️-prerequisites)
- [🎯 Purpose](#-purpose)
- [🏗️ Test architecture principles](#️-test-architecture-principles)
- [1. Create the test project](#1-create-the-test-project)
- [2. Install required packages](#2-install-required-packages)
- [3. Configure connection to Dataverse](#3-configure-connection-to-dataverse)
- [4. Create Autofac module for dependency injection](#4-create-autofac-module-for-dependency-injection)
- [5. Create test data repositories](#5-create-test-data-repositories)
- [6. Create your test base class](#6-create-your-test-base-class)
- [7. Create your first integration test](#7-create-your-first-integration-test)
- [8. Run and verify](#8-run-and-verify)
- [✅ Recommendations](#-recommendations)
- [➡️ Next steps](#️-next-steps)

---

## ⚙️ Prerequisites

You need:

- a Microsoft Dataverse environment with **deployed plugins**
- Visual Studio 2022 or later
- basic knowledge of integration testing
- support for `.NET 8` or later in test projects
- access to the `Logic` project containing business logic

> [!IMPORTANT]
> Tests run against a real Dataverse environment.
> Plugins must be deployed and registered in the target environment before running tests.
> Tests verify that the entire plugin pipeline executes correctly with real data.

> [!NOTE]
> The framework testing stack supports **xUnit only**.
> Other testing frameworks (NUnit, MSTest) are not supported.

---

## 🎯 Purpose

The testing stack enables you to:

- **Test complete business logic** against a real Dataverse environment
- **Verify plugin execution end-to-end** including all registered tasks and validations
- **Detect regressions** when new plugins or changes break existing behavior
- **Validate business rules** with real data before deploying to production
- **Manage test data lifecycle** with automatic cleanup

### What makes this different?

| Manual testing | Framework integration testing |
|----------------|------------------------------|
| Manual data creation in UI | Programmatic test data creation via repositories |
| Manual verification | Automated assertions |
| Manual cleanup (or no cleanup) | Automatic cleanup after each test |
| Hard to reproduce scenarios | Repeatable test scenarios with versioned test data |
| One environment state at a time | Isolated test data per test execution |
| Regression risk | Regression detection through automated test runs |

### Integration testing model

Tests follow the **Arrange-Act-Assert** pattern:

1. **Arrange** — prepare test data using repositories
2. **Act** — create/update data in Dataverse (triggers plugins)
3. **Assert** — retrieve and verify that plugin business logic executed correctly

> [!NOTE]
> Tests do NOT mock services or simulate plugin execution.
> They create real data in Dataverse, let plugins execute naturally, and verify the results.

---

## 🏗️ Test architecture principles

### Test organization mirrors plugin structure

The `Tests/` folder structure should mirror the `Tasks/` folder structure in the `Logic` project.

Example:

~~~
Logic/Tasks/
├── Account/
│   ├── ValidateAccountDataTask.cs
│   └── GenerateAccountNumberTask.cs
└── Contact/
    └── ValidateContactAddressTask.cs

Tests/Tests/
├── Account/
│   ├── AccountValidationTests.cs
│   └── AccountNumberingTests.cs
└── Contact/
    └── ContactAddressValidationTests.cs
~~~

This naming convention ensures clarity and makes it easy to find tests for specific business logic.

### One task, one test class (typically)

The recommended ratio is **1:1** — one task, one test class.

This ensures:

- Clear mapping between business logic and tests
- Easier navigation and maintenance
- Better accountability through test traits

### Test data governance through repositories

Test data is centralized in **repositories** to ensure:

- **Consistency**: all tests use the same base data structure
- **Maintainability**: when the data model changes, only repositories need updates (not all tests)
- **Reusability**: common test data patterns are defined once and reused
- **Governance**: centralized control over test data creation prevents data sprawl

### Automatic cleanup

The framework automatically tracks and cleans up test data created during test execution.

- Use `TestDataService.CreateTestEntity(...)` for all test entity creation
- Cleanup happens automatically via `TestBase.Dispose()`
- Environment stays clean without leftover test data

### Test ownership and traceability

Every test must include:

- **`[Trait("Owner", "...")]`** — identifies who created the test for accountability when issues arise
- **`[Trait("Category", "...")]`** — links the test to the corresponding task for traceability

This ensures efficient collaboration and faster issue resolution.

---

## 1. Create the test project

### 1.1 Create a test project

Create an xUnit test project targeting `.NET 8` or later:

~~~bash
dotnet new xunit -n YourSolution.Tests -f net8.0
~~~

### 1.2 Reference the Logic project

Add a project reference from `YourSolution.Tests` to `YourSolution.Logic`:

~~~xml
<ProjectReference Include="..\YourSolution.Logic\YourSolution.Logic.csproj" />
~~~

> [!IMPORTANT]
> Always reference the `Logic` project, not the merged `Plugins` assembly.
> Merged assemblies introduce duplicate types and dependency issues that make them unsuitable test targets.

### 1.3 Create the folder structure

Recommended structure:

~~~
YourSolution.Tests/
├── Tests/
│   ├── Account/
│   ├── Contact/
│   ├── TestBase.cs
│   └── ...
├── Data/
│   └── Repositories/
│       ├── AccountRepository.cs
│       ├── ContactRepository.cs
│       └── ...
├── TestAutofacModule.cs
└── appsettings.json
~~~

Purpose:

- `Tests/` → test classes organized by entity (mirroring `Tasks/` structure in Logic project)
- `Data/Repositories/` → centralized test data creation
- `TestAutofacModule.cs` → Autofac dependency injection configuration

---

## 2. Install required packages

### 2.1 Install the testing framework package

Add the framework testing package to your test project:

~~~bash
dotnet add package Pillaro.Dataverse.PluginFramework.Testing
~~~

### 2.2 Install xUnit packages

Add xUnit packages:

~~~bash
dotnet add package xunit.v3
dotnet add package xunit.runner.visualstudio
~~~

> [!NOTE]
> Only xUnit is supported. Do not use NUnit or MSTest.

---

## 3. Configure connection to Dataverse

### 3.1 Create appsettings.json

Create a configuration file in the root of your test project:

~~~json appsettings.json
{
  "ConnectionStrings": {
    "Dataverse": "AuthType=OAuth;Url=https://yourorg.crm.dynamics.com;ClientId=your-client-id;ClientSecret=your-client-secret;"
  }
}
~~~

> [!IMPORTANT]
> Never commit actual credentials to source control.
> Use user secrets, environment variables, or Azure Key Vault for sensitive data.

### 3.2 Configure file copy

Ensure the configuration file is copied to output:

~~~xml
<ItemGroup>
  <None Update="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
  <None Update="appsettings.Development.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
~~~

### 3.3 Use user secrets (recommended)

For local development, use user secrets:

~~~bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Dataverse" "your-connection-string"
~~~

---

## 4. Create Autofac module for dependency injection

### 4.1 What is the Autofac module for?

The `TestAutofacModule` configures dependency injection for the testing stack.

It is responsible for:

- Registering the framework testing services
- Auto-discovering and registering test data repositories
- Providing services to test classes via `TestBase`

### 4.2 Create TestAutofacModule

Create the Autofac module in the root of your test project:

~~~csharp TestAutofacModule.cs
using Autofac;
using Pillaro.Dataverse.PluginFramework.Testing;
using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

namespace YourSolution.Tests;

public class TestAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        // Register framework testing services
        builder.RegisterModule<FrameworkTestingAutofacModule>();

        // Auto-register all test data repositories
        builder.RegisterAssemblyTypes(GetType().Assembly)
            .Where(t => t.GetInterfaces().Any(i => i.IsAssignableFrom(typeof(IAutoRegisteredTestDataRepository))))
            .AsSelf();
    }
}
~~~

### 4.3 How repositories are registered

Repositories that implement `IAutoRegisteredTestDataRepository` are automatically discovered and registered in the DI container.

This allows you to access them via `TestDataService.GetRepository<T>()` in your tests.

---

## 5. Create test data repositories

### 5.1 What are test data repositories?

Repositories centralize test data creation to:

- Provide reusable base test data for all tests
- Enable data model changes to be handled in one place
- Allow tests to customize only what they need
- Improve long-term maintainability and governance

### 5.2 Create a repository class

Create a repository for each entity you test:

~~~csharp Data/Repositories/ContactRepository.cs
using Microsoft.Xrm.Sdk;
using Logic;
using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

namespace YourSolution.Tests.Data.Repositories;

public class ContactRepository : IAutoRegisteredTestDataRepository
{
    public Contact GetNew(
        string firstName = "Test",
        string lastName = "Contact",
        EntityReference? parentCustomer = null)
    {
        var contact = new Contact
        {
            FirstName = firstName,
            LastName = lastName
        };

        if (parentCustomer != null)
            contact.ParentCustomerId = parentCustomer;

        return contact;
    }

    public Contact GetNewWithAddress(
        string firstName = "Test",
        string lastName = "Contact",
        string? addressLine1 = null,
        string? city = null,
        string? postalCode = null)
    {
        var contact = GetNew(firstName, lastName);

        if (addressLine1 != null)
            contact.Address1_Line1 = addressLine1;

        if (city != null)
            contact.Address1_City = city;

        if (postalCode != null)
            contact.Address1_PostalCode = postalCode;

        return contact;
    }
}
~~~

### 5.3 Repository key principles

- **Implement `IAutoRegisteredTestDataRepository`**: enables automatic discovery and DI registration
- **Provide default values**: allow tests to call `GetNew()` without parameters for standard data
- **Support customization**: expose parameters for optional fields tests need to customize
- **Multiple factory methods**: create methods like `GetNew()`, `GetNewWithAddress()`, `GetNewWithDetails()` for different data scenarios

### 5.4 Access repositories in tests

Repositories are accessed through `TestDataService`:

~~~csharp
var contactRepo = TestDataService.GetRepository<ContactRepository>();
var contact = contactRepo.GetNew(firstName: "John", lastName: "Doe");
~~~

---

## 6. Create your test base class

### 6.1 Create TestBase

Create a base class that all test classes will inherit from:

~~~csharp Tests/TestBase.cs
using Autofac;
using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;
using Pillaro.Dataverse.PluginFramework.Testing.Tests;
using Xunit.Abstractions;

namespace YourSolution.Tests.Tests;

public abstract class TestBase : TestBase<TestAutofacModule>
{
    public TestBase(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output)
        : base(testFixture, output)
    {
    }
}
~~~

### 6.2 What TestBase provides

The framework `TestBase<TAutofacModule>` provides the following services:

| Service | Purpose |
|---------|---------|
| `TestDataService` | Create test entities and manage cleanup |
| `OrganizationService` | Direct access to Dataverse `IOrganizationService` |
| `ConnectionService` | Dataverse connection management |
| `Output` | xUnit test output for logging |
| `LifetimeScope` | Autofac lifetime scope for resolving services |

These services are already initialized and ready to use — no manual setup required.

### 6.3 Automatic cleanup on disposal

`TestBase` implements `IDisposable`.

When the test completes, `Dispose()` is called automatically, triggering cleanup via:

~~~csharp
TestDataService.DeleteTestEntities();
~~~

This ensures all test data is removed from the environment.

---

## 7. Create your first integration test

### 7.1 Create a test class

Create a test class for a specific entity:

~~~csharp Tests/Contact/ContactAddressValidationTests.cs
using Logic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Pillaro.Dataverse.PluginFramework.Testing.Tests;
using Xunit;
using Xunit.Abstractions;
using YourSolution.Tests.Data.Repositories;

namespace YourSolution.Tests.Tests.Contact;

[Trait("Owner", "YourName")]
[Trait("Category", nameof(ContactAddressValidation)]
public class ContactAddressValidationTests : TestBase
{
    public ContactAddressValidationTests(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output)
        : base(testFixture, output)
    {
    }

    [Fact]
    public void CreateContact_WithValidAddress_ShouldSucceed()
    {
        // Arrange - prepare test data using repository
        var contactRepo = TestDataService.GetRepository<ContactRepository>();
        var contact = contactRepo.GetNewWithAddress(
            firstName: "John",
            lastName: "Doe",
            addressLine1: "123 Main St",
            city: "Prague",
            postalCode: "11000"
        );

        // Act - create in Dataverse (triggers plugin execution)
        var createdId = TestDataService.CreateTestEntity(contact);

        // Assert - retrieve and verify plugin logic executed correctly
        var retrieved = OrganizationService.Retrieve(
            Logic.Contact.EntityLogicalName,
            createdId,
            new ColumnSet(
                nameof(Logic.Contact.FirstName),
                nameof(Logic.Contact.LastName),
                nameof(Logic.Contact.Address1_Line1),
                nameof(Logic.Contact.Address1_City),
                nameof(Logic.Contact.Address1_PostalCode)
            )
        ).ToEntity<Logic.Contact>();
        
        Assert.Equal("John", retrieved.FirstName);
        Assert.Equal("Doe", retrieved.LastName);
        Assert.Equal("123 Main St", retrieved.Address1_Line1);
        Assert.Equal("Prague", retrieved.Address1_City);
        Assert.Equal("11000", retrieved.Address1_PostalCode);
        
        // Cleanup happens automatically via TestBase.Dispose()
    }

    [Fact]
    public void CreateContact_WithMissingPostalCode_ShouldFail()
    {
        // Arrange
        var contactRepo = TestDataService.GetRepository<ContactRepository>();
        var contact = contactRepo.GetNewWithAddress(
            addressLine1: "123 Main St",
            city: "Prague",
            postalCode: null // Missing required field
        );

        // Act & Assert - expect plugin validation to prevent creation
        Assert.Throws<InvalidPluginExecutionException>(
            () => TestDataService.CreateTestEntity(contact)
        );
    }
}
~~~

### 7.2 Test structure explained

Each test follows the **Arrange-Act-Assert** pattern:

1. **Arrange**:
   - Get repository via `TestDataService.GetRepository<T>()`
   - Call repository method (e.g., `GetNew()`, `GetNewWithAddress()`)
   - Customize only the fields needed for this specific test

2. **Act**:
   - Create entity using `TestDataService.CreateTestEntity(...)` (triggers plugins)
   - Or update entity using `OrganizationService.Update(...)` (triggers plugins)

3. **Assert**:
   - Retrieve entity from Dataverse using `OrganizationService.Retrieve(...)`
   - Verify that plugin business logic executed correctly
   - Check calculated fields, validations, or side effects

> [!IMPORTANT]
> Always use `TestDataService.CreateTestEntity(...)` instead of direct `OrganizationService.Create(...)`.
> This ensures automatic cleanup after test execution.

### 7.3 Required test attributes

Each test class should define these attributes:

~~~csharp
[Trait("Owner", "YourName")]
[Trait("Category", nameof([TaskClass]))]
~~~

They should be placed primarily on the **test class**.

A test method should define them only if they are not already provided on the test class.

- **Owner**: Identifies who created or owns the test. When a test fails, the team knows who to contact.
- **Category**: Links the test to the corresponding task for traceability between tests and business logic.

> [!NOTE]
> The `Category` trait should reference the task name, for example `ValidateContactAddressTask`.
> This creates a clear mapping between tests and the related task implementation.

---

## 8. Run and verify

### 8.1 Deploy plugins to environment

Before running tests, ensure plugins are deployed and registered:

~~~bash
# Build and deploy your plugins to the target Dataverse environment
dotnet build YourSolution.Plugins
# Deploy using Plugin Registration Tool or your deployment pipeline
~~~

### 8.2 Run tests

Execute tests using Visual Studio Test Explorer or CLI:

~~~bash
dotnet test
~~~

### 8.3 Verify results

After test execution:

- ✅ Tests should pass if plugin logic is correct
- ✅ Test data should be automatically cleaned up via `TestBase.Dispose()`
- ✅ Environment should remain clean without orphaned records

> [!NOTE]
> If a test fails before cleanup, some test data may remain.
> The framework attempts cleanup even on failure, but catastrophic failures may prevent it.

---

## ✅ Recommendations

### Test organization

- **Mirror plugin structure**: `Tests/` folder should mirror `Tasks/` folder structure
- **One task, one test class**: typically maintain a 1:1 ratio between tasks and test classes
- **One entity per folder**: `Tests/Account/`, `Tests/Contact/`, etc.
- **One concern per test**: each test should validate a single business rule or scenario
- **Descriptive names**: test names should clearly state what they verify

### Test data management

- **Use repositories**: centralize test data creation in `Data/Repositories/`
- **Implement `IAutoRegisteredTestDataRepository`**: enables auto-discovery via `TestAutofacModule`
- **Default values in repositories**: allow `GetNew()` calls without parameters
- **Customize in tests**: modify only what's needed after calling repository methods
- **Use `CreateTestEntity`**: always create test data through `TestDataService.CreateTestEntity(...)`

### Data governance

- **Centralize data changes**: when data model changes, update repositories (not all tests)
- **Reuse repository methods**: avoid duplicating data creation logic across tests
- **Multiple factory methods**: create `GetNew()`, `GetNewWithDetails()`, etc. for different scenarios

### Test quality

- **Integration tests only**: do NOT mock Dataverse services
- **Test real behavior**: verify actual plugin execution against Dataverse
- **Use `StringComparison`**: always specify `StringComparison.OrdinalIgnoreCase` for string operations
- **Required traits**: every test must have `[Trait("Owner", "...")]` and `[Trait("Category", "...")]`

### Test ownership and accountability

- **Owner trait**: use `[Trait("Owner", "YourName")]` to identify test creator
- **Category trait**: use `[Trait("Category", "TaskName")]` to link test to business logic
- **Contact efficiency**: when tests fail, the team knows who to contact for help
- **Traceability**: category trait creates clear mapping between tests and tasks

### Performance

- **Parallel execution**: structure tests to support parallel execution when possible
- **Minimize roundtrips**: batch operations where appropriate
- **Reuse connections**: connections are managed by `TestBase` via `ConnectionService`

---

## ➡️ Next steps

After creating your first test, explore:

1. [Test Architecture](./test-architecture.md) — understand the structure and building blocks
2. [Test Execution Flow](./test-execution-flow.md) — learn how setup, execution, and cleanup work
3. [Test Data Lifecycle](./test-data-lifecycle.md) — manage test data creation and removal
4. [Test Data Access](./data-access.md) — work with Dataverse through the testing stack

For plugin development, see the [Plugin Development](../plugins/getting-started.md) section.

---

**Questions?** Open a [Discussion](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/discussions) or check [Issues](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/issues)