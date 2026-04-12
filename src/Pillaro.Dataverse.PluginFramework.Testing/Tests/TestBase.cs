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
    protected readonly ITestDataService TestDataService;
    protected readonly ILifetimeScope LifetimeScope;
    protected readonly ITestOutputHelper Output;

    protected TestBase(TestFixture<TAutofacModule> testFixture, ITestOutputHelper output)
    {
        Output = output;
        LifetimeScope = testFixture.Container;

        TestDataService = LifetimeScope.Resolve<ITestDataService>();
        TestDataService.SetOutput(output);

        ConnectionService = LifetimeScope.Resolve<IDataverseConnectionService>();
        OrganizationService = ConnectionService.GetOrganizationService();
    }

    public void Dispose()
    {
        TestDataService.DeleteTestEntities();
    }
}