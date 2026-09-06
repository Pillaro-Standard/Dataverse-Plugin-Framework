using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Pillaro.Dataverse.PluginFramework.Examples.Logic;
using Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Contact;
using Pillaro.Dataverse.PluginFramework.Examples.Tests.Data.Repositories;
using Pillaro.Dataverse.PluginFramework.Testing.Tests;

namespace Pillaro.Dataverse.PluginFramework.Examples.Tests.Tests.Contacts;

[Trait("Owner", "JM")]
[Trait("Category", nameof(RecordJobTitleChange))]
public class RecordJobTitleChangeTests(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output)
    : TestBase(testFixture, output)
{
    [Fact]
    public void Update_JobTitle_SavesQueuedDescriptionWithTheSameOperation()
    {
        var contact = TestDataService.GetRepository<ContactRepository>().GetNew("Queued", "Contact");
        contact.Id = TestDataService.CreateTestEntity(contact);

        OrganizationService.Update(new Contact
        {
            Id = contact.Id,
            JobTitle = "Consultant",
            EntityState = EntityState.Changed
        });

        var loaded = OrganizationService.Retrieve(
            Contact.EntityLogicalName,
            contact.Id,
            new ColumnSet(Contact.Fields.Description, Contact.Fields.JobTitle, Contact.Fields.ModifiedBy));

        // Queued in a pre-stage, so the value is merged into the message target
        // and saved by the update that is already running.
        Assert.Equal("Consultant", loaded.GetAttributeValue<string>(Contact.Fields.JobTitle));
        Assert.Equal("Job title: Consultant", loaded.GetAttributeValue<string>(Contact.Fields.Description));

        // Saved by the caller's own operation, so the audit shows the caller and not the plugin.
        var currentUserId = ((WhoAmIResponse)OrganizationService.Execute(new WhoAmIRequest())).UserId;
        Assert.Equal(currentUserId, loaded.GetAttributeValue<EntityReference>(Contact.Fields.ModifiedBy)?.Id);
    }

    [Fact]
    public void Update_WithoutJobTitle_DoesNotTouchTheDescription()
    {
        var contact = TestDataService.GetRepository<ContactRepository>().GetNew("Unqueued", "Contact");
        contact.Description = "Original description";
        contact.Id = TestDataService.CreateTestEntity(contact);

        OrganizationService.Update(new Contact
        {
            Id = contact.Id,
            LastName = "Contact renamed",
            EntityState = EntityState.Changed
        });

        var loaded = TestDataService
            .Query<Contact>()
            .Where(x => x.Id == contact.Id)
            .Select(x => new Contact { Description = x.Description })
            .First();

        Assert.Equal("Original description", loaded.Description);
    }
}
