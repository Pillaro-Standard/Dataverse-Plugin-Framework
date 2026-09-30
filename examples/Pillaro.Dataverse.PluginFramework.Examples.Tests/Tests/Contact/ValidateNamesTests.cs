using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Contact;
using Pillaro.Dataverse.PluginFramework.Examples.Tests.Data.Repositories;
using Pillaro.Dataverse.PluginFramework.Testing.Tests;
using System.ServiceModel;

namespace Pillaro.Dataverse.PluginFramework.Examples.Tests.Tests.Contact;

[Trait("Owner", "JM")]
[Trait("Category", nameof(ValidateNames))]
public class ValidateNamesTests : TestBase
{
    private readonly List<string> _forbiddenWords;

    public ValidateNamesTests(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output) : base(testFixture, output)
    {
        // The task reads the same setting, so the tests follow whatever the environment forbids.
        var forbiddenWordsJson = SettingService.GetJsonValue("ForbiddenWords");
        _forbiddenWords = JsonConvert.DeserializeObject<List<string>>(forbiddenWordsJson) ?? [];
    }

    [Fact]
    public void CreateContact_WithValidNames_ShouldSucceed()
    {
        var contact = TestDataService.GetRepository<ContactRepository>().GetNew("ValidTestFirstName", "ValidTestLastName");

        contact.Id = TestDataService.CreateTestEntity(contact);

        var created = LoadContact(contact.Id);
        Assert.Equal("ValidTestFirstName", created.FirstName);
        Assert.Equal("ValidTestLastName", created.LastName);
    }

    [Fact]
    public void CreateContact_WithForbiddenFirstName_ShouldThrow()
    {
        var contact = TestDataService.GetRepository<ContactRepository>().GetNew(FirstForbiddenWord(), "ValidTestLastName");

        var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => TestDataService.CreateTestEntity(contact));

        Assert.Contains("First name is forbidden word", ex.Detail.Message);
    }

    [Fact]
    public void CreateContact_WithForbiddenLastName_ShouldThrow()
    {
        var contact = TestDataService.GetRepository<ContactRepository>().GetNew("ValidTestFirstName", FirstForbiddenWord());

        var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => TestDataService.CreateTestEntity(contact));

        Assert.Contains("Last name is forbidden word", ex.Detail.Message);
    }

    [Fact]
    public void CreateContact_WithForbiddenFirstNameCaseInsensitive_ShouldThrow()
    {
        var forbiddenWord = FirstForbiddenWord();
        var mixedCaseName = forbiddenWord.Length > 1
            ? char.ToUpperInvariant(forbiddenWord[0]) + forbiddenWord[1..].ToLowerInvariant()
            : forbiddenWord.ToUpperInvariant();

        var contact = TestDataService.GetRepository<ContactRepository>().GetNew(mixedCaseName, "ValidTestLastName");

        var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => TestDataService.CreateTestEntity(contact));

        Assert.Contains("First name is forbidden word", ex.Detail.Message);
    }

    [Fact]
    public void UpdateContact_WithForbiddenFirstName_ShouldThrow()
    {
        var contact = TestDataService.GetRepository<ContactRepository>().GetNew("ValidTestFirstName", "ValidTestLastName");
        var contactId = TestDataService.CreateTestEntity(contact, byPassPlugins: true);

        var update = new Logic.Contact { Id = contactId, FirstName = FirstForbiddenWord() };

        var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => OrganizationService.Update(update));

        Assert.Contains("First name is forbidden word", ex.Detail.Message);
    }

    [Fact]
    public void UpdateContact_WithForbiddenLastName_ShouldThrow()
    {
        var contact = TestDataService.GetRepository<ContactRepository>().GetNew("ValidTestFirstName", "ValidTestLastName");
        var contactId = TestDataService.CreateTestEntity(contact, byPassPlugins: true);

        var update = new Logic.Contact { Id = contactId, LastName = FirstForbiddenWord() };

        var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => OrganizationService.Update(update));

        Assert.Contains("Last name is forbidden word", ex.Detail.Message);
    }

    [Fact]
    public void UpdateContact_WithValidNames_ShouldSucceed()
    {
        var contact = TestDataService.GetRepository<ContactRepository>().GetNew("OriginalFirst", "OriginalLast");
        var contactId = TestDataService.CreateTestEntity(contact, byPassPlugins: true);

        OrganizationService.Update(new Logic.Contact
        {
            Id = contactId,
            FirstName = "UpdatedValidFirst",
            LastName = "UpdatedValidLast"
        });

        var updated = LoadContact(contactId);
        Assert.Equal("UpdatedValidFirst", updated.FirstName);
        Assert.Equal("UpdatedValidLast", updated.LastName);
    }

    private string FirstForbiddenWord()
    {
        Assert.True(_forbiddenWords.Count > 0, "The ForbiddenWords setting must contain at least one entry.");
        return _forbiddenWords[0];
    }

    private Logic.Contact LoadContact(Guid id)
    {
        return TestDataService
            .Query<Logic.Contact>()
            .Where(x => x.Id == id)
            .Select(x => new Logic.Contact { FirstName = x.FirstName, LastName = x.LastName })
            .First();
    }
}
