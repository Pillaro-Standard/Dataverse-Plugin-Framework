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
            RegisterTask<RecordJobTitleChange>(PluginStage.Preoperation, ["Update"], Contact.EntityLogicalName, PluginMode.Synchronous);
            RegisterTask<ArchiveDeletedContact>(PluginStage.Postoperation, [DataverseMessages.Delete], Contact.EntityLogicalName, PluginMode.Synchronous);
        }

        public override void Register(IPluginRegistration registration)
        {
            registration
                .OnCreate<Contact>("4e56ef4c-0e08-f111-8407-000d3ab261ac")
                .PreValidation()
                .Synchronous()
                .WithName("Pillaro Examples PreVal Create Contact")
                .Rank(1)
                // Typed attribute selection is available on Create steps too, not only on Update.
                .WithFilteringAttributes(c => c.FirstName, c => c.LastName)
                ;

            registration                
                .OnUpdate<Contact>("5056ef4c-0e08-f111-8407-000d3ab261ac")
                .PreValidation()
                .Synchronous()
                .WithName("Pillaro Examples PreVal Update Contact")
                .Rank(1)
                .WhenChanged("firstname", "lastname");

            registration
                .OnCreate<Contact>("4e72086e-1508-f111-8407-000d3ab261ac")
                .PreOperation()
                .Synchronous()
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
                .WithName("Pillaro Examples Pre Update Contact")
                .Rank(1)
                .WhenChanged(
                    c => c.FirstName,
                    c => c.LastName,
                    // The step also runs the task that records a job title change.
                    c => c.JobTitle,
                    c => c.Address1_Line1,
                    c => c.Address1_Line2,
                    c => c.Address1_Line3,
                    c => c.Address1_City,
                    c => c.Address1_PostalCode,
                    c => c.Address1_StateOrProvince,
                    c => c.Address1_Country)
                // Image attributes can be selected the typed way as well.
                .WithPreImage(
                    "d79f2630-9be7-4b0c-9fe3-bf5fc4d7d4f1",
                    "image",
                    c => c.Address1_Line1,
                    c => c.Address1_Line2,
                    c => c.Address1_Line3,
                    c => c.Address1_City,
                    c => c.Address1_PostalCode,
                    c => c.Address1_StateOrProvince,
                    c => c.Address1_Country);

            registration
                .OnDelete<Contact>("f0b83d33-0fe5-4e09-b22c-a4146ae1c7b3")
                .PostOperation()
                .Synchronous()
                .WithName("Pillaro Examples Post Delete Contact")
                .Rank(1)
                // On Delete the pre-image is the only source of the deleted values,
                // so the task depends on it being registered here.
                .WithPreImage(
                    "f849eb00-5139-49a8-bcab-2b8ab96ed443",
                    "image",
                    c => c.FirstName,
                    c => c.LastName,
                    c => c.ParentCustomerId);
        }
    }
}
