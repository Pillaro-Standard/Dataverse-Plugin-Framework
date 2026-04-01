using Autofac;
using Pillaro.Dataverse.PluginFramework.Settings;
using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;
using Pillaro.Dataverse.PluginFramework.Testing.Tests;

namespace Pillaro.Dataverse.PluginFramework.Tests;

public class TestBase : TestBase<TestAutofacModule>
{
    protected readonly SettingsService SettingService;

    public TestBase(TestFixture<TestAutofacModule> testFixture) : base(testFixture)
    {

        SettingService = testFixture.Container.Resolve<SettingsService>();

        var handlers = testFixture.Container.Resolve<IEnumerable<ICleanupDeleteHandler>>();

        foreach (var item in handlers)
        {
            DataService.AddCleanUpDeleteHandler(item);
        }

    }
}
