# Pillaro.Dataverse.PluginFramework.Testing

A testing package focused on integration testing for Microsoft Dataverse solutions.

The package provides a structured and maintainable approach to testing Dataverse logic using real data, deterministic execution and automatic cleanup.

---

## What this package provides

- Infrastructure for integration testing against Microsoft Dataverse
- Test `DataService` for working with Dataverse in test scenarios
- Repository-based test data preparation
- Automatic creation and cleanup of test data
- Handling of referenced data that cannot be deleted directly
- Structured test setup using fixtures and dependency injection

---

## Why use it

- Reduce complexity of Dataverse integration tests
- Keep test data consistent and reusable
- Automatically clean up test data after execution
- Handle complex cleanup scenarios (relationships, references)
- Improve long-term maintainability of test suite
- Keep tests focused on behavior, not infrastructure

---

## Testing approach

Each test follows a deterministic lifecycle:

1. Prepare data using repositories  
2. Create data in Dataverse via `DataService`  
3. Execute tested logic (plugin / business operation)  
4. Verify result  
5. Automatically clean up all created data  

This ensures tests are isolated, predictable and maintainable.

---

## Project structure (recommended)

- **Repositories/**  
  Reusable test data definitions

- **Tests/**  
  Test scenarios (behavior only)

- **TestBase / Fixtures**  
  Shared setup (DI, lifecycle)

- **Infrastructure/Dataverse/**  
  `TestDataService`, cleanup, Dataverse access

- **AutoFacModule**  
  Dependency registration

---

## Core concepts

### TestDataService

Central entry point for test interaction with Dataverse.

- creates entities
- tracks created data
- provides querying
- ensures cleanup

---

### Data repositories

Encapsulate test data creation.

- reusable
- centralized
- resilient to schema changes

---

### Automatic cleanup

- deletes all created data
- handles referenced entities
- prevents test pollution

---

## Autofac configuration

Tests use Autofac for dependency injection.

```csharp
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
```

---

## Custom TestBase

You can extend the base test class to include your own services:

```csharp
public class TestBase : TestBase<TestAutofacModule>
{
    protected readonly SettingsService SettingService;

    public TestBase(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output)
        : base(testFixture, output)
    {
        SettingService = testFixture.Container.Resolve<SettingsService>();
    }
}
```

---

## Example test class

```csharp
[Trait("Owner", "JM")]
[Trait("Category", nameof(TestTask))]
public class TestTaskTest(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output) : TestBase(testFixture, output)
{
    [Fact]
    public void Valid_Firstname_And_Lastname()
    {
        var contact = new Contact
        {
            FirstName = "Jan",
            LastName = "Mucha"
        };

        contact.Id = TestDataService.CreateTestEntity(contact);

        var loaded = TestDataService
            .Query<Contact>()
            .Where(x => x.Id == contact.Id)
            .Select(x => new Contact { FirstName = x.FirstName, LastName = x.LastName })
            .First();

        Assert.Equal(contact.FirstName, loaded.FirstName);
        Assert.Equal(contact.LastName, loaded.LastName);
    }
}
```

---

## Recommended test structure

Each test should:

- use repositories for data preparation
- use `TestDataService` for all Dataverse interaction
- focus only on behavior validation
- rely on automatic cleanup

Avoid:

- manual entity construction inside tests
- manual cleanup logic
- duplicated data setup

---

## Maintainability strategy

The package is designed to reduce long-term cost of test maintenance:

- centralized data definitions (repositories)
- deterministic execution
- automatic cleanup

Result:

- easier refactoring
- lower maintenance cost
- higher clarity

---

## Where to find more

- GitHub: https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework

---

## License

This project is published under the Pillaro Community License (PCL) v1.0.

Attribution is required when the framework is used in delivered solutions:

> "This solution is built using Pillaro Dataverse Plugin Framework."