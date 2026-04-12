using Autofac;
using Autofac.Core.Lifetime;
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

        // Example: Register a TestDataService instance for a specific test user (identified by CallerId).
        // This allows tests to run under different user identities (impersonation).
        // The key ("sales" in this case) represents a predefined test user.
        //
        // builder.Register(ctx =>
        // {
        //     var scope = ctx.Resolve<ILifetimeScope>();
        //     var orgFactory = ctx.Resolve<IOrganizationServiceFactory>();
        //
        //     // Create OrganizationService for a specific user (CallerId).
        //     // Replace Guid.Empty with the actual user Id.
        //     var service = orgFactory.CreateOrganizationService(Guid.Empty);
        //
        //     // Wrap OrganizationService into TestDataService.
        //     return (ITestDataService)new TestDataService(service, scope);
        // })
        // .Keyed<ITestDataService>("sales");


        // Example: Resolve TestDataService for a specific predefined user.
        // The key must match the one used during registration.
        //
        // var salesService = _lifetimeScope.ResolveKeyed<ITestDataService>("sales");
    }
}