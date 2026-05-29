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

    private static int UnknownPluginCommand(string command)
    {
        Console.Error.WriteLine($"Unknown plugin command '{command}'.");
        PrintPluginHelp();
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
        Console.WriteLine();
        PrintPluginHelp();
    }

    private static void PrintPluginHelp()
    {
        Console.WriteLine("Plugin commands:");
        Console.WriteLine("  manifest   Generate plugin manifest from compiled assembly.");
        Console.WriteLine("  validate   Validate plugin manifest.");
        Console.WriteLine("  diff       Compare manifest with Dataverse.");
        Console.WriteLine("  deploy     Deploy plugin manifest to Dataverse.");
        Console.WriteLine();
        Console.WriteLine("Deploy options:");
        Console.WriteLine("  --settings <path>    Path to PillaroSettings.json. Default: PillaroSettings.json in current or parent directory.");
        Console.WriteLine("  --profile <name>     Profile to use from PillaroSettings.json profiles section. Overrides defaultProfile.");
        Console.WriteLine("  --assembly <path>    Explicit plugin assembly path. Overrides profile pluginAssemblyPath.");
        Console.WriteLine("  --solution <name>    Dataverse solution name. Overrides solution in PillaroSettings.json.");
        Console.WriteLine("  --just-assembly      Deploy only assembly without updating step/image registration.");
        Console.WriteLine("                       Useful for fast developer workflow when only code changed.");
    }
}
