using Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.EarlyBound;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal static class PluginCommandRouter
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintRootHelp();
            return 0;
        }

        if (string.Equals(args[0], "plugin", StringComparison.OrdinalIgnoreCase))
        {
            return await RunPluginCommandAsync(args);
        }

        if (string.Equals(args[0], "earlybound", StringComparison.OrdinalIgnoreCase)
            || string.Equals(args[0], "eb", StringComparison.OrdinalIgnoreCase))
        {
            return await RunEarlyBoundCommandAsync(args);
        }

        Console.Error.WriteLine($"Unknown command '{args[0]}'.");
        PrintRootHelp();
        return 2;
    }

    private static async Task<int> RunPluginCommandAsync(string[] args)
    {
        if (args.Length == 1 || IsHelp(args[1]))
        {
            PrintPluginHelp();
            return 0;
        }

        var command = args[1];
        var commandArgs = args.Skip(2).ToArray();

        return command.ToLowerInvariant() switch
        {
            "manifest" => await PluginManifestCommand.RunAsync(commandArgs),
            "validate" => await PluginValidateCommand.RunAsync(commandArgs),
            "diff" => await PluginDiffCommand.RunAsync(commandArgs),
            "deploy" => await PluginDeployCommand.RunAsync(commandArgs),
            _ => UnknownPluginCommand(command)
        };
    }

    private static async Task<int> RunEarlyBoundCommandAsync(string[] args)
    {
        if (args.Length == 1 || IsHelp(args[1]))
        {
            PrintEarlyBoundHelp();
            return 0;
        }

        var command = args[1];
        var commandArgs = args.Skip(2).ToArray();

        return command.ToLowerInvariant() switch
        {
            "generate" => await EarlyBoundGenerateCommand.RunAsync(commandArgs),
            _ => UnknownEarlyBoundCommand(command)
        };
    }

    private static int UnknownPluginCommand(string command)
    {
        Console.Error.WriteLine($"Unknown plugin command '{command}'.");
        PrintPluginHelp();
        return 2;
    }

    private static int UnknownEarlyBoundCommand(string command)
    {
        Console.Error.WriteLine($"Unknown early-bound command '{command}'.");
        PrintEarlyBoundHelp();
        return 2;
    }

    private static bool IsHelp(string value)
    {
        return value is "-h" or "--help" or "/?";
    }

    private static void PrintRootHelp()
    {
        Console.WriteLine("Pillaro Dataverse CLI");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  pillaro-dv plugin <command> [options]");
        Console.WriteLine("  pillaro-dv earlybound <command> [options]");
        Console.WriteLine("  pillaro-dv eb <command> [options]");
        Console.WriteLine();
        PrintPluginHelp();
        Console.WriteLine();
        PrintEarlyBoundHelp();
    }

    private static void PrintPluginHelp()
    {
        Console.WriteLine("Plugin commands:");
        Console.WriteLine("  manifest   Generate plugin manifest from compiled assembly.");
        Console.WriteLine("  validate   Validate plugin manifest.");
        Console.WriteLine("  diff       Compare manifest with Dataverse.");
        Console.WriteLine("  deploy     Deploy plugin manifest to Dataverse.");
    }

    private static void PrintEarlyBoundHelp()
    {
        Console.WriteLine("Early-bound commands:");
        Console.WriteLine("  generate   Generate early-bound classes through PAC modelbuilder.");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  pillaro-dv eb generate --conn %DV_CONN%");
        Console.WriteLine("  pillaro-dv eb generate --settings PillaroSettings.json --conn %DV_CONN%");
    }
}
