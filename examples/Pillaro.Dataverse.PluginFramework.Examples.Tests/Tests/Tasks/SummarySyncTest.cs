using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Examples.Logic;
using Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Task;
using Pillaro.Dataverse.PluginFramework.Examples.Tests.Data.Repositories;
using Pillaro.Dataverse.PluginFramework.Testing.Tests;
using Task = Pillaro.Dataverse.PluginFramework.Examples.Logic.Task;

namespace Pillaro.Dataverse.PluginFramework.Examples.Tests.Tests.Tasks;

[Trait("Owner", "JM")]
[Trait("Category", nameof(SummarySync))]
public class SummarySyncTest(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output) : TestBase(testFixture, output)
{
    [Fact]
    public void Should_SetPlannedActivityDate_When_TaskWithScheduledEndIsCreatedForContact()
    {
        var contact = DataService.GetRepository<ContactRepository>().GetNew();
        contact.Id = DataService.CreateTestEntity(contact);

        var scheduledEnd = DateTime.UtcNow.Date.AddDays(7);
        var task = DataService.GetRepository<TaskRepository>().GetNew();
        task.RegardingObjectId = contact.ToEntityReference();
        task.ScheduledEnd = scheduledEnd;
        task.Id = DataService.CreateTestEntity(task);

        var loaded = LoadContactDescription(contact.Id);

        Assert.NotNull(loaded.Description);
        Assert.Contains($"Last planned activity: {scheduledEnd:yyyy-MM-dd}", loaded.Description);
        Assert.DoesNotContain("Last completed activity", loaded.Description);
    }

    [Fact]
    public void Should_SetCompletedActivityDate_When_TaskIsCompletedForContact()
    {
        var contact = DataService.GetRepository<ContactRepository>().GetNew();
        contact.Id = DataService.CreateTestEntity(contact);

        var task = DataService.GetRepository<TaskRepository>().GetNew();
        task.RegardingObjectId = contact.ToEntityReference();
        task.ScheduledEnd = DateTime.UtcNow.Date.AddDays(7);
        task.Id = DataService.CreateTestEntity(task);

        CompleteTask(task.Id);

        var loaded = LoadContactDescription(contact.Id);

        Assert.NotNull(loaded.Description);
        Assert.Contains("Last completed activity:", loaded.Description);
        Assert.DoesNotContain("Last planned activity:", loaded.Description);
    }

    [Fact]
    public void Should_SetBothDates_When_PlannedAndCompletedTasksExistForContact()
    {
        var contact = DataService.GetRepository<ContactRepository>().GetNew();
        contact.Id = DataService.CreateTestEntity(contact);

        var plannedDate = DateTime.UtcNow.Date.AddDays(14);
        var activeTask = DataService.GetRepository<TaskRepository>().GetNew();
        activeTask.RegardingObjectId = contact.ToEntityReference();
        activeTask.ScheduledEnd = plannedDate;
        activeTask.Id = DataService.CreateTestEntity(activeTask);

        var completedTask = DataService.GetRepository<TaskRepository>().GetNew();
        completedTask.RegardingObjectId = contact.ToEntityReference();
        completedTask.ScheduledEnd = DateTime.UtcNow.Date.AddDays(-3);
        completedTask.Id = DataService.CreateTestEntity(completedTask);

        CompleteTask(completedTask.Id);

        var loaded = LoadContactDescription(contact.Id);

        Assert.NotNull(loaded.Description);
        Assert.Contains($"Last planned activity: {plannedDate:yyyy-MM-dd}", loaded.Description);
        Assert.Contains("Last completed activity:", loaded.Description);
    }

