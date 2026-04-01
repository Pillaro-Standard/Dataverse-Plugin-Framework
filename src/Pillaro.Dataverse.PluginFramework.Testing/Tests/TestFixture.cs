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
        builder.RegisterModule<TAutofacModule>();
        builder.RegisterInstance(GetConfiguration()).As<IConfiguration>();
        Container = builder.Build();
    }

    private IConfiguration GetConfiguration()
    {
        return new ConfigurationBuilder()
         .SetBasePath(Directory.GetCurrentDirectory())
         .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
         .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
         .Build();
    }
}