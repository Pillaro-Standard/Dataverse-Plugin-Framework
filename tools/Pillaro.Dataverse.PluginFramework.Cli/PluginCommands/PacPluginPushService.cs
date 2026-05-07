using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal static class PacPluginPushService
{
    public static async Task<int> PushAsync(
        DataverseConnectionOptions connectionOptions,
        PacPluginPushOptions pushOptions,
        string pluginFile)
    {
        if (pushOptions.SkipPacPush)
        {
            Console.WriteLine("PAC plugin push skipped by --skip-pac-push.");
            return 0;
        }

        var arguments = $"plugin push --pluginId \"{pushOptions.PluginId}\" --pluginFile \"{Path.GetFullPath(pluginFile)}\" --type \"{pushOptions.PluginType}\"";

        Console.WriteLine("Running PAC plugin push...");
        Console.WriteLine($"pac {arguments}");

        var result = await ProcessRunner.RunAsync(connectionOptions.PacCliPath ?? "pac", arguments);
        if (!result.Succeeded)
        {
            Console.Error.WriteLine("PAC plugin push failed.");
            WritePacOutput(result);
            return 40;
        }

        WritePacOutput(result);
        return 0;
    }

    private static void WritePacOutput(ProcessResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.Output))
        {
            Console.WriteLine(result.Output.Trim());
        }

        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            Console.Error.WriteLine(result.Error.Trim());
        }
    }
}
