# Pillaro.Dataverse.PluginFramework.Tests

Test project for the **Dataverse Plugin Framework**.

Provides infrastructure primarily for **integration testing** of Dataverse plugin logic.

---

## Overview

Tests in this project are designed to run against a real or dedicated **Dataverse environment**.

The project separates:

- **Tests** → actual test cases  
- **Data** → test data, repositories, and cleanup logic  

This keeps tests clean while centralizing all test-related infrastructure.

---

## Requirements

To run the tests correctly, the following setup is required:

- A configured and accessible **Dataverse environment**
- Installed **Pillaro Framework solution**

All tests are built on top of the **standard Dataverse Sales Enterprise data model**:

- no custom entities are required  
- no additional fields are introduced  
- tests rely only on out-of-the-box Dataverse structures  

---

## Structure

    Data/
      Cleanup/        → handles cleanup dependencies before delete operations
      Repositories/   → provides structured test data

    Tests/
      *               → test implementations grouped by domain

    TestBase.cs       → shared test setup and lifecycle
    TestAutofacModule → dependency registration

---

## Key Concepts

### Integration-first approach

Tests are designed to validate behavior against Dataverse, including:

- CRUD operations  
- plugin execution  
- real data interactions  

---

### TestBase

All tests inherit from `TestBase`, which provides:

- initialized services for Dataverse communication  
- shared setup and lifecycle handling  
- automatic cleanup of created entities  

---

### Test Data Service

Responsible for:

- interacting with Dataverse (CRUD operations, queries)  
- creating test entities  
- tracking created data for cleanup  
- working with repositories as data sources  

---

### Repositories

Repositories act as centralized test data providers.

They should be designed so that changes in the data model can be handled in one place, without modifying individual tests.

---

### Cleanup

Cleanup handlers ensure that dependent data can be safely removed by resolving relationships before deletion.

---

### Test Lifecycle

1. Test creates data via test data service  
2. Created entities are tracked  
3. After execution, cleanup is triggered  
4. Dependencies are resolved  
5. All test data is removed  

---

## Conventions

- Namespaces follow folder structure **1:1**  
- Test infrastructure is centralized and reusable  
- Test data should be created via the test data service  
- Repositories are the single source of test data  

---

## Goal

Keep tests:

- realistic  
- maintainable  
- resilient to data model changes  