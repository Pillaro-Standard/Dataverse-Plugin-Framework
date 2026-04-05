using Autofac;
using Pillaro.Dataverse.PluginFramework.Testing;
using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

namespace Pillaro.Dataverse.PluginFramework.Examples.Tests;

public class TestAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterModule<FrameworkTestingAutofacModule>();

        builder.RegisterAssemblyTypes(GetType().Assembly)
            .Where(t => t.GetInterfaces().Any(i => i.IsAssignableFrom(typeof(IAutoRegisteredTestDataRepository))))
            .AsSelf();
    }
}
