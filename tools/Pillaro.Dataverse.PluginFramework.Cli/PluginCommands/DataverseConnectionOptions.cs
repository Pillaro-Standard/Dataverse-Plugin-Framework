using Pillaro.Dataverse.PluginFramework.Cli.Configuration;
using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal sealed class DataverseConnectionOptions
{
    public string? SdkConnectionString { get; private init; }

    public string? PacAuthProfile { get; private init; }

    public string? PacCliPath { get; private init; }

    public string? SolutionName { get; private init; }

    public bool UsesPacCli => !string.IsNullOrWhiteSpace(PacAuthProfile);

    public static DataverseConnectionOptions From(CommandLineOptions options)
    {
        return new DataverseConnectionOptions
        {
            SdkConnectionString = options.Get("conn") ?? options.Get("sdk-connection-string") ?? options.Get("connection-string"),
            PacAuthProfile = options.Get("pac-profile") ?? options.Get("pac-auth-profile"),
            PacCliPath = options.Get("pac-cli") ?? "pac",
            SolutionName = options.Get("solution"),
        };
    }

    public static async Task<DataverseConnectionOptions> ResolveAsync(CommandLineOptions options, PillaroSettings? settings = null)
    {
        var profile = await PillaroSettingsLoader.TryLoadProfileAsync(options, settings);

        return new DataverseConnectionOptions
        {
            SdkConnectionString = options.Get("conn")
                ?? options.Get("sdk-connection-string")
                ?? options.Get("connection-string")
                ?? profile?.ConnectionString,
            PacAuthProfile = options.Get("pac-profile")
                ?? options.Get("pac-auth-profile")
                ?? profile?.PacProfile,
            PacCliPath = options.Get("pac-cli") ?? "pac",
            SolutionName = options.Get("solution"),
        };
    }

    public IReadOnlyCollection<string> ValidateSdk()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(SdkConnectionString))
        {
            errors.Add("Missing Dataverse connection string. Use --conn or configure a profile in %USERPROFILE%\\.pillaro\\dataverse-profiles.json.");
        }

        return errors;
    }

    public IReadOnlyCollection<string> ValidatePac(bool required)
    {
        var errors = new List<string>();

        if (!required)
        {
            return errors;
        }

        if (string.IsNullOrWhiteSpace(PacCliPath))
        {
            errors.Add("Missing --pac-cli.");
        }

        return errors;
    }
}
