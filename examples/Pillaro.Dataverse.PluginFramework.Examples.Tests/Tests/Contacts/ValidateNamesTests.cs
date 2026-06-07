//using Microsoft.Xrm.Sdk;
//using Newtonsoft.Json;
//using Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Contact;
//using Pillaro.Dataverse.PluginFramework.Examples.Tests.Data.Repositories;
//using Pillaro.Dataverse.PluginFramework.Testing.Tests;
//using System.ServiceModel;

//namespace Pillaro.Dataverse.PluginFramework.Examples.Tests.Tests.Contacts;

//[Trait("Owner", "JM")]
//[Trait("Category", nameof(ValidateNames))]
//public class ValidateNamesTests : TestBase
//{
//    private readonly List<string> _forbiddenWords;

//    public ValidateNamesTests(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output) : base(testFixture, output)
//    {
//        var forbiddenWordsJson = SettingService.GetJsonValue("ForbiddenWords");
//        _forbiddenWords = JsonConvert.DeserializeObject<List<string>>(forbiddenWordsJson) ?? [];
//    }

//    [Fact]
//    public void CreateContact_WithValidNames_ShouldSucceed()
//    {
//        var contact = TestDataService.GetRepository<ContactRepository>().GetNew("ValidTestFirstName", "ValidTestLastName");

//        // Act
//        contact.Id = TestDataService.CreateTestEntity(contact);


//        var created = TestDataService.Query<Logic.Contact>()
//            .Where(c => c.Id == contact.Id)
//            .FirstOrDefault();

//        Assert.NotNull(created);
//        Assert.Equal("ValidTestFirstName", created.FirstName);
//        Assert.Equal("ValidTestLastName", created.LastName);
//    }

//    [Fact]
//    public async Task CreateContact_WithForbiddenFirstName_ShouldThrow()
//    {
//        // Arrange
//        Assert.True(_forbiddenWords.Count > 0, "ForbiddenWords setting must contain at least one entry.");

//        var forbiddenFirstName = _forbiddenWords[0];
//        var contact = TestDataService.GetRepository<ContactRepository>().GetNew(forbiddenFirstName, "ValidTestLastName");

//        // Act & Assert
//        var ex = await Assert.ThrowsAsync<FaultException<OrganizationServiceFault>>(() =>
//            Task.Run(() => TestDataService.CreateTestEntity(contact)));

//        Assert.Contains("First name is forbidden word", ex.Detail.Message);
//    }

//    [Fact]
//    public async Task CreateContact_WithForbiddenLastName_ShouldThrow()
//    {
//        // Arrange
//        Assert.True(_forbiddenWords.Count > 0, "ForbiddenWords setting must contain at least one entry.");

//        var forbiddenLastName = _forbiddenWords[0];
//        var contact = TestDataService.GetRepository<ContactRepository>().GetNew("ValidTestFirstName", forbiddenLastName);

//        // Act & Assert
//        var ex = await Assert.ThrowsAsync<FaultException<OrganizationServiceFault>>(() =>
//            Task.Run(() => TestDataService.CreateTestEntity(contact)));

//        Assert.Contains("Last name is forbidden word", ex.Detail.Message);
//    }

//    [Fact]
//    public async Task CreateContact_WithForbiddenFirstNameCaseInsensitive_ShouldThrow()
//    {
//        // Arrange
//        Assert.True(_forbiddenWords.Count > 0, "ForbiddenWords setting must contain at least one entry.");

//        var forbiddenWord = _forbiddenWords[0];
//        var mixedCaseName = forbiddenWord.Length > 1
//            ? char.ToUpper(forbiddenWord[0]) + forbiddenWord.Substring(1).ToLower()
//            : forbiddenWord.ToUpper();

//        var contact = TestDataService.GetRepository<ContactRepository>().GetNew(mixedCaseName, "ValidTestLastName");

//        // Act & Assert
//        var ex = await Assert.ThrowsAsync<FaultException<OrganizationServiceFault>>(() =>
//            Task.Run(() => TestDataService.CreateTestEntity(contact)));

//        Assert.Contains("First name is forbidden word", ex.Detail.Message);
//    }

//    [Fact]
//    public async Task UpdateContact_WithForbiddenFirstName_ShouldThrow()
//    {
//        // Arrange
//        Assert.True(_forbiddenWords.Count > 0, "ForbiddenWords setting must contain at least one entry.");

//        var contact = TestDataService.GetRepository<ContactRepository>().GetNew("ValidTestFirstName", "ValidTestLastName");
//        var contactId = TestDataService.CreateTestEntity(contact, byPassPlugins: true);

//        var updateEntity = new Logic.Contact
//        {
//            Id = contactId,
//            FirstName = _forbiddenWords[0]
//        };

//        // Act & Assert
//        var ex = await Assert.ThrowsAsync<FaultException<OrganizationServiceFault>>(() =>
//            Task.Run(() => OrganizationService.Update(updateEntity), TestContext.Current.CancellationToken));

//        Assert.Contains("First name is forbidden word", ex.Detail.Message);
//    }

//    [Fact]
//    public async Task UpdateContact_WithForbiddenLastName_ShouldThrow()
//    {
//        // Arrange
//        Assert.True(_forbiddenWords.Count > 0, "ForbiddenWords setting must contain at least one entry.");

//        var contact = TestDataService.GetRepository<ContactRepository>().GetNew("ValidTestFirstName", "ValidTestLastName");
//        var contactId = TestDataService.CreateTestEntity(contact, byPassPlugins: true);

//        var updateEntity = new Logic.Contact
//        {
//            Id = contactId,
//            LastName = _forbiddenWords[0]
//        };

//        // Act & Assert
//        var ex = await Assert.ThrowsAsync<FaultException<OrganizationServiceFault>>(() =>
//            Task.Run(() => OrganizationService.Update(updateEntity), TestContext.Current.CancellationToken));

//        Assert.Contains("Last name is forbidden word", ex.Detail.Message);
//    }

//    [Fact]
//    public void UpdateContact_WithValidNames_ShouldSucceed()
//    {
//        // Arrange
//        var contact = TestDataService.GetRepository<ContactRepository>().GetNew("OriginalFirst", "OriginalLast");
//        var contactId = TestDataService.CreateTestEntity(contact, byPassPlugins: true);

//        var updateEntity = new Logic.Contact
//        {
//            Id = contactId,
//            FirstName = "UpdatedValidFirst",
//            LastName = "UpdatedValidLast"
//        };

//        // Act
//        OrganizationService.Update(updateEntity);

//        // Assert
//        var updated = TestDataService.Query<Logic.Contact>()
//            .Where(c => c.Id == contactId)
//            .FirstOrDefault();
//        Assert.NotNull(updated);
//        Assert.Equal("UpdatedValidFirst", updated.FirstName);
//        Assert.Equal("UpdatedValidLast", updated.LastName);
//    }
//}