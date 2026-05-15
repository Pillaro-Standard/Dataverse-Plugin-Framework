using Contoso.Dataverse.PatchingProject.Logic.Tasks.Contact;
using Pillaro.Dataverse.PluginFramework.PluginRegistrations;
using Pillaro.Dataverse.PluginFramework.Plugins;

namespace Contoso.Dataverse.PatchingProject.Logic.Plugins
{
    [CrmPluginRegistration(
        "Create",
        "contact",
        StageEnum.PreValidation,
        ExecutionModeEnum.Synchronous,
        "firstname,lastname",
        "Contact - PreValidation Create",
        1,
        IsolationModeEnum.Sandbox,
        Id = "55555555-5555-5555-5555-555555555555")]
    [CrmPluginRegistration(
        "Update",
        "contact",
        StageEnum.PreValidation,
        ExecutionModeEnum.Synchronous,
        "firstname,lastname",
        "Contact - PreValidation Update",
        1,
        IsolationModeEnum.Sandbox,
        Id = "66666666-6666-6666-6666-666666666666")]
    public class ContactPlugin : PluginBase
    {
        public ContactPlugin(string unsecureConfig, string secureConfig)
            : base(unsecureConfig, secureConfig)
        {
            RegisterTask<ValidateContactName>(
                PluginStage.Prevalidation,
                new[] { "Create", "Update" },
                "contact",
                PluginMode.Synchronous);
        }
    }
}
