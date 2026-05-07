using System.Reflection;
using System.Text.Json;
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
        var hasConnectionString = !string.IsNullOrWhiteSpace(connectionOptions?.SdkConnectionString);

        Console.WriteLine($"Pillaro Dataverse CLI {version}");
        Console.WriteLine($"Command: {commandName}");
        Console.WriteLine($"Working directory: {Directory.GetCurrentDirectory()}");
        Console.WriteLine($"Settings: {settingsPath}");
        WriteSettingsDiagnostics(settingsPath);
        Console.WriteLine(hasConnectionString
            ? "Profiles: <not used because SDK connection string was provided>"
            : $"Profiles: {profilesPath}");

        if (settings != null)
        {
            Console.WriteLine($"Profile: {settings.Profile}");
            Console.WriteLine($"Solution: {settings.Solution}");
            Console.WriteLine($"Plugins assembly: {settings.Plugins.Assembly}");
            Console.WriteLine($"Early-bound solution: {settings.EarlyBound.Solution ?? "<not specified>"}");
        }

        if (connectionOptions != null)
        {
            Console.WriteLine($"PAC profile: {connectionOptions.PacAuthProfile ?? "<not specified>"}");
            Console.WriteLine($"SDK connection: {(hasConnectionString ? "<provided>" : "<not specified>")}");
        }

        Console.WriteLine();
    }

    private static void WriteSettingsDiagnostics(string settingsPath)
    {
        if (string.Equals(settingsPath, "<not found>", StringComparison.OrdinalIgnoreCase) || !File.Exists(settingsPath))
        {
            return;
        }

        var info = new FileInfo(settingsPath);
        Console.WriteLine($"Settings last write: {info.LastWriteTime:yyyy-MM-dd HH:mm:ss}");

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(settingsPath), new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });

            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                var keys = document.RootElement.EnumerateObject().Select(property => property.Name).ToArray();
                Console.WriteLine($"Settings root keys: {string.Join(", ", keys)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Settings diagnostics failed: {ex.Message}");
        }
    }
}
