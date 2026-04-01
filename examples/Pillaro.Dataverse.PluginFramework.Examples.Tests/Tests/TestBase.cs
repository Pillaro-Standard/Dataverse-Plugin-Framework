using Autofac;
using Pillaro.Dataverse.PluginFramework.Settings;
using Pillaro.Dataverse.PluginFramework.Testing.Tests;

namespace Pillaro.Dataverse.PluginFramework.Examples.Tests.Tests;

public class TestBase : TestBase<TestAutofacModule>
{
    protected readonly SettingsService SettingService;

    public TestBase(TestFixture<TestAutofacModule> testFixture) : base(testFixture)
    {
        SettingService = testFixture.Container.Resolve<SettingsService>();
    }
}
