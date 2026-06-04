using Microsoft.Crm.Sdk.Messages;
using Pillaro.Dataverse.PluginFramework.Testing.Tests;

namespace Pillaro.Dataverse.PluginTemplate.Tests.Tests;

public class ExampleTests : TestBase
{
    public ExampleTests(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output)
        : base(testFixture, output)
    {
    }

    [Fact]
    public void Template_Should_Be_Ready_For_Project_Tests()
    {
        var res = (WhoAmIResponse)OrganizationService.Execute(new WhoAmIRequest());

        Assert.NotEqual(Guid.Empty, res.UserId);
    }
}
