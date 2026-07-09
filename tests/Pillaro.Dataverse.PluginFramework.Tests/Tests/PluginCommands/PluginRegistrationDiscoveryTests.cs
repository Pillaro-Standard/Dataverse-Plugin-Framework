using Pillaro.Dataverse.PluginFramework.PluginRegistrations;
using Pillaro.Dataverse.PluginFramework.Plugins;

namespace Pillaro.Dataverse.PluginFramework.Tests.Tests.PluginCommands;

public class PluginRegistrationDiscoveryTests
{
    [Fact]
    public void Discover_WhenStepHasNoSolutionMetadata_ReturnsRegistration()
    {
        var descriptor = PluginRegistrationDiscovery.Discover<SolutionlessPlugin>();

        Assert.NotNull(descriptor);

        var step = Assert.Single(descriptor.Steps);
        Assert.Equal("Create", step.MessageName);
        Assert.Equal("account", step.EntityName);
        Assert.Equal(PluginStage.Preoperation, step.Stage);
        Assert.Equal(PluginMode.Synchronous, step.Mode);
    }

    private sealed class SolutionlessPlugin(string unsecureConfig, string secureConfig)
        : PluginBase(unsecureConfig, secureConfig)
    {
        public override void Register(IPluginRegistration registration)
        {
            registration
                .OnCreate("account", "88d0ccfa-f4e9-43a9-856b-e948f3f5efba")
                .PreOperation()
                .Synchronous()
                .Rank(1);
        }
    }
}
