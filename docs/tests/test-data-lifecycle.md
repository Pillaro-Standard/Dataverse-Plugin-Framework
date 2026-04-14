# Test Data Lifecycle

> [!IMPORTANT]
> Test data should be created, tracked, queried, and cleaned in a controlled way.
> The testing stack provides built-in support for this lifecycle.

---

## 📑 Navigation

- [🔍 What test data lifecycle means](#-what-test-data-lifecycle-means)
- [🧱 Lifecycle stages](#-lifecycle-stages)
- [🧹 Cleanup and reference deletion](#-cleanup-and-reference-deletion)
- [✅ Recommendations](#-recommendations)
- [➡️ Related documents](#️-related-documents)

---

## 🔍 What test data lifecycle means

Test data lifecycle describes how test data moves through the test:

- create
- track
- use
- verify
- clean up

The goal is to keep tests:

- repeatable
- isolated
- cleanup-safe

---

## 🧱 Lifecycle stages

### 1. Create

Create test entities through:

- `TestDataService.CreateTestEntity(...)`

This ensures the created entity is tracked for cleanup.

If needed, an existing entity can also be added manually to cleanup tracking through:

- `AddTestEntityToDelete(...)`

### 2. Track

Tracked entities are stored for later cleanup.

This is why test-created data should not be created ad hoc through unrelated patterns when it is supposed to be removed automatically.

### 3. Use

Tests work with the created data in real Dataverse execution.

Typical usage includes:

- triggering plugins by create or update
- reading data back
- verifying side effects

For querying in tests, use:

- `TestDataService.Query<TEntity>()`

### 4. Verify

Assertions should verify the real result of Dataverse execution.

This includes things such as:

- changed field values
- created related records
- blocked operations
- expected plugin behavior

### 5. Clean up

After execution, tracked entities are deleted through the centralized cleanup flow.

This keeps the environment clean and avoids leftover test data.

---

## 🧹 Cleanup and reference deletion

Cleanup is coordinated through `TestDataService`.

When the main entity cannot be deleted directly because of dependent reference data, the testing stack supports:

- `ICleanupDeleteHandler`

A cleanup handler allows custom pre-delete logic before the main entity is removed.

Use this when cleanup must:

- remove dependent records
- remove blocking references
- resolve relationship constraints before deletion

> [!IMPORTANT]
> Cleanup is not only about deleting the created entity.
> It may also require controlled cleanup of related data first.

---

## ✅ Recommendations

- create tracked test entities through `TestDataService.CreateTestEntity(...)`
- query test results through `TestDataService.Query<TEntity>()`
- use `AddTestEntityToDelete(...)` when an entity must be added to cleanup manually
- keep cleanup centralized
- use `ICleanupDeleteHandler` only when dependent reference cleanup is required
- do not spread cleanup logic across test methods

> [!TIP]
> Good lifecycle management keeps tests predictable and environments clean.

---

## ➡️ Related documents

- [Testing Overview](./testing.md)
- [Test Architecture](./test-architecture.md)
- [Test Execution Flow](./test-execution-flow.md)
- [Test Data Access](./data-access.md)