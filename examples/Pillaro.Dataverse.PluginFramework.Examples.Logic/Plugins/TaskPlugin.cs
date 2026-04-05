using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Task;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Plugins
{
    [CrmPluginRegistration("Create", 
    "task", StageEnum.PreOperation, ExecutionModeEnum.Synchronous,
    "subject","Pillaro Examples Pre Create Task", 1, 
    IsolationModeEnum.Sandbox 
    ,Id = "f94d984d-0f31-f111-88b4-000d3ab2695d" 
    )]
    public class TaskPlugin : PluginBase
    {
        public TaskPlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            //Pre
            RegisterTask<TaskAutoNumbering>(PluginStage.Preoperation, ["Create"], Task.EntityLogicalName, PluginMode.Synchronous);
        }
    }
}