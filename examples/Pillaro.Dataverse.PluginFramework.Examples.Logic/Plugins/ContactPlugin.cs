using Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Contact;
using Pillaro.Dataverse.PluginFramework.Plugins;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Plugins
{
    [CrmPluginRegistration("Create", 
    "contact", StageEnum.PreValidation, ExecutionModeEnum.Synchronous,
    "firstname,lastname","Pillaro Examples PreVal Create Contact", 1, 
    IsolationModeEnum.Sandbox 
    ,Id = "4e56ef4c-0e08-f111-8407-000d3ab261ac" 
    )]
    [CrmPluginRegistration("Update", 
    "contact", StageEnum.PreValidation, ExecutionModeEnum.Synchronous,
    "firstname,lastname","Pillaro Examples PreVal Update Contact", 1, 
    IsolationModeEnum.Sandbox 
    ,Id = "5056ef4c-0e08-f111-8407-000d3ab261ac" 
    )]
    [CrmPluginRegistration("Create", 
    "contact", StageEnum.PreOperation, ExecutionModeEnum.Synchronous,
    "firstname,lastname,address1_line1,address1_line2,address1_line3,address1_city,address1_postalcode,address1_stateorprovince,address1_country","Pillaro Examples Pre Create Contact", 1, 
    IsolationModeEnum.Sandbox 
    ,Id = "4e72086e-1508-f111-8407-000d3ab261ac" 
    )]
    [CrmPluginRegistration("Update", 
    "contact", StageEnum.PreOperation, ExecutionModeEnum.Synchronous,
    "firstname,lastname,address1_line1,address1_line2,address1_line3,address1_city,address1_postalcode,address1_stateorprovince,address1_country","Pillaro Examples Pre Update Contact", 1, 
    IsolationModeEnum.Sandbox
    , Image1Type = ImageTypeEnum.PreImage
    , Image1Name = "image"
    , Image1Attributes = "address1_line1,address1_line2,address1_line3,address1_city,address1_postalcode,address1_stateorprovince,address1_country"
    , Id = "5072086e-1508-f111-8407-000d3ab261ac" 
    )]
    public class ContactPlugin : PluginBase
    {

        public ContactPlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            //PreVal
            RegisterTask<ValidateNames>(PluginStage.Prevalidation, ["Create", "Update"], Contact.EntityLogicalName, PluginMode.Synchronous);


            //Pre
            RegisterTask<UpdateAddressLabel>(PluginStage.Preoperation, ["Create", "Update"], Contact.EntityLogicalName, PluginMode.Synchronous);
        }
    }
}
