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

The package is designed to work with a structured test project:

- **Repositories/**  
  Defines reusable test data (e.g. `ContactRepository`, `AccountRepository`)  
  Central place for managing test data structure

- **Tests/**  
  Contains test scenarios (focused only on behavior)

- **TestBase / Fixtures**  
  Shared setup (connection, DI, lifecycle)

- **Infrastructure/Dataverse/**  
  Implementation of:
  - `TestDataService`
  - cleanup handling
  - Dataverse communication

- **AutoFacModule**  
  Registers dependencies for test runtime

---

## Core concepts

### TestDataService

Main entry point for working with Dataverse in tests.

Responsibilities:
- create entities in Dataverse
- track created records
- ensure cleanup after test execution
- provide query capabilities

---

### Data repositories

Repositories define how test data is constructed.

Responsibilities:
- encapsulate entity creation logic
- provide reusable data definitions
- isolate schema changes

Benefits:
- single place for updating data structure
- reduced duplication
- clearer test intent

---

### Automatic cleanup

All data created during a test is automatically deleted.

Includes:
- standard entities
- referenced entities
- dependency-aware cleanup

Prevents:
- data pollution
- flaky tests
- manual cleanup logic

---

### Dependency injection

Tests use a prepared DI container (AutoFac):

- `DataService`
- repositories
- services

Ensures:
- consistent configuration
- reusable setup
- easy extensibility

---

## Example test flow

```csharp
[Fact]
public void Create_WithAddress_ShouldSetAddressLabel()
{
    var contact = DataService
        .GetRepository<ContactRepository>()
        .GetNewWithAddress("Jan", "Label", "Main street 1", "Prague", "11000", "CZ");

    contact.Id = DataService.CreateTestEntity(contact);

    var loaded = DataService
        .Query<Contact>()
        .Where(x => x.Id == contact.Id)
        .Select(x => new Contact { Address1_Name = x.Address1_Name })
        .First();

    Assert.Equal("Main street 1, Prague 11000, CZ", loaded.Address1_Name);
}
```

---

## Recommended test structure

Each test should:

- use repositories for data preparation
- use `DataService` for all Dataverse interaction
- focus only on behavior validation
- rely on automatic cleanup

Avoid:
- manual entity construction inside tests
- manual cleanup logic
- duplicated data setup

---

## Maintainability strategy

The package is designed to reduce long-term cost of test maintenance.

This is achieved by:

- repository-based data definitions
- centralized data structure management
- deterministic test execution
- automatic cleanup

Result:

- easier refactoring when Dataverse schema changes
- lower maintenance cost
- higher test clarity

---

## Where to find more

- GitHub: https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework

---

## License

This project is published under the Pillaro Community License (PCL) v1.0.

Attribution is required when the framework is used in delivered solutions:

> "This solution is built using Pillaro Dataverse Plugin Framework."