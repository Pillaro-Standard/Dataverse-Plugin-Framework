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

        if (string.Equals(args[0], "deploy", StringComparison.OrdinalIgnoreCase))
        {
            return await PluginDeployCommand.RunAsync(args.Skip(1).ToArray());
        }

        Console.Error.WriteLine($"Unknown command '{args[0]}'.");
        PrintRootHelp();
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
        Console.WriteLine("  pillaro-dv <command> [options]");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  deploy              Deploy Dataverse plugin assembly and synchronize plugin registration metadata.");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -h, --help          Show help.");
    }
}
