using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Pillaro.Dataverse.PluginFramework.Examples.Logic;
using Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Contact;
using Pillaro.Dataverse.PluginFramework.Examples.Tests.Data.Repositories;
using Pillaro.Dataverse.PluginFramework.Testing.Tests;

namespace Pillaro.Dataverse.PluginFramework.Examples.Tests.Tests.Contacts;

[Trait("Owner", "JM")]
[Trait("Category", nameof(ArchiveDeletedContact))]
public class ArchiveDeletedContactTests(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output)
    : TestBase(testFixture, output)
{
    [Fact]
    public void Delete_ContactWithParentAccount_RecordsDeletedContactOnAccount()
    {
        var account = TestDataService.GetRepository<AccountRepository>().GetNew();
        account.Id = TestDataService.CreateTestEntity(account);

        var contact = TestDataService.GetRepository<ContactRepository>()
            .GetNew("Deleted", "Contact", account.ToEntityReference());

        contact.Id = TestDataService.CreateTestEntity(contact);

        OrganizationService.Delete(Contact.EntityLogicalName, contact.Id);

        var loaded = OrganizationService.Retrieve(
            Account.EntityLogicalName,
            account.Id,
            new ColumnSet(Account.Fields.Description, Account.Fields.ModifiedBy));

        // The task runs on Delete, where the pre-image is the only source of the deleted values,
        // and writes the account through the update queue.
        Assert.Equal("Deleted contact: Deleted Contact", loaded.GetAttributeValue<string>(Account.Fields.Description));

        // The task queues the write for the initiating user, because a post-operation Delete runs
        // as SYSTEM. The audit therefore shows the person who deleted the contact.
        var currentUserId = ((WhoAmIResponse)OrganizationService.Execute(new WhoAmIRequest())).UserId;
        Assert.Equal(currentUserId, loaded.GetAttributeValue<EntityReference>(Account.Fields.ModifiedBy)?.Id);
    }

    [Fact]
    public void Delete_ContactWithoutParentAccount_DeletesWithoutError()
    {
        var contact = TestDataService.GetRepository<ContactRepository>().GetNew("Orphan", "Contact");

        contact.Id = TestDataService.CreateTestEntity(contact);

        // The step has a pre-image, the task simply has nothing to record.
        OrganizationService.Delete(Contact.EntityLogicalName, contact.Id);

        var found = TestDataService
            .Query<Contact>()
            .Where(x => x.Id == contact.Id)
            .Select(x => new Contact { Id = x.Id })
            .FirstOrDefault();

        Assert.Null(found);
    }
}
