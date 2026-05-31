using Autofac;
using Pillaro.Dataverse.PluginFramework.Settings;
using Pillaro.Dataverse.PluginFramework.Testing.Tests;
using System.Net;

namespace Pillaro.Dataverse.PluginFramework.Examples.Tests.Tests;

public class TestBase : TestBase<TestAutofacModule>
{
    protected readonly SettingsService SettingService;

    public TestBase(TestFixture<TestAutofacModule> testFixture, ITestOutputHelper output) : base(testFixture, output)
    {
        SettingService = testFixture.Container.Resolve<SettingsService>();
    }
}
