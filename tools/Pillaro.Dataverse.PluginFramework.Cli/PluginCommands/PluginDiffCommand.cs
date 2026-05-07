using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;
using Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.RegistrationState;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal static class PluginDiffCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var options = CommandLineOptions.Parse(args);
            var manifestPath = options.Require("manifest");
            var connectionOptions = DataverseConnectionOptions.From(options);
            var includeUnchanged = options.HasFlag("include-unchanged");

            if (!File.Exists(manifestPath))
            {
                Console.Error.WriteLine($"Manifest was not found: {manifestPath}");
                return 2;
            }

            var sdkConnectionErrors = connectionOptions.ValidateSdk();
            if (sdkConnectionErrors.Count > 0)
            {
                Console.Error.WriteLine("Dataverse SDK connection options are invalid:");
                foreach (var error in sdkConnectionErrors)
                {
                    Console.Error.WriteLine($"- {error}");
                }

                return 3;
            }

            var manifest = await PluginManifestSerializer.LoadAsync(manifestPath);
            var manifestErrors = PluginManifestValidator.Validate(manifest);
            if (manifestErrors.Count > 0)
            {
                Console.Error.WriteLine("Plugin manifest validation failed:");
                foreach (var error in manifestErrors)
                {
                    Console.Error.WriteLine($"- {error}");
                }

                return 4;
            }

            Console.WriteLine("Plugin manifest is valid.");
            Console.WriteLine($"Target environment: {GetTargetLabel(connectionOptions)}");
            Console.WriteLine($"Plugins: {manifest.Plugins.Count}");
            Console.WriteLine($"Steps: {manifest.Plugins.Sum(plugin => plugin.Steps.Count)}");
            Console.WriteLine($"Images: {manifest.Plugins.SelectMany(plugin => plugin.Steps).Sum(step => step.Images.Count)}");
            Console.WriteLine();

            var service = DataverseSdkConnectionFactory.Create(connectionOptions);
            var currentState = await DataverseRegistrationStateReader.ReadAsync(service, manifest);
            var diff = PluginRegistrationDiffCalculator.Calculate(manifest, currentState);
            PluginRegistrationDiffWriter.Write(diff, includeUnchanged);

            return diff.HasChanges ? 10 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static string GetTargetLabel(DataverseConnectionOptions options)
    {
        return options.SdkEnvironmentUrl ?? "<connection-string>";
    }
}
