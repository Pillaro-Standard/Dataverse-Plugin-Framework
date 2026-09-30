# Testing

> [!IMPORTANT]
> Read [`AGENTS.md`](../../../AGENTS.md) first. This file is one entry in its rule catalog.
> Full model: [Testing Overview](../../tests/testing.md), [Test Execution Flow](../../tests/test-execution-flow.md).

| ID | Rule | Source |
|---|---|---|
| PF-TEST-001 | Tests are integration tests against live Dataverse. NEVER mock Dataverse services. | [testing.md](../../tests/testing.md) |
| PF-TEST-002 | Create test records through `TestDataService.CreateTestEntity(...)`, not `OrganizationService.Create(...)`. | [testing.md](../../tests/testing.md) |
| PF-TEST-003 | Test data comes from a repository in `Data/Repositories/` (`IAutoRegisteredTestDataRepository`). | [testing.md](../../tests/testing.md) |
| PF-TEST-004 | Every test class has `[Trait("Owner", …)]` and `[Trait("Category", nameof(SomeTask))]`. `Owner` is the initials of the developer responsible for the task — ask if you do not know them; never copy a value from an example. | [testing.md](../../tests/testing.md) |
| PF-TEST-005 | Every new task gets at least one happy-path test and one business-rejection test. | [getting-started.md](../../plugins/getting-started.md) |
| PF-TEST-006 | Test code has zero warnings, same as production code. | [testing.md](../../tests/testing.md) |
| PF-TEST-007 | NEVER run integration tests against an environment without explicit instruction to do so. | — |
| PF-TEST-008 | A business rejection reaches the test as `FaultException<OrganizationServiceFault>` — never `InvalidPluginExecutionException` or `DataverseValidationException`. Assert the type **and** `ex.Detail.Message`. | live-verified |
| PF-TEST-009 | After Act, re-read the record from Dataverse by Id and assert on that. Never assert on the object you sent. | observed in large production solutions |
| PF-TEST-010 | Asynchronous steps: `await TestDataService.WaitOnAsyncProcess(id)`, then assert `GetAsyncProcessResults(id)` succeeded. NEVER `Thread.Sleep` / `Task.Delay`. | framework API |
| PF-TEST-011 | Records created by a plugin, or through an impersonated service, are registered with `AddTestEntityToDelete(...)`. Relations that block deletion get an `ICleanupDeleteHandler`. | framework API |
| PF-TEST-012 | A rule that depends on the user's role or business unit is tested on both sides, impersonating a user who has it and one who does not. NEVER rely on the test identity being an administrator. | observed gap |
| PF-TEST-013 | The repository default record must pass **every** task on that entity and message, not just the one under test. Unique values come from a GUID fragment, not the clock. Reference data is queried by business key, never a hard-coded GUID. | observed in large production solutions |
| PF-TEST-014 | A test is `void` or `async Task` (never `async void`), every test asserts something, no test is commented out (use `Skip = "reason"`), limits come from configuration rather than being hard-coded. | observed anti-patterns |
| PF-TEST-015 | One test class per task — `<TaskName>Tests` in a `Tests/<Entity>/` folder mirroring `Tasks/<Entity>/`, namespace following the folder. Method names are the `tests:` names from the plan. Inside `…Tests.<Entity>` write `Logic.Contact`, not a bare `Contact` (the name is also a namespace); with a `Tests/Task/` folder, a bare `Task` is a namespace in every test namespace — write `System.Threading.Tasks.Task` for an async test. | [analysis-workflow.md](../analysis-workflow.md), `/examples` |

## PF-ENV-* — running tests against a live dev environment

These apply whenever you have been given a connection to a real Dataverse environment.

| ID | Rule |
|---|---|
| PF-ENV-001 | Work ONLY against the dedicated dev environment. NEVER against test/UAT/production. |
| PF-ENV-002 | Read the connection string from user-secrets or an environment variable. NEVER write it into a repo file, a log, or your own output. |
| PF-ENV-003 | Create data ONLY through `TestDataService.CreateTestEntity(...)`, so cleanup works (PF-TEST-002). |
| PF-ENV-004 | NEVER delete or modify records you did not create in the current run. |
| PF-ENV-005 | NEVER run deployment or register an assembly yourself (PF-PROC-005) — you test against what is already deployed. |
| PF-ENV-006 | Before running integration tests, verify the connected environment against `ExpectedEnvironmentUrl`; stop on mismatch. `TestFixture` does this, but only when the setting is present — if the test settings do not set it, ask the developer to set it before you run anything. Never bypass or remove the check. |
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
[Trait("Owner", "<initials of the responsible developer>")]
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

## Asserting a business rejection (PF-TEST-008)

The plugin throws `DataverseValidationException`; Dataverse turns it into a fault on the wire. The
test only ever sees `FaultException<OrganizationServiceFault>`:

