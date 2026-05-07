using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal sealed class DataverseConnectionOptions
{
    public string? EnvironmentUrl { get; private init; }

    public string? AuthType { get; private init; }

    public string? TenantId { get; private init; }

    public string? ClientId { get; private init; }

    public string? ClientSecret { get; private init; }

    public string? ConnectionString { get; private init; }

    public string? PacAuthProfile { get; private init; }

    public string? PacCliPath { get; private init; }

    public string? SolutionName { get; private init; }

    public bool UsesPacCli => string.Equals(AuthType, "PacCli", StringComparison.OrdinalIgnoreCase);

    public static DataverseConnectionOptions From(CommandLineOptions options)
    {
        return new DataverseConnectionOptions
        {
            EnvironmentUrl = options.Get("environment"),
            AuthType = options.Get("auth-type") ?? "ClientSecret",
            TenantId = options.Get("tenant-id"),
            ClientId = options.Get("client-id"),
            ClientSecret = options.Get("client-secret"),
            ConnectionString = options.Get("connection-string"),
            PacAuthProfile = options.Get("pac-auth-profile"),
            PacCliPath = options.Get("pac-cli") ?? "pac",
            SolutionName = options.Get("solution"),
        };
    }

    public IReadOnlyCollection<string> Validate()
    {
        var errors = new List<string>();

        if (string.Equals(AuthType, "ConnectionString", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(ConnectionString))
            {
                errors.Add("Missing --connection-string for ConnectionString authentication.");
            }

            return errors;
        }

        if (string.Equals(AuthType, "PacCli", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(PacCliPath))
            {
                errors.Add("Missing --pac-cli for PacCli authentication.");
            }

            return errors;
        }

        if (string.IsNullOrWhiteSpace(EnvironmentUrl))
        {
            errors.Add("Missing --environment.");
        }

        if (string.Equals(AuthType, "Interactive", StringComparison.OrdinalIgnoreCase))
        {
            return errors;
        }

        if (!string.Equals(AuthType, "ClientSecret", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Unsupported --auth-type '{AuthType}'. Supported values: ClientSecret, ConnectionString, Interactive, PacCli.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(TenantId))
        {
            errors.Add("Missing --tenant-id for ClientSecret authentication.");
        }

        if (string.IsNullOrWhiteSpace(ClientId))
        {
            errors.Add("Missing --client-id for ClientSecret authentication.");
        }

        if (string.IsNullOrWhiteSpace(ClientSecret))
        {
            errors.Add("Missing --client-secret for ClientSecret authentication.");
        }

        return errors;
    }
}
