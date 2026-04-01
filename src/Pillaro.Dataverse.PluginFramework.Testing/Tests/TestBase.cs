using Autofac;
using MediatR;
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
    protected readonly IMediator Mediator;
    protected readonly ILifetimeScope LifetimeScope;

    protected TestBase(TestFixture<TAutofacModule> testFixture)
    {
        LifetimeScope = testFixture.Container;
        ConnectionService = LifetimeScope.Resolve<IDataverseConnectionService>();
        OrganizationService = ConnectionService.GetOrganizationService();
        DataService = LifetimeScope.Resolve<ITestDataService>();
        Mediator = LifetimeScope.Resolve<IMediator>();
        ConnectionService.GetOrganizationService();
    }

    public void Dispose()
    {
        DataService.DeleteTestEntities();
    }
}