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
    // Logic project, switch to the typed overloads - OnCreate<Contact>(...) with typed selectors
    // such as c => c.FirstName - which keep the registration metadata bound to the entity type.
    // The step ids below are generated for this project when it is created from the template,
    // so they never collide with another project's steps in the same environment.
    public override void Register(IPluginRegistration registration)
    {
        registration
            .OnCreate("contact", "$guid1$")
            .PreValidation()
            .Synchronous()
            .WithName($"{StepPrefix} contact Create PreValidation Synchronous")
            .Rank(1)
            .WithFilteringAttributes("firstname", "lastname");

        registration
            .OnUpdate("contact", "$guid2$")
            .PreValidation()
            .Synchronous()
            .WithName($"{StepPrefix} contact Update PreValidation Synchronous")
            .Rank(1)
            .WhenChanged("firstname", "lastname");
    }
}
