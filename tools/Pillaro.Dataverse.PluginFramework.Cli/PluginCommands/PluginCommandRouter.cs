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

        if (!string.Equals(args[0], "plugin", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Unknown command '{args[0]}'.");
            PrintRootHelp();
            return 2;
        }

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
        Console.WriteLine("  diff       Compare manifest with Dataverse. Initial command skeleton.");
        Console.WriteLine("  deploy     Deploy plugin manifest to Dataverse. Initial command skeleton.");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  pillaro-dv plugin manifest --assembly ./bin/Debug/net462/Contoso.Plugins.dll --output ./artifacts/plugin-manifest.json");
        Console.WriteLine("  pillaro-dv plugin validate --manifest ./artifacts/plugin-manifest.json");
    }
}
