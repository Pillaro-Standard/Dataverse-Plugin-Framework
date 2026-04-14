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
