using Autofac;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Settings;
using Pillaro.Dataverse.PluginFramework.Testing.Application.Handlers;
using Pillaro.Dataverse.PluginFramework.Testing.Domain.Interfaces;
using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

namespace Pillaro.Dataverse.PluginFramework.Testing;

public class FrameworkTestingAutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
        builder.RegisterAssemblyTypes(ThisAssembly)
            .Where(t => typeof(IAutoRegisteredService).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
            .AsImplementedInterfaces();

        builder.RegisterType<WaitOnAsyncProcessHandler>().AsSelf();
        builder.RegisterType<GetAsyncProcessesHandler>().AsSelf();

        builder.Register(c => new MemoryCache(Options.Create(new MemoryCacheOptions())))
            .As<IMemoryCache>()
            .SingleInstance();

        builder.Register(c => c.Resolve<IDataverseConnectionService>().GetOrganizationService())
            .As<IOrganizationService>()
            .InstancePerLifetimeScope();


        builder.RegisterType<SettingsService>().AsSelf();

    }
}