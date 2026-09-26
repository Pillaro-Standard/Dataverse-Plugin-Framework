using System.Reflection;
using Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;
using Pillaro.Dataverse.PluginFramework.PluginRegistrations;
using Pillaro.Dataverse.PluginFramework.Plugins;

namespace Pillaro.Dataverse.PluginFramework.Tests.Tests.PluginCommands;

public class PluginCommandRouterTests
{
    [Fact]
    public async Task RunAsync_Manifest_GeneratesManifestWithoutConnection()
    {
        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.plugin-manifest.json");

        try
        {
            var exitCode = await PluginCommandRouter.RunAsync(["manifest", "--assembly", assemblyPath, "--output", outputPath]);

            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(outputPath));
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public async Task RunAsync_Validate_ValidatesGeneratedManifestWithoutConnection()
    {
        var assemblyPath = Assembly.GetExecutingAssembly().Location;
        var manifestPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.plugin-manifest.json");

        try
        {
            var manifestExitCode = await PluginCommandRouter.RunAsync(["manifest", "--assembly", assemblyPath, "--output", manifestPath]);
            Assert.Equal(0, manifestExitCode);

            var validateExitCode = await PluginCommandRouter.RunAsync(["validate", "--manifest", manifestPath]);

            Assert.Equal(0, validateExitCode);
        }
        finally
        {
            if (File.Exists(manifestPath))
                File.Delete(manifestPath);
        }
    }

    [Fact]
    public async Task RunAsync_Validate_MissingManifestFile_ReturnsNonZeroExitCode()
    {
        var missingManifestPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.plugin-manifest.json");

        var exitCode = await PluginCommandRouter.RunAsync(["validate", "--manifest", missingManifestPath]);

        Assert.NotEqual(0, exitCode);
    }

    [Fact]
    public async Task RunAsync_UnknownCommand_ReturnsExitCode2()
    {
        var exitCode = await PluginCommandRouter.RunAsync(["frobnicate"]);

        Assert.Equal(2, exitCode);
    }

    private sealed class RouterTestPlugin(string unsecureConfig, string secureConfig)
        : PluginBase(unsecureConfig, secureConfig)
    {
        public override void Register(IPluginRegistration registration)
        {
            registration
                .OnCreate("account", "5b2f6b0a-2f7f-4b3a-9b7a-5c7b6f6b1a10")
                .PreOperation()
                .Synchronous()
                .Rank(1);
        }
    }
}
