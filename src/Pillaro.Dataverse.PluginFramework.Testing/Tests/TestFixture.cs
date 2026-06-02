using Autofac;
using Microsoft.Extensions.Configuration;

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
}