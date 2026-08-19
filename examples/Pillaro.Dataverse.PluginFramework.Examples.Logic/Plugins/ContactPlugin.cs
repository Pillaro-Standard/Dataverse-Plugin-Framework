using Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Contact;
using Pillaro.Dataverse.PluginFramework.PluginRegistrations;
using Pillaro.Dataverse.PluginFramework.Plugins;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Plugins
{
    public class ContactPlugin : PluginBase
    {
        
        public ContactPlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            RegisterTask<ValidateNames>(PluginStage.Prevalidation, ["Create", "Update"], Contact.EntityLogicalName, PluginMode.Synchronous);
            RegisterTask<UpdateAddressLabel>(PluginStage.Preoperation, ["Create", "Update"], Contact.EntityLogicalName, PluginMode.Synchronous);
        }

        public override void Register(IPluginRegistration registration)
        {
            registration
                .OnCreate<Contact>("4e56ef4c-0e08-f111-8407-000d3ab261ac")
                .PreValidation()
                .Synchronous()
                .WithName($"{StepPrefix} contact Create PreValidation Synchronous")
                .Rank(1)
                .WithFilteringAttributes(Contact.Fields.FirstName, Contact.Fields.LastName)
                ;

            registration                
                .OnUpdate<Contact>("5056ef4c-0e08-f111-8407-000d3ab261ac")
                .PreValidation()
                .Synchronous()
                .WithName($"{StepPrefix} contact Update PreValidation Synchronous")
                .Rank(1)
                .WhenChanged(Contact.Fields.FirstName, Contact.Fields.LastName);

            registration
                .OnCreate<Contact>("4e72086e-1508-f111-8407-000d3ab261ac")
                .PreOperation()
                .Synchronous()
                .WithName($"{StepPrefix} contact Create PreOperation Synchronous")
                .Rank(1)
                .WithFilteringAttributes(
                    Contact.Fields.FirstName,
                    Contact.Fields.LastName,
                    Contact.Fields.Address1_Line1,
                    Contact.Fields.Address1_Line2,
                    Contact.Fields.Address1_Line3,
                    Contact.Fields.Address1_City,
                    Contact.Fields.Address1_PostalCode,
                    Contact.Fields.Address1_StateOrProvince,
                    Contact.Fields.Address1_Country);

            registration
                .OnUpdate<Contact>("5072086e-1508-f111-8407-000d3ab261ac")
                .PreOperation()
                .Synchronous()
                .WithName($"{StepPrefix} contact Update PreOperation Synchronous")
                .Rank(1)
                .WhenChanged(
                    Contact.Fields.FirstName,
                    Contact.Fields.LastName,
                    Contact.Fields.Address1_Line1,
                    Contact.Fields.Address1_Line2,
                    Contact.Fields.Address1_Line3,
                    Contact.Fields.Address1_City,
                    Contact.Fields.Address1_PostalCode,
                    Contact.Fields.Address1_StateOrProvince,
                    Contact.Fields.Address1_Country)
                .WithPreImage(
                    "d79f2630-9be7-4b0c-9fe3-bf5fc4d7d4f1",
                    "image",
                    Contact.Fields.Address1_Line1,
                    Contact.Fields.Address1_Line2,
                    Contact.Fields.Address1_Line3,
                    Contact.Fields.Address1_City,
                    Contact.Fields.Address1_PostalCode,
                    Contact.Fields.Address1_StateOrProvince,
                    Contact.Fields.Address1_Country);
        }
    }
}
