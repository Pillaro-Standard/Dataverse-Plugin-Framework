using System.Reflection;
using Pillaro.Dataverse.PluginFramework.Cli.Configuration;
using Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

namespace Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

internal static class CliLog
{
    public static void WriteHeader(
        string commandName,
        CommandLineOptions options,
        PillaroSettings? settings = null,
        DataverseConnectionOptions? connectionOptions = null)
    {
        var assembly = Assembly.GetExecutingAssembly().GetName();
        var version = assembly.Version?.ToString() ?? "dev";
        var settingsPath = PillaroSettingsLoader.GetResolvedSettingsPath(options) ?? "<not found>";
        var profilesPath = PillaroSettingsLoader.GetProfilesPath(options);

        Console.WriteLine($"Pillaro Dataverse CLI {version}");
        Console.WriteLine($"Command: {commandName}");
        Console.WriteLine($"Working directory: {Directory.GetCurrentDirectory()}");
        Console.WriteLine($"Settings: {settingsPath}");
        Console.WriteLine($"Profiles: {profilesPath}");

        if (settings != null)
        {
            Console.WriteLine($"Profile: {settings.Profile}");
            Console.WriteLine($"Solution: {settings.Solution}");
        }

        if (connectionOptions != null)
        {
            Console.WriteLine($"PAC profile: {connectionOptions.PacAuthProfile ?? "<not specified>"}");
            Console.WriteLine($"SDK connection: {(string.IsNullOrWhiteSpace(connectionOptions.SdkConnectionString) ? "<not specified>" : "<provided>")}");
        }

        Console.WriteLine();
    }
}
