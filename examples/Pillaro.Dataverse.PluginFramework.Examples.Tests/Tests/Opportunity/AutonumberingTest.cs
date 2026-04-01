using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Opportunity;
using Pillaro.Dataverse.PluginFramework.Testing.Tests;

namespace Pillaro.Dataverse.PluginFramework.Examples.Tests.Tests.Opportunity;

[Trait("Owner", "JM")]
[Trait("Category", nameof(Autonumbering))]
public class AutonumberingTests(TestFixture<TestAutofacModule> testFixture) : TestBase(testFixture)
{
    [Fact]
    public void AutonumberingCreateTest()
    {
        var contact = new Logic.Opportunity
        {
            Name = "Test opportunity",
        };

        contact.Id = DataService.CreateTestEntity(contact);

        var cFc = DataService
          .Query<Logic.Opportunity>()
          .Where(o => o.Id == contact.Id)
          .Select(s => new Logic.Opportunity { Name = s.Name })
          .First();


        Assert.True(cFc.Name.Contains(':'), $"Name does not contains ':'. Name: '{cFc.Name}'");
    }

    [Fact]
    public void AutonumberingUpdateTest()
    {
        var contact = new Logic.Opportunity
        {
            Name = "",
        };

        contact.Id = DataService.CreateTestEntity(contact);

        var cFc = DataService
          .Query<Logic.Opportunity>()
          .Where(o => o.Id == contact.Id)
          .Select(s => new Logic.Opportunity { Id = s.Id, Name = s.Name })
          .First();

        var number = cFc.Name;
        cFc.EntityState = EntityState.Changed;
        cFc.Description = "test";

        DataService.Update(cFc);

        var cFc2 = DataService
          .Query<Logic.Opportunity>()
          .Where(o => o.Id == contact.Id)
          .Select(s => new Logic.Opportunity { Id = s.Id, Name = s.Name })
          .First();

        Assert.True(number.Equals(cFc2.Name, StringComparison.InvariantCulture));
    }
}
