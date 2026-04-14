using Pillaro.Dataverse.PluginFramework.Logging.Models;

namespace Pillaro.Dataverse.PluginFramework.Tasks;

public interface ITask
{
    void Execute();
    Log GetTaskLog();
}