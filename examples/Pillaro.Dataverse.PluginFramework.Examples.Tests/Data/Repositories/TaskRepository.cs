using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;
using Task = Pillaro.Dataverse.PluginFramework.Examples.Logic.Task;

namespace Pillaro.Dataverse.PluginFramework.Examples.Tests.Data.Repositories;

public class TaskRepository : IAutoRegisteredTestDataRepository
{
    public Task GetNew(string subject = "Test subject")
    {
        var obj = new Task
        {
            Subject = subject
        };
        

        return obj;
    }
}