```csharp
[Fact]
public async Task CreateAccount_WithForbiddenName_ShouldBeRejected()
{
    var account = TestDataService.GetRepository<AccountRepository>().GetNew("Fake Company");

    var ex = await Assert.ThrowsAsync<FaultException<OrganizationServiceFault>>(() =>
        Task.Run(() => TestDataService.CreateTestEntity(account)));

    Assert.Contains("is a forbidden word", ex.Detail.Message);
}
```

```csharp
// ❌ Never passes — this exception type does not exist on the client side
Assert.Throws<InvalidPluginExecutionException>(() => TestDataService.CreateTestEntity(account));
```

Assert on the message text from the requirement, not only on the type: a different task rejecting the
same record raises the same exception type.

## Act, then re-read (PF-TEST-009)

```csharp
OrganizationService.Update(new Logic.Account { Id = account.Id, PrimaryContactId = contact.ToEntityReference() });

var updated = TestDataService.Query<Logic.Account>().Where(a => a.Id == account.Id).FirstOrDefault();
Assert.Equal("second@example.test", updated?.EMailAddress1);
```

## Asynchronous steps (PF-TEST-010)

```csharp
using Pillaro.Dataverse.PluginFramework.Testing.Shared.Extensions;

account.Id = TestDataService.CreateTestEntity(account);

await TestDataService.WaitOnAsyncProcess(account.Id);
Assert.True((await TestDataService.GetAsyncProcessResults(account.Id)).IsNewestProcessValid());

// only now read back and assert the outcome
```

A sleep either waits too long on every run or not long enough on a busy environment — both make the
suite untrustworthy.

## Records the test did not create itself (PF-TEST-011)

```csharp
// The plugin created a follow-up task; make sure cleanup removes it too
var followUp = TestDataService.Query<Logic.Task>().Where(t => t.RegardingObjectId.Id == account.Id).FirstOrDefault();
TestDataService.AddTestEntityToDelete(followUp.ToEntityReference());
```

Use an `ICleanupDeleteHandler` when deleting the test record is blocked by records the plugin created
or by a lookup that points back at it — the handler removes or clears them first.

## Role-dependent rules (PF-TEST-012)

```csharp
var restrictedUser = ConnectionService.GetOrganizationService(nonAdminUserId);

var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() =>
    restrictedUser.Update(new Logic.Contact { Id = contact.Id, StateCode = contact_statecode.Inactive }));
Assert.Contains("Only an administrator", ex.Detail.Message);
```

Look up the user by a business key (domain name, a dedicated test user) — never a hard-coded GUID.
Anything created through `restrictedUser` bypasses `CreateTestEntity(...)` and must be registered with
`AddTestEntityToDelete(...)`.

## Test data that survives the whole plugin (PF-TEST-013)

Creating a record runs **every** task registered on that entity and message — in a mature solution,
dozens. The repository's default record must therefore be valid for all of them (mandatory fields,
duplicate checks, allowed values), or tests fail for reasons unrelated to the task under test.

```csharp
public Logic.Contact GetNew(string lastName = null) => new()
{
    FirstName = "Test",
    LastName = lastName ?? $"Test-{Guid.NewGuid():N}"[..20],          // unique: duplicate checks
    EMailAddress1 = $"test-{Guid.NewGuid():N}@example.test",
    TransactionCurrencyId = GetCurrencyByIsoCode("CZK"),                  // reference data: query by key
};
```

- Unique values: a GUID fragment. Clock ticks collide on fast machines and parallel runs.
- Reference data (countries, currencies, business units, settings): query it by business key. If a
  configuration record the test needs does not exist, create a dedicated one through
  `CreateTestEntity(...)`; never modify a shared one (PF-ENV-004). If the test cannot work without
  changing shared configuration, stop and ask a human.

**Reading a failure:** when the test for task A fails with task B's message, the defect is in task B
or in the repository default — not in task A, and not in the test.

## Test hygiene (PF-TEST-014)

```csharp
// ✅ Variants of one rule as a matrix
[Theory]
[InlineData("Fake Company")]
[InlineData("Test Company Ltd")]
public async Task CreateAccount_WithForbiddenName_ShouldBeRejected(string name) { /* ... */ }

// ✅ Temporarily disabled, with a reason someone can act on
[Fact(Skip = "Waits for pl_setting 'AccountForbiddenWords' in the test environment")]
public void ...
```

```csharp
// ❌ Each of these hides a broken test
public async void CreateAccount_...()          // exceptions are lost; the runner reports success
//[Fact]                                       // invisible: nobody knows it is disabled
[Fact] public void CreateAccount_Works() { TestDataService.CreateTestEntity(account); }  // no assert
```

## ➡️ Related

- [Testing Overview](../../tests/testing.md)
- [Test Execution Flow](../../tests/test-execution-flow.md)
- [Test Data Lifecycle](../../tests/test-data-lifecycle.md)
