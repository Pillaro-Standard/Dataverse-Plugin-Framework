using Microsoft.Crm.Sdk.Messages;
using Pillaro.Dataverse.PluginFramework.Testing.Tests;

namespace $testprojectname$.Tests;

public class ConnectionTests : TestBase
{
    public ConnectionTests(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output)
        : base(testFixture, output)
    {
    }

    [Fact]
    public void Connect_Should_Return_Valid_UserId()
    {
        var res = (WhoAmIResponse)OrganizationService.Execute(new WhoAmIRequest());

        Assert.NotEqual(Guid.Empty, res.UserId);
    }
}
