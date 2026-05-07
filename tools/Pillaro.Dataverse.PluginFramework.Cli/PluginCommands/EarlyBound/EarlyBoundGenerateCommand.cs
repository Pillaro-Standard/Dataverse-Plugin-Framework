using Pillaro.Dataverse.PluginFramework.Cli.Configuration;
using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.EarlyBound;

internal static class EarlyBoundGenerateCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var options = CommandLineOptions.Parse(args);
            var settings = await PillaroSettingsLoader.LoadAsync(options);
            var connectionOptions = DataverseConnectionOptions.From(options);

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

            if (string.IsNullOrWhiteSpace(settings.EarlyBound.Namespace))
            {
                Console.Error.WriteLine("Missing earlyBound.namespace in PillaroSettings.json.");
                return 3;
            }

            var entityNames = await ResolveEntityNamesAsync(settings, connectionOptions);
            if (entityNames.Count == 0)
            {
                Console.Error.WriteLine("No entities were configured or resolved for early-bound generation.");
                return 4;
            }

            var modelBuilderSettingsPath = await PillaroSettingsLoader.WritePacModelBuilderSettingsAsync(settings, entityNames);
            var pacCli = connectionOptions.PacCliPath ?? "pac";
            var outDirectory = Path.GetFullPath(settings.EarlyBound.Out);
            Directory.CreateDirectory(outDirectory);

            var arguments = $"modelbuilder build --outdirectory \"{outDirectory}\" --settingsTemplateFile \"{modelBuilderSettingsPath}\"";
            Console.WriteLine("Running PAC modelbuilder...");
            Console.WriteLine($"{pacCli} {arguments}");
            Console.WriteLine($"Entities: {string.Join(", ", entityNames)}");

            var result = await ProcessRunner.RunAsync(pacCli, arguments);
            if (!result.Succeeded)
            {
                Console.Error.WriteLine("PAC modelbuilder failed.");
                WriteOutput(result, error: true);
                return 40;
            }

            WriteOutput(result, error: false);
            Console.WriteLine("Early-bound generation completed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static async Task<IReadOnlyCollection<string>> ResolveEntityNamesAsync(PillaroSettings settings, DataverseConnectionOptions connectionOptions)
    {
        var names = new HashSet<string>(settings.EarlyBound.Entities.Where(entity => !string.IsNullOrWhiteSpace(entity)), StringComparer.OrdinalIgnoreCase);

        if (settings.EarlyBound.IncludeSolutionEntities)
        {
            var service = DataverseSdkConnectionFactory.Create(connectionOptions);
            var solutionEntityNames = await EarlyBoundSolutionEntityReader.ReadEntityLogicalNamesAsync(service, settings.Solution);
            foreach (var entityName in solutionEntityNames)
            {
                names.Add(entityName);
            }
        }

        return names.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static void WriteOutput(ProcessResult result, bool error)
    {
        if (!string.IsNullOrWhiteSpace(result.Output))
        {
            if (error)
            {
                Console.Error.WriteLine(result.Output.Trim());
            }
            else
            {
                Console.WriteLine(result.Output.Trim());
            }
        }

        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            Console.Error.WriteLine(result.Error.Trim());
        }
    }
}