    [Fact]
    public void Should_UseLatestScheduledEnd_When_MultipleActiveTasksExist()
    {
        var contact = DataService.GetRepository<ContactRepository>().GetNew();
        contact.Id = DataService.CreateTestEntity(contact);

        var earlierDate = DateTime.UtcNow.Date.AddDays(5);
        var laterDate = DateTime.UtcNow.Date.AddDays(30);

        var task1 = DataService.GetRepository<TaskRepository>().GetNew();
        task1.RegardingObjectId = contact.ToEntityReference();
        task1.ScheduledEnd = earlierDate;
        task1.Id = DataService.CreateTestEntity(task1);

        var task2 = DataService.GetRepository<TaskRepository>().GetNew();
        task2.RegardingObjectId = contact.ToEntityReference();
        task2.ScheduledEnd = laterDate;
        task2.Id = DataService.CreateTestEntity(task2);

        var loaded = LoadContactDescription(contact.Id);

        Assert.NotNull(loaded.Description);
        Assert.Contains($"Last planned activity: {laterDate:yyyy-MM-dd}", loaded.Description);
    }

    [Fact]
    public void Should_ClearDescription_When_NoTasksMeetConditions()
    {
        var contact = DataService.GetRepository<ContactRepository>().GetNew();
        contact.Id = DataService.CreateTestEntity(contact);

        var scheduledEnd = DateTime.UtcNow.Date.AddDays(7);
        var task = DataService.GetRepository<TaskRepository>().GetNew();
        task.RegardingObjectId = contact.ToEntityReference();
        task.ScheduledEnd = scheduledEnd;
        task.Id = DataService.CreateTestEntity(task);

        var loadedBefore = LoadContactDescription(contact.Id);
        Assert.NotNull(loadedBefore.Description);

        var update = new Task { Id = task.Id };
        update["scheduledend"] = null;
        DataService.OrganizationService.Update(update);

        var loadedAfter = LoadContactDescription(contact.Id);
        Assert.Null(loadedAfter.Description);
    }

    [Fact]
    public void Should_RecalculateBothRecords_When_RegardingChanges()
    {
        var contact1 = DataService.GetRepository<ContactRepository>().GetNew("First", "Contact");
        contact1.Id = DataService.CreateTestEntity(contact1);

        var contact2 = DataService.GetRepository<ContactRepository>().GetNew("Second", "Contact");
        contact2.Id = DataService.CreateTestEntity(contact2);

        var scheduledEnd = DateTime.UtcNow.Date.AddDays(10);
        var task = DataService.GetRepository<TaskRepository>().GetNew();
        task.RegardingObjectId = contact1.ToEntityReference();
        task.ScheduledEnd = scheduledEnd;
        task.Id = DataService.CreateTestEntity(task);

        var loaded1Before = LoadContactDescription(contact1.Id);
        Assert.Contains($"Last planned activity: {scheduledEnd:yyyy-MM-dd}", loaded1Before.Description);

        UpdateTaskRegarding(task.Id, contact2.ToEntityReference());

        var loaded1After = LoadContactDescription(contact1.Id);
        var loaded2After = LoadContactDescription(contact2.Id);

        Assert.Null(loaded1After.Description);
        Assert.NotNull(loaded2After.Description);
        Assert.Contains($"Last planned activity: {scheduledEnd:yyyy-MM-dd}", loaded2After.Description);
    }

    private Contact LoadContactDescription(Guid id)
    {
        return DataService
            .Query<Contact>()
            .Where(x => x.Id == id)
            .Select(x => new Contact { Description = x.Description })
            .First();
    }

    private void CompleteTask(Guid taskId)
    {
        DataService.OrganizationService.Execute(new SetStateRequest
        {
            EntityMoniker = new EntityReference(Task.EntityLogicalName, taskId),
            State = new OptionSetValue((int)TaskState.Completed),
            Status = new OptionSetValue(-1)
        });
        
    }

    private void UpdateTaskRegarding(Guid taskId, EntityReference newRegarding)
    {
        var update = new Task { Id = taskId, RegardingObjectId = newRegarding };
        DataService.OrganizationService.Update(update);
    }
}