using Autofac;
using Pillaro.Dataverse.PluginFramework.Testing;
using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

namespace Pillaro.Dataverse.PluginFramework.Tests;

public class TestAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule<FrameworkTestingAutofacModule>();

        builder.RegisterAssemblyTypes(typeof(TestAutofacModule).Assembly)
            .Where(t => typeof(IAutoRegisteredTestDataRepository).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .AsSelf();

        builder.RegisterAssemblyTypes(typeof(TestAutofacModule).Assembly)
            .Where(t => typeof(ICleanupDeleteHandler).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
            .AsImplementedInterfaces();
    }
}
