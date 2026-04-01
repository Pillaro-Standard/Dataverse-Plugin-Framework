using Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Opportunity;
using Pillaro.Dataverse.PluginFramework.Plugins;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Plugins
{
    [CrmPluginRegistration("Create", 
    "opportunity", StageEnum.PreValidation, ExecutionModeEnum.Synchronous,
    "","Pilalro Demo PreVal Create Opportunity", 1, 
    IsolationModeEnum.Sandbox 
    ,Id = "14b4b6a7-a311-f111-8407-000d3ab2695d" 
    )]
    [CrmPluginRegistration("Update", 
    "opportunity", StageEnum.PreValidation, ExecutionModeEnum.Synchronous,
    "","Pilalro Demo PreVal Update Opportunity", 1, 
    IsolationModeEnum.Sandbox 
    ,Id = "a3ed41af-a311-f111-8407-000d3ab2695d" 
    )]
    [CrmPluginRegistration("Create", 
    "opportunity", StageEnum.PreOperation, ExecutionModeEnum.Synchronous,
    "","Pilalro Demo Pre Create Opportunity", 1, 
    IsolationModeEnum.Sandbox 
    ,Id = "b1ed41af-a311-f111-8407-000d3ab2695d" 
    )]
    [CrmPluginRegistration("Update", 
    "opportunity", StageEnum.PreOperation, ExecutionModeEnum.Synchronous,
    "","Pilalro Demo Pre Update Opportunity", 1, 
    IsolationModeEnum.Sandbox 
    ,Id = "b4ed41af-a311-f111-8407-000d3ab2695d" 
    )]
    [CrmPluginRegistration("Create", 
    "opportunity", StageEnum.PostOperation, ExecutionModeEnum.Synchronous,
    "","Pilalro Demo Post Create Opportunity", 1, 
    IsolationModeEnum.Sandbox 
    ,Id = "baed41af-a311-f111-8407-000d3ab2695d" 
    )]
    [CrmPluginRegistration("Update", 
    "opportunity", StageEnum.PostOperation, ExecutionModeEnum.Synchronous,
    "","Pilalro Demo Post Update Opportunity", 1, 
    IsolationModeEnum.Sandbox 
    ,Id = "c5ed41af-a311-f111-8407-000d3ab2695d" 
    )]
    public class OpportunityPlugin : PluginBase
    {
        public OpportunityPlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            //RegisterTask<ForbiddenNamesTask>(PluginStage.Prevalidation, new[] { "Create", "Update" }, Contact.EntityLogicalName, PluginMode.Synchronous);
            RegisterTask<Autonumbering>(PluginStage.Preoperation, new[] { "Create" }, Opportunity.EntityLogicalName, PluginMode.Synchronous); 
        }
    }
}
