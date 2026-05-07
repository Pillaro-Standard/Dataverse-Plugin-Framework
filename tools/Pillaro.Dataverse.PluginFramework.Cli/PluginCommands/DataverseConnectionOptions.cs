using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal sealed class DataverseConnectionOptions
{
    public string? SdkConnectionString { get; private init; }

    public string? PacAuthProfile { get; private init; }

    public string? PacCliPath { get; private init; }

    public string? SolutionName { get; private init; }

    public bool UsesPacCli => !string.IsNullOrWhiteSpace(PacAuthProfile) || !string.IsNullOrWhiteSpace(PacCliPath);

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

    public IReadOnlyCollection<string> ValidateSdk()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(SdkConnectionString))
        {
            errors.Add("Missing --conn.");
        }

        return errors;
    }

    public IReadOnlyCollection<string> ValidatePac(bool required)
    {
        var errors = new List<string>();

        if (!required && string.IsNullOrWhiteSpace(PacAuthProfile))
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
