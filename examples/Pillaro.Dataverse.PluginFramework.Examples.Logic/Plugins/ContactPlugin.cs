using Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Contact;
using Pillaro.Dataverse.PluginFramework.Plugins;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Plugins
{
    [CrmPluginRegistration("Create", 
    "contact", StageEnum.PreValidation, ExecutionModeEnum.Synchronous,
    "firstname,lastname","Pilalro Demo PreVal Create Contact", 1, 
    IsolationModeEnum.Sandbox 
    ,Id = "4e56ef4c-0e08-f111-8407-000d3ab261ac" 
    )]
    [CrmPluginRegistration("Update", 
    "contact", StageEnum.PreValidation, ExecutionModeEnum.Synchronous,
    "firstname,lastname","Pilalro Demo PreVal Update Contact", 1, 
    IsolationModeEnum.Sandbox 
    ,Id = "5056ef4c-0e08-f111-8407-000d3ab261ac" 
    )]
    [CrmPluginRegistration("Create", 
    "contact", StageEnum.PreOperation, ExecutionModeEnum.Synchronous,
    "firstname,lastname,address1_line1,address1_line2,address1_line3,address1_city,address1_postalcode,address1_stateorprovince,address1_country","Pilalro Demo Pre Create Contact", 1, 
    IsolationModeEnum.Sandbox 
    ,Id = "4e72086e-1508-f111-8407-000d3ab261ac" 
    )]
    [CrmPluginRegistration("Update", 
    "contact", StageEnum.PreOperation, ExecutionModeEnum.Synchronous,
    "firstname,lastname,address1_line1,address1_line2,address1_line3,address1_city,address1_postalcode,address1_stateorprovince,address1_country","Pilalro Demo Pre Update Contact", 1, 
    IsolationModeEnum.Sandbox 
    ,Id = "5072086e-1508-f111-8407-000d3ab261ac" 
    )]
    [CrmPluginRegistration("Create", 
    "contact", StageEnum.PostOperation, ExecutionModeEnum.Synchronous,
    "","Pilalro Demo Post Create Contact", 1, 
    IsolationModeEnum.Sandbox 
    ,Id = "87598457-1008-f111-8407-000d3ab2695d" 
    )]
    [CrmPluginRegistration("Update", 
    "contact", StageEnum.PostOperation, ExecutionModeEnum.Synchronous,
    "","Pilalro Demo Post Update Contact", 1, 
    IsolationModeEnum.Sandbox 
    ,Id = "8a598457-1008-f111-8407-000d3ab2695d" 
    )]
    public class ContactPlugin : PluginBase
    {

        public ContactPlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            //PreVal
            RegisterTask<ForbiddenNamesTask>(PluginStage.Prevalidation, new[] { "Create", "Update" }, Contact.EntityLogicalName, PluginMode.Synchronous);


            //Pre
            RegisterTask<ScoreOnAddressChange>(PluginStage.Preoperation, new[] { "Create", "Update" }, Contact.EntityLogicalName, PluginMode.Synchronous);
        }
    }
}
