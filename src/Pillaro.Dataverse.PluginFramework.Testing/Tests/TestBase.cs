using Autofac;
using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;
using Xunit;

namespace Pillaro.Dataverse.PluginFramework.Testing.Tests;

public abstract class TestBase<TAutofacModule> : IClassFixture<TestFixture<TAutofacModule>>, IDisposable
    where TAutofacModule : Module, new()
{
    protected readonly IDataverseConnectionService ConnectionService;
    protected readonly IOrganizationService OrganizationService;
    protected readonly ITestDataService DataService;
    protected readonly ILifetimeScope LifetimeScope;
    protected readonly ITestOutputHelper Output;

    protected TestBase(TestFixture<TAutofacModule> testFixture, ITestOutputHelper output)
    {
        LifetimeScope = testFixture.Container;
        ConnectionService = LifetimeScope.Resolve<IDataverseConnectionService>();
        OrganizationService = ConnectionService.GetOrganizationService();
        DataService = LifetimeScope.Resolve<ITestDataService>();
        DataService.SetOutput(output);
        ConnectionService.GetOrganizationService();
        Output = output;
    }

    public void Dispose()
    {
        DataService.DeleteTestEntities();
    }
}