using Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Contact;
using Pillaro.Dataverse.PluginFramework.PluginRegistrations;
using Pillaro.Dataverse.PluginFramework.Plugins;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Plugins
{
    public class ContactPlugin : PluginBase
    {
        private const string SolutionName = "PillaroPluginFrameworkExamples";

        public ContactPlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            RegisterTask<ValidateNames>(PluginStage.Prevalidation, ["Create", "Update"], Contact.EntityLogicalName, PluginMode.Synchronous);
            RegisterTask<UpdateAddressLabel>(PluginStage.Preoperation, ["Create", "Update"], Contact.EntityLogicalName, PluginMode.Synchronous);
        }

        public override void Register(IPluginRegistration registration)
        {
            registration
                //.OnCreate("contact", "4e56ef4c-0e08-f111-8407-000d3ab261ac")
                .OnCreate<Contact>("4e56ef4c-0e08-f111-8407-000d3ab261ac")
                .PreValidation()
                .Synchronous()
                .InSolution(SolutionName)
                .WithName("Pillaro Examples PreVal Create Contact")
                .Rank(1)
                .WithFilteringAttributes("firstname", "lastname")
                ;

            registration
                //.OnUpdate("contact", "5056ef4c-0e08-f111-8407-000d3ab261ac")
                .OnUpdate<Contact>("5056ef4c-0e08-f111-8407-000d3ab261ac")
                .PreValidation()
                .Synchronous()
                .InSolution(SolutionName)
                .WithName("Pillaro Examples PreVal Update Contact")
                .Rank(1)
                .WhenChanged("firstname", "lastname");

            registration
                .OnCreate<Contact>("4e72086e-1508-f111-8407-000d3ab261ac")
                .PreOperation()
                .Synchronous()
                .InSolution(SolutionName)
                .WithName("Pillaro Examples Pre Create Contact")
                .Rank(1)
                .WithFilteringAttributes(
                    "firstname",
                    "lastname",
                    "address1_line1",
                    "address1_line2",
                    "address1_line3",
                    "address1_city",
                    "address1_postalcode",
                    "address1_stateorprovince",
                    "address1_country");

            registration
                .OnUpdate<Contact>("5072086e-1508-f111-8407-000d3ab261ac")
                .PreOperation()
                .Synchronous()
                .InSolution(SolutionName)
                .WithName("Pillaro Examples Pre Update Contact")
                .Rank(1)
                .WhenChanged(
                    "firstname",
                    "lastname",
                    "address1_line1",
                    "address1_line2",
                    "address1_line3",
                    "address1_city",
                    "address1_postalcode",
                    "address1_stateorprovince",
                    "address1_country")
                .WithPreImage(
                    "d79f2630-9be7-4b0c-9fe3-bf5fc4d7d4f1",
                    "image",
                    "address1_line1",
                    "address1_line2",
                    "address1_line3",
                    "address1_city",
                    "address1_postalcode",
                    "address1_stateorprovince",
                    "address1_country");
        }
    }
}
