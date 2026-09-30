---
name: Tests
description: Integration tests against a real Dataverse environment — test data, assertions, cleanup, running them.
applyTo: "**/*Tests/**/*.cs"
# Also matches test projects that do not use Dataverse — the first paragraph excludes them.
---

<!-- Managed by pillaro-dv ai-sync (Pillaro Dataverse Plugin Framework). Do not edit: the next sync overwrites this file. Your own rules belong in AGENTS.md, outside the managed block. -->

# Integration tests

Full rules and reasons: [`70-testing.md`](../../.pillaro/ai/rules/70-testing.md). Paths and the
reference test: [`project-setup.md`](../../.pillaro/project-setup.md).

Tests run against a real Dataverse environment with the deployed plugin. Never mock
`IOrganizationService` or any Dataverse behavior (PF-TEST-001). This applies to the solution's
integration test project, the one that references `Pillaro.Dataverse.PluginFramework.Testing`.

## Structure

- One test class per task: `Tests/<Entity>/<TaskName>Tests.cs`, inheriting the project's `TestBase`
  (PF-TEST-015). The namespace follows the folder: `<Tests root>.Tests.<Entity>`.
- Inside those namespaces an entity name is also a namespace name: write `Logic.Contact`, never a
  bare `Contact`. With a `Tests/Task/` folder, a bare `Task` is the namespace too — write
  `System.Threading.Tasks.Task` for an `async` test, or keep the test synchronous.
- Add every new file to the test project file — the project lists its compile items explicitly, and a
  test class that is not listed never runs (PF-BUILD-007).
- Tests reference the `Logic` project, never the merged `Plugins` assembly (PF-ARCH-003).
- Every class has `[Trait("Owner", "<initials>")]` and `[Trait("Category", nameof(<TaskName>))]`. The
  owner is the developer responsible for the task — ask for the initials, never copy them from an
  example (PF-TEST-004).
- Method names are the `tests:` names from the approved plan.
- At least one happy-path test and one business-rejection test per task (PF-TEST-005).
- A test is `void` or `async Task`, never `async void`. Every test asserts something. A disabled test uses
  `Skip = "reason"`, never a comment. Limits come from configuration (PF-TEST-014).

## Test data

- Records come from a repository in `Data/Repositories/` (`GetNew(...)`) and are created with
  `TestDataService.CreateTestEntity(...)` — never `OrganizationService.Create(...)` (PF-TEST-002/003).
  Change only the fields the scenario is about. A new repository implements
  `IAutoRegisteredTestDataRepository`, otherwise `GetRepository<T>()` cannot find it.
- The repository's default record must pass every task on that entity and message. Unique values
  come from a GUID fragment, not the clock. Reference data is queried by business key, never a
  hard-coded GUID (PF-TEST-013).
- Records created by the plugin, or through an impersonated service, are registered with
  `TestDataService.AddTestEntityToDelete(...)`. When a relation blocks deleting the test record, add
  an `ICleanupDeleteHandler` — see
  [Test Data Lifecycle](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/tests/test-data-lifecycle.md) (PF-TEST-011).
- Never modify a shared configuration record. If the test cannot work without it, stop and ask.

## Assertions

- After the act, read the record back from Dataverse by id and assert on that — not on the object
  you sent (PF-TEST-009).
- A business rejection arrives as `FaultException<OrganizationServiceFault>`. Assert the type and
  `ex.Detail.Message` with the text from the requirement (PF-TEST-008).
- Asynchronous steps: `await TestDataService.WaitOnAsyncProcess(id)`, then assert
  `GetAsyncProcessResults(id)` succeeded. Never `Thread.Sleep` or `Task.Delay` (PF-TEST-010).
- A rule that depends on the user's role or business unit is tested with a user who has it and one
  who does not, via `ConnectionService.GetOrganizationService(userId)` (PF-TEST-012).

## Running the tests

- Only when you are told to, and only against a dedicated dev environment (PF-TEST-007, PF-ENV-001).
- The connection string comes from user-secrets or an environment variable. Never write it anywhere
  (PF-ENV-002).
- Before the first run, check that the test settings set `ExpectedEnvironmentUrl`. `TestFixture`
  then refuses to run against any other environment; without the setting it checks nothing, so ask
  the developer to set it. Never bypass the check (PF-ENV-006).
- A failing test is fixed by fixing the task, or reported to a human. Never edit a test until it
  passes (PF-ENV-007). When the test of task A fails with task B's message, the cause is task B or the
  repository default record.

## Shape

```csharp
[Trait("Owner", "<initials of the responsible developer>")]
[Trait("Category", nameof(ValidateAccountName))]
public class ValidateAccountNameTests(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output)
    : TestBase(testFixture, output)
{
    [Fact]
    public void CreateAccount_WithAllowedName_Succeeds()
    {
        var account = TestDataService.GetRepository<AccountRepository>().GetNew();

        account.Id = TestDataService.CreateTestEntity(account);

        var created = TestDataService.Query<Logic.Account>().Where(a => a.Id == account.Id).FirstOrDefault();
        Assert.NotNull(created);
    }

    [Fact]
    public async Task CreateAccount_WithForbiddenName_ShouldBeRejected()
    {
        var account = TestDataService.GetRepository<AccountRepository>().GetNew("Fake Company");

        var ex = await Assert.ThrowsAsync<FaultException<OrganizationServiceFault>>(() =>
            Task.Run(() => TestDataService.CreateTestEntity(account)));

        Assert.Contains("is a forbidden word", ex.Detail.Message);
    }
}
```
