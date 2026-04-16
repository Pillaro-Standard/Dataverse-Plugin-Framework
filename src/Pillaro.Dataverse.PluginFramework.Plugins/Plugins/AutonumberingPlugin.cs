using Pillaro.Dataverse.PluginFramework.Plugins.Tasks.Autonumbering;

namespace Pillaro.Dataverse.PluginFramework.Plugins.Plugins
{

    [CrmPluginRegistration("pl_AutoNumbering_GetNewNumber", 
    "none", StageEnum.PostOperation, ExecutionModeEnum.Synchronous,
    "","Post Operation pl_AutoNumbering_GetNewNumber", 1, 
    IsolationModeEnum.Sandbox 
    ,Id = "5a6a2b33-e410-f111-8407-000d3ab26bbc" 
    )]
    public class AutonumberingPlugin : PluginBase
    {
        public AutonumberingPlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            RegisterTask<GetAutoNumber>(PluginStage.Postoperation, ["pl_AutoNumbering_GetNewNumber"], "", PluginMode.Synchronous);
        }
    }
}
