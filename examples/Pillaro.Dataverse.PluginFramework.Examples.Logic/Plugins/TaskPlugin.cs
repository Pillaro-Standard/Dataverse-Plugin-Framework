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
    [CrmPluginRegistration("Create",
    "task", StageEnum.PostOperation, ExecutionModeEnum.Synchronous,
    "","Pillaro Examples Post Create Task", 2,
    IsolationModeEnum.Sandbox
    ,Id = "a14d984d-0f31-f111-88b4-000d3ab2695d"
    )]
    [CrmPluginRegistration("Update",
    "task", StageEnum.PostOperation, ExecutionModeEnum.Synchronous,
    "regardingobjectid,scheduledend,statecode,statuscode","Pillaro Examples Post Update Task", 3,
    IsolationModeEnum.Sandbox
    ,Id = "b24d984d-0f31-f111-88b4-000d3ab2695d"
    ,Image1Name = "image"
    ,Image1Type = ImageTypeEnum.PreImage
    ,Image1Attributes = "regardingobjectid,scheduledend,statecode,statuscode,actualend"
    ,Image2Name = "image"
    ,Image2Type = ImageTypeEnum.PostImage
    ,Image2Attributes = "regardingobjectid,scheduledend,statecode,statuscode,actualend"
    )]
    public class TaskPlugin : PluginBase
    {
        public TaskPlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            //Pre
            RegisterTask<TaskAutoNumbering>(PluginStage.Preoperation, ["Create"], Task.EntityLogicalName, PluginMode.Synchronous);

            //Post
            RegisterTask<TaskSummarySync>(PluginStage.Postoperation, ["Create", "Update"], Task.EntityLogicalName, PluginMode.Synchronous);
        }
    }
}