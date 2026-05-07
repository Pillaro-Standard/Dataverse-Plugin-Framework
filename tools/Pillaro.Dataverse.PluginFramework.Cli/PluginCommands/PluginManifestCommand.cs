using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal static class PluginManifestCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var options = CommandLineOptions.Parse(args);
            var assemblyPath = options.Require("assembly");
            var outputPath = options.Get("output") ?? "artifacts/plugin-manifest.json";

            var manifest = PluginManifestFactory.CreateFromAssembly(assemblyPath);
            var errors = PluginManifestValidator.Validate(manifest);
            if (errors.Count > 0)
            {
                Console.Error.WriteLine("Plugin manifest was generated but contains validation errors:");
                foreach (var error in errors)
                {
                    Console.Error.WriteLine($"- {error}");
                }

                return 3;
            }

            await PluginManifestSerializer.SaveAsync(manifest, outputPath);

            Console.WriteLine($"Plugin manifest generated: {outputPath}");
            Console.WriteLine($"Plugins: {manifest.Plugins.Count}");
            Console.WriteLine($"Steps: {manifest.Plugins.Sum(plugin => plugin.Steps.Count)}");
            Console.WriteLine($"Images: {manifest.Plugins.SelectMany(plugin => plugin.Steps).Sum(step => step.Images.Count)}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}
