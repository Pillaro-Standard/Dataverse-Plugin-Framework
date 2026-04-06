using Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Task;
using Pillaro.Dataverse.PluginFramework.Examples.Tests.Data.Repositories;
using Pillaro.Dataverse.PluginFramework.Testing.Tests;
using Xunit.Sdk;
using Task = Pillaro.Dataverse.PluginFramework.Examples.Logic.Task;

namespace Pillaro.Dataverse.PluginFramework.Examples.Tests.Tests.Tasks;

[Trait("Owner", "JM")]
[Trait("Category", nameof(TaskAutoNumbering))]
public class TaskAutoNumberingTest(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output) : TestBase(testFixture, output)
{
    [Fact]
    public void Should_PrefixSubjectWithAutoNumber_When_TaskIsCreated()
    {
        var task = DataService.GetRepository<TaskRepository>().GetNew();
        task.Subject = "Follow up call";
        task.Id = DataService.CreateTestEntity(task);

        var loaded = LoadTask(task.Id);

        Assert.NotNull(loaded.Subject);
        Assert.NotEqual(task.Subject, loaded.Subject);
        Assert.Contains(": Follow up call", loaded.Subject);
    }

    [Fact]
    public void Should_ContainOriginalSubject_When_TaskIsCreated()
    {
        var task = DataService.GetRepository<TaskRepository>().GetNew();
        task.Subject = "Prepare proposal";

        task.Id = DataService.CreateTestEntity(task);

        var loaded = LoadTask(task.Id);

        Assert.NotEqual(task.Subject, loaded.Subject);
        Assert.Contains(": Prepare proposal", loaded.Subject);
    }

    [Fact]
    public void Should_HandleEmptySubject_When_TaskIsCreated()
    {
        var task = DataService.GetRepository<TaskRepository>().GetNew();
        task.Subject = "";

        task.Id = DataService.CreateTestEntity(task);

        var loaded = LoadTask(task.Id);

        Assert.NotNull(loaded.Subject);
        Assert.NotEqual(task.Subject, loaded.Subject);
        Assert.EndsWith(": ", loaded.Subject);
    }

    [Fact]
    public void Should_GenerateDifferentNumbers_When_MultipleTasksCreated()
    {
        var task1 = DataService.GetRepository<TaskRepository>().GetNew();
        task1.Subject = "Task 5";

        var task2 = DataService.GetRepository<TaskRepository>().GetNew();
        task2.Subject = "Task 5";

        task1.Id = DataService.CreateTestEntity(task1);
        task2.Id = DataService.CreateTestEntity(task2);

        var loaded1 = LoadTask(task1.Id);
        var loaded2 = LoadTask(task2.Id);

        Assert.NotEqual(loaded1.Subject, loaded2.Subject);
    }

    private Task LoadTask(Guid id)
    {
        return DataService
            .Query<Task>()
            .Where(x => x.Id == id)
            .Select(x => new Task { Subject = x.Subject })
            .First();
    }
}