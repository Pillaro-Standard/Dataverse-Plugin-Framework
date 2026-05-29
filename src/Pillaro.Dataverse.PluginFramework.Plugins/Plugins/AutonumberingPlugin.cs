using Pillaro.Dataverse.PluginFramework.PluginRegistrations;
using Pillaro.Dataverse.PluginFramework.Plugins.Tasks.Autonumbering;

namespace Pillaro.Dataverse.PluginFramework.Plugins.Plugins
{
    public class AutonumberingPlugin : PluginBase
    {
        public AutonumberingPlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            RegisterTask<GetAutoNumber>(PluginStage.Postoperation, ["pl_AutoNumbering_GetNewNumber"], "", PluginMode.Synchronous);
        }

        public override void Register(IPluginRegistration registration)
        {
            registration         
                .OnMessage("5a6a2b33-e410-f111-8407-000d3ab26bbc", "pl_AutoNumbering_GetNewNumber")
                .PostOperation()
                .Synchronous()
                .WithName("Post Operation pl_AutoNumbering_GetNewNumber")
                .InSolution("PillaroPluginFramework")
                .Rank(1);
        }
    }
}
