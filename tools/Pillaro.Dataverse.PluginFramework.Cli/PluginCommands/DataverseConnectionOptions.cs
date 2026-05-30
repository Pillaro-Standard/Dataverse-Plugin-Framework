using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal sealed class DataverseConnectionOptions
{
    public string? SdkConnectionString { get; private init; }

    public static DataverseConnectionOptions FromSdkConnectionString(string connectionString)
    {
        return new DataverseConnectionOptions
        {
            SdkConnectionString = connectionString,
        };
    }

    public static DataverseConnectionOptions From(CommandLineOptions options)
    {
        return new DataverseConnectionOptions
        {
            SdkConnectionString = options.Get("conn") ?? options.Get("sdk-connection-string") ?? options.Get("connection-string"),
        };
    }

    public IReadOnlyCollection<string> ValidateSdk()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(SdkConnectionString))
        {
            errors.Add("Missing Dataverse connection string. Use --conn, --sdk-connection-string, or --connection-string.");
        }

        return errors;
    }
}
