using $safeprojectname$.Logic.Tasks.Example;
using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.PluginRegistrations;

namespace $safeprojectname$.Logic.Plugins;

public class ExamplePlugin : PluginBase
{
    public ExamplePlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
    {
        RegisterTask<ExampleTask>(PluginStage.Prevalidation, ["Create", "Update"], "contact", PluginMode.Synchronous);
    }

    // Entity and attribute names are written as string literals because this template ships
    // without early-bound entity classes. After generating them with Tools/EarlyBound in the
    // Logic project, switch to the typed overloads - OnCreate<Contact>(...) with
    // Contact.Fields.FirstName - which keep the registration metadata bound to the entity type.
    public override void Register(IPluginRegistration registration)
    {
        registration
            .OnCreate("contact", "a4621296-6f72-42b6-b2c6-766732cec9fc")
            .PreValidation()
            .Synchronous()
            .WithName("Pillaro Example Plugin PreVal Create Contact")
            .Rank(1)
            .WithFilteringAttributes("firstname", "lastname");

        registration
            .OnUpdate("contact", "19d6cfed-9967-4465-9647-201ddb6a8082")
            .PreValidation()
            .Synchronous()
            .WithName("Pillaro Example Plugin PreVal Update Contact")
            .Rank(1)
            .WhenChanged("firstname", "lastname");
    }
}
