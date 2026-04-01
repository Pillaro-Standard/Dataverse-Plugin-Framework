using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Examples.Logic;
using Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Contact;
using Pillaro.Dataverse.PluginFramework.Examples.Tests;
using Pillaro.Dataverse.PluginFramework.Examples.Tests.Tests;
using Pillaro.Dataverse.PluginFramework.Testing.Tests;

namespace Pillaro.Dataverse.PluginFramework.Examples.Tests.Tests.Contacts;

[Trait("Owner", "JM")]
[Trait("Category", nameof(ScoreOnAddressChange))]
public class ScoreOnAddressChangeTest(TestFixture<TestAutofacModule> testFixture) : TestBase(testFixture)
{
    [Fact]
    public void Create_WithAddress_ShouldSetScore()
    {
        Contact c = new()
        {
            FirstName = "Jan",
            LastName = "Score",
            Address1_Line1 = "Main street 1",
            Address1_City = "Prague",
            Address1_PostalCode = "11000",
            Address1_Country = "CZ"
        };

        c.Id = DataService.CreateTestEntity(c);

        var loaded = DataService
            .Query<Contact>()
            .Where(x => x.Id == c.Id)
            .Select(x => new Contact { NumberOfChildren = x.NumberOfChildren })
            .First();

        Assert.True(loaded.NumberOfChildren.HasValue, "Score (NumberOfChildren) must be set on create when address is provided.");
        Assert.InRange(loaded.NumberOfChildren.Value, 0, 100);
    }

    [Fact]
    public void Update_WhenAddressChanges_ShouldChangeScore()
    {
        Contact c = new()
        {
            FirstName = "Jan",
            LastName = "Score",
            Address1_Line1 = "Old address 1"
        };

        c.Id = DataService.CreateTestEntity(c);

        var before = DataService
            .Query<Contact>()
            .Where(x => x.Id == c.Id)
            .Select(x => new Contact { Id = x.Id, NumberOfChildren = x.NumberOfChildren })
            .First();

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
            .Select(x => new Contact { NumberOfChildren = x.NumberOfChildren })
            .First();

        Assert.True(after.NumberOfChildren.HasValue, "Score must be set after address change.");
        Assert.NotEqual(before.NumberOfChildren, after.NumberOfChildren);
    }
}