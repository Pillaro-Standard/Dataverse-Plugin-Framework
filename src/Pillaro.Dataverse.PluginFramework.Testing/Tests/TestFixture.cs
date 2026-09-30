using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.PowerPlatform.Dataverse.Client;
using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

namespace Pillaro.Dataverse.PluginFramework.Testing.Tests;

public class TestFixture<TAutofacModule>
    where TAutofacModule : Module, new()
{
    public IContainer Container { get; private set; }

    public TestFixture()
    {
        ContainerBuilder builder = new();

        builder.RegisterInstance(GetConfiguration())
            .As<IConfiguration>()
            .SingleInstance();

        builder.RegisterModule<TAutofacModule>();

        Container = builder.Build();

        EnsureConnectedEnvironmentMatchesExpectation(Container);
    }

    private static IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
         .SetBasePath(Directory.GetCurrentDirectory())
         .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
         .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
         .AddEnvironmentVariables()
         .Build();
    }

    /// <summary>
    /// PF-ENV-006: before integration tests run against a live Dataverse connection, verify that the
    /// connection actually points at the environment the solution declares as its test/dev environment.
    /// A single misconfigured connection string (env var, secret store, local override) must not be the
    /// only thing standing between "tests run against dev" and "tests run against production".
    /// </summary>
    /// <remarks>
    /// Opt-in: set the non-secret <c>ExpectedEnvironmentUrl</c> key (committed to the repository, unlike
    /// the connection string itself) to the environment URL tests are allowed to run against. When the key
    /// is absent, no check is performed — existing solutions are unaffected until they adopt PF-ENV-006.
    /// </remarks>
    private static void EnsureConnectedEnvironmentMatchesExpectation(IContainer container)
    {
        var configuration = container.Resolve<IConfiguration>();
        var expectedUrl = configuration["ExpectedEnvironmentUrl"];
        if (string.IsNullOrWhiteSpace(expectedUrl))
            return;

        var connectionService = container.Resolve<IDataverseConnectionService>();
        var organizationService = connectionService.GetOrganizationService();

        if (organizationService is not ServiceClient serviceClient || serviceClient.ConnectedOrgUriActual == null)
        {
            throw new InvalidOperationException(
                "PF-ENV-006: could not determine the connected Dataverse environment URL to verify it against 'ExpectedEnvironmentUrl'.");
        }

        var expectedHost = new Uri(expectedUrl).Host;
        var actualHost = serviceClient.ConnectedOrgUriActual.Host;

        if (!string.Equals(expectedHost, actualHost, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"PF-ENV-006: connected Dataverse environment '{actualHost}' does not match the expected " +
                $"environment '{expectedHost}' configured in 'ExpectedEnvironmentUrl'. Refusing to run " +
                "integration tests against an unexpected environment.");
        }
    }
}