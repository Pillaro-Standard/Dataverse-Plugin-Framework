# Testing

> [!IMPORTANT]
> Read [`AGENTS.md`](../../../AGENTS.md) first. This file is one entry in its rule catalog.
> Full model: [Testing Overview](../../tests/testing.md), [Test Execution Flow](../../tests/test-execution-flow.md).

| ID | Rule | Source |
|---|---|---|
| PF-TEST-001 | Tests are integration tests against live Dataverse. NEVER mock Dataverse services. | [testing.md](../../tests/testing.md) |
| PF-TEST-002 | Create test records through `TestDataService.CreateTestEntity(...)`, not `OrganizationService.Create(...)`. | [testing.md](../../tests/testing.md) |
| PF-TEST-003 | Test data comes from a repository in `Data/Repositories/` (`IAutoRegisteredTestDataRepository`). | [testing.md](../../tests/testing.md) |
| PF-TEST-004 | Every test class has `[Trait("Owner", …)]` and `[Trait("Category", nameof(SomeTask))]`. | [testing.md](../../tests/testing.md) |
| PF-TEST-005 | Every new task gets at least one happy-path test and one business-rejection test. | [getting-started.md](../../plugins/getting-started.md) |
| PF-TEST-006 | Test code has zero warnings, same as production code. | [testing.md](../../tests/testing.md) |
| PF-TEST-007 | NEVER run integration tests against an environment without explicit instruction to do so. | — |

## PF-ENV-* — running tests against a live dev environment

These apply whenever you have been given a connection to a real Dataverse environment.

| ID | Rule |
|---|---|
| PF-ENV-001 | Work ONLY against the dedicated dev environment. NEVER against test/UAT/production. |
| PF-ENV-002 | Read the connection string from user-secrets or an environment variable. NEVER write it into a repo file, a log, or your own output. |
| PF-ENV-003 | Create data ONLY through `TestDataService.CreateTestEntity(...)`, so cleanup works (PF-TEST-002). |
| PF-ENV-004 | NEVER delete or modify records you did not create in the current run. |
| PF-ENV-005 | NEVER run deployment or register an assembly yourself (PF-PROC-005) — you test against what is already deployed. |
| PF-ENV-006 | Before running integration tests, verify the connected environment against `ExpectedEnvironmentUrl`; stop on mismatch. Implemented in `TestFixture` — never bypass or remove the check. |
| PF-ENV-007 | A failing integration test is fixed by fixing the task, or escalated to a human. NEVER "fixed" by editing the test to pass. |

## Test-first for a new task (TDD loop)

1. Write the test(s) for the task's happy path and its business-rejection path **before** the task's
   `DoExecute()` has real logic (a stub that throws `NotImplementedException`, or simply before the
   task class exists, is fine).
2. Run the tests — they fail. That failure is the point: it proves the test actually exercises the
   new behavior and isn't accidentally passing for an unrelated reason.
3. Implement the task.
4. Run the tests again — they pass.

```csharp
[Trait("Owner", "JM")]
[Trait("Category", nameof(ValidateNames))]
public class ValidateNamesTests(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output)
    : TestBase(testFixture, output)
{
    [Fact]
    public void CreateContact_WithValidNames_ShouldSucceed()
    {
        var contact = TestDataService.GetRepository<ContactRepository>().GetNew("ValidFirst", "ValidLast");

        contact.Id = TestDataService.CreateTestEntity(contact);

        var created = TestDataService.Query<Logic.Contact>().Where(c => c.Id == contact.Id).FirstOrDefault();
        Assert.NotNull(created);
    }
}
```

```csharp
// ❌ Wrong — mocks the very thing the test suite exists to exercise for real
var mockOrgService = new Mock<IOrganizationService>();
mockOrgService.Setup(s => s.Create(It.IsAny<Entity>())).Returns(Guid.NewGuid());
```

```csharp
// ❌ Wrong — bypasses TestDataService, so cleanup never tracks or removes this record
var id = OrganizationService.Create(contact);
```

## ➡️ Related

- [Testing Overview](../../tests/testing.md)
- [Test Execution Flow](../../tests/test-execution-flow.md)
- [Test Data Lifecycle](../../tests/test-data-lifecycle.md)
