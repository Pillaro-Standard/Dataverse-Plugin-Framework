# Testing Overview

> [!IMPORTANT]
> This document is the entry point for building integration tests against Dataverse using the Pillaro Dataverse Plugin Framework testing stack.
> Tests run against a real Dataverse environment with deployed plugins.
> This is integration testing, not unit testing.

> [!NOTE]
> The testing part of the repository is intentionally separate from plugin development.

---

## 📑 Navigation

- [⚙️ Prerequisites](#️-prerequisites)
- [🎯 Purpose](#-purpose)
- [🏗️ Basic principles](#️-basic-principles)
- [1. Create the test project](#1-create-the-test-project)
- [2. Install required packages](#2-install-required-packages)
- [3. Configure connection to Dataverse](#3-configure-connection-to-dataverse)
- [4. Create Autofac module](#4-create-autofac-module)
- [5. Create test data repositories](#5-create-test-data-repositories)
- [6. Create your test base class](#6-create-your-test-base-class)
- [7. Create your first integration test](#7-create-your-first-integration-test)
- [8. Run and verify](#8-run-and-verify)
- [✅ Recommendations](#-recommendations)
- [➡️ Next steps](#️-next-steps)

---

## ⚙️ Prerequisites

You need:

- a Microsoft Dataverse environment with deployed plugins
- Visual Studio 2022 or later
- a test project targeting `.NET 8` or later
- access to the `Logic` project
- basic knowledge of integration testing

> [!IMPORTANT]
> Tests run against a real Dataverse environment.
> Plugins must already be deployed and registered.

> [!NOTE]
> The framework testing stack supports xUnit only.

---

## 🎯 Purpose

The testing stack helps you:

- verify plugin behavior end-to-end
- test real business logic against Dataverse
- detect regressions
- manage test data lifecycle with cleanup

Tests follow the standard pattern:

1. **Arrange** — prepare test data
2. **Act** — create or update data in Dataverse
3. **Assert** — verify that plugin logic executed correctly

> [!NOTE]
> Tests do not simulate plugin execution.
> They create real data in Dataverse and verify real results.

---

## 🏗️ Basic principles

### Mirror plugin structure

The `Tests/` folder structure should mirror the `Tasks/` structure in `Logic`.

### Keep test data in repositories

Use repositories to centralize test data creation.

This improves:

- consistency
- reuse
- maintainability

### Use automatic cleanup

Create test records through `TestDataService.CreateTestEntity(...)`.

This ensures the framework can clean them up automatically.

### Use class-level traits

Each test class should define:

~~~csharp
[Trait("Owner", "YourName")]
[Trait("Category", nameof(ValidateContactAddressTask))]
~~~

Put them primarily on the test class.
Use method-level traits only when needed.

---

## 1. Create the test project

### 1.1 Create the project

~~~bash
dotnet new xunit -n YourSolution.Tests -f net8.0
~~~

### 1.2 Reference the Logic project

~~~xml
<ProjectReference Include="..\YourSolution.Logic\YourSolution.Logic.csproj" />
~~~

> [!IMPORTANT]
> Reference `Logic`, not the merged `Plugins` assembly.

### 1.3 Recommended structure

~~~
YourSolution.Tests/
├── Tests/
│   ├── Account/
│   ├── Contact/
│   └── TestBase.cs
├── Data/
│   └── Repositories/
├── TestAutofacModule.cs
└── appsettings.json
~~~

---

## 2. Install required packages

### 2.1 Install framework testing package

~~~bash
dotnet add package Pillaro.Dataverse.PluginFramework.Testing
~~~

### 2.2 Install xUnit packages

~~~bash
dotnet add package xunit.v3
dotnet add package xunit.runner.visualstudio
~~~

---

## 3. Configure connection to Dataverse

### 3.1 Create `appsettings.json`

~~~json
{
  "ConnectionStrings": {
    "Dataverse": "AuthType=OAuth;Url=https://yourorg.crm.dynamics.com;ClientId=your-client-id;ClientSecret=your-client-secret;"
  }
}
~~~

> [!IMPORTANT]
> Never commit real credentials to source control.

### 3.2 Copy config to output

~~~xml
<ItemGroup>
  <None Update="appsettings.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
~~~

### 3.3 Use user secrets for local development

~~~bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:Dataverse" "your-connection-string"
~~~

---

## 4. Create Autofac module

Create `TestAutofacModule.cs` in the test project root:

~~~csharp
using Autofac;
using Pillaro.Dataverse.PluginFramework.Testing;
using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

namespace YourSolution.Tests;

public class TestAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule<FrameworkTestingAutofacModule>();

        builder.RegisterAssemblyTypes(GetType().Assembly)
            .Where(t => t.GetInterfaces().Any(i => i.IsAssignableFrom(typeof(IAutoRegisteredTestDataRepository))))
            .AsSelf();
    }
}
~~~

This module:

- registers framework testing services
- auto-registers test data repositories

---

## 5. Create test data repositories

Repositories centralize reusable test data creation.

Example:

~~~csharp
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

Use repositories through:

~~~csharp
var contactRepo = TestDataService.GetRepository<ContactRepository>();
var contact = contactRepo.GetNew(firstName: "John", lastName: "Doe");
~~~

---

## 6. Create your test base class

Create a base class for all test classes:

~~~csharp
using Pillaro.Dataverse.PluginFramework.Testing.Tests;
using Xunit;

namespace YourSolution.Tests.Tests;

public abstract class TestBase : TestBase<TestAutofacModule>
{
    public TestBase(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output)
        : base(testFixture, output)
    {
    }
}
~~~

This gives you ready-to-use services such as:

- `TestDataService`
- `OrganizationService`
- `ConnectionService`
- `Output`

Cleanup happens automatically when the test finishes.

---

## 7. Create your first integration test

~~~csharp
using Logic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Pillaro.Dataverse.PluginFramework.Testing.Tests;
using Xunit;
using YourSolution.Tests.Data.Repositories;

namespace YourSolution.Tests.Tests.Contact;

[Trait("Owner", "YourName")]
[Trait("Category", nameof(ValidateContactAddressTask))]
public class ContactAddressValidationTests : TestBase
{
    public ContactAddressValidationTests(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output)
        : base(testFixture, output)
    {
    }

    [Fact]
    public void CreateContact_WithValidAddress_ShouldSucceed()
    {
        var contactRepo = TestDataService.GetRepository<ContactRepository>();
        var contact = contactRepo.GetNewWithAddress(
            firstName: "John",
            lastName: "Doe",
            addressLine1: "123 Main St",
            city: "Prague",
            postalCode: "11000"
        );

        var createdId = TestDataService.CreateTestEntity(contact);

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
    }

    [Fact]
    public void CreateContact_WithMissingPostalCode_ShouldFail()
    {
        var contactRepo = TestDataService.GetRepository<ContactRepository>();
        var contact = contactRepo.GetNewWithAddress(
            addressLine1: "123 Main St",
            city: "Prague",
            postalCode: null
        );

        Assert.Throws<InvalidPluginExecutionException>(
            () => TestDataService.CreateTestEntity(contact)
        );
    }
}
~~~

> [!IMPORTANT]
> Use `TestDataService.CreateTestEntity(...)` instead of direct `OrganizationService.Create(...)` for test-created records.

---

## 8. Run and verify

### 8.1 Deploy plugins

Make sure plugins are already deployed to the target environment.

### 8.2 Run tests

~~~bash
dotnet test
~~~

### 8.3 Verify results

After execution:

- tests should pass
- test data should be cleaned up
- the environment should remain clean

---

## ✅ Recommendations

- mirror plugin structure in the test project
- keep test data creation in repositories
- use `CreateTestEntity(...)` for cleanup-safe entity creation
- use class-level `Owner` and `Category` traits
- keep test code warning-free
- do not mock Dataverse services
- verify real behavior, not simulated behavior

---

## ➡️ Next steps

- [Test Architecture](./test-architecture.md)
- [Test Execution Flow](./test-execution-flow.md)
- [Test Data Lifecycle](./test-data-lifecycle.md)
- [Test Data Access](./data-access.md)

For plugin development, see:

- [Getting Started](../plugins/getting-started.md)