using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Examples.Logic;
using Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Contact;
using Pillaro.Dataverse.PluginFramework.Examples.Tests.Data.Repositories;
using Pillaro.Dataverse.PluginFramework.Testing.Tests;

namespace Pillaro.Dataverse.PluginFramework.Examples.Tests.Tests.Contacts;

[Trait("Owner", "JM")]
[Trait("Category", nameof(UpdateAddressLabel))]
public class UpdateAddressLabelTests(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output) 
    : TestBase(testFixture, output)
{

    [Fact]
    public void Create_WithAddress_ShouldSetAddressLabel()
    {
        Contact c = DataService.GetRepository<ContactRepository>()
            .GetNewWithAddress("Jan", "Label", "Main street 1", "Prague", "11000", "CZ");

        c.Id = DataService.CreateTestEntity(c);

        var loaded = DataService
            .Query<Contact>()
            .Where(x => x.Id == c.Id)
            .Select(x => new Contact { Address1_Name = x.Address1_Name })
            .First();

        Assert.False(string.IsNullOrWhiteSpace(loaded.Address1_Name), "Address1_Name must be set on create when address is provided.");
        Assert.Equal("Main street 1, Prague 11000, CZ", loaded.Address1_Name);
    }

    [Fact]
    public void Update_WhenAddressChanges_ShouldUpdateAddressLabel()
    {
        Contact c = DataService.GetRepository<ContactRepository>()
            .GetNewWithAddress("Jan", "Label", addressLine1: "Old address 1");

        c.Id = DataService.CreateTestEntity(c);
        
        var before = DataService
            .Query<Contact>()
            .Where(x => x.Id == c.Id)
            .Select(x => new Contact { Id = x.Id, Address1_Name = x.Address1_Name })
            .First();

        Assert.Equal("Old address 1", before.Address1_Name);

        Contact update = new()
        {
            Id = c.Id,
            Address1_Line1 = "New address 1",
            EntityState = EntityState.Changed
        };

        DataService.Update(update);

        var after = DataService
            .Query<Contact>()
            .Where(x => x.Id == c.Id)
            .Select(x => new Contact { Address1_Name = x.Address1_Name, Address1_Line1 = x.Address1_Line1 })
            .First();

        Assert.Equal("New address 1", after.Address1_Line1);
        Assert.NotEqual(before.Address1_Name, after.Address1_Name);
    }
}