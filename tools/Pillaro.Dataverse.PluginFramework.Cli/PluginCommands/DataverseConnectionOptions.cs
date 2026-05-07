using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal sealed class DataverseConnectionOptions
{
    public string? SdkEnvironmentUrl { get; private init; }

    public string? SdkAuthType { get; private init; }

    public string? SdkTenantId { get; private init; }

    public string? SdkClientId { get; private init; }

    public string? SdkClientSecret { get; private init; }

    public string? SdkConnectionString { get; private init; }

    public string? PacAuthProfile { get; private init; }

    public string? PacCliPath { get; private init; }

    public string? SolutionName { get; private init; }

    public bool UsesPacCli => !string.IsNullOrWhiteSpace(PacAuthProfile) || !string.IsNullOrWhiteSpace(PacCliPath);

    public static DataverseConnectionOptions From(CommandLineOptions options)
    {
        return new DataverseConnectionOptions
        {
            SdkEnvironmentUrl = options.Get("sdk-environment") ?? options.Get("environment"),
            SdkAuthType = options.Get("sdk-auth-type") ?? NormalizeLegacyAuthType(options.Get("auth-type")) ?? "ClientSecret",
            SdkTenantId = options.Get("sdk-tenant-id") ?? options.Get("tenant-id"),
            SdkClientId = options.Get("sdk-client-id") ?? options.Get("client-id"),
            SdkClientSecret = options.Get("sdk-client-secret") ?? options.Get("client-secret"),
            SdkConnectionString = options.Get("sdk-connection-string") ?? options.Get("connection-string"),
            PacAuthProfile = options.Get("pac-auth-profile"),
            PacCliPath = options.Get("pac-cli") ?? "pac",
            SolutionName = options.Get("solution"),
        };
    }

    public IReadOnlyCollection<string> ValidateSdk()
    {
        var errors = new List<string>();

        if (string.Equals(SdkAuthType, "ConnectionString", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(SdkConnectionString))
            {
                errors.Add("Missing --sdk-connection-string for ConnectionString authentication.");
            }

            return errors;
        }

        if (string.IsNullOrWhiteSpace(SdkEnvironmentUrl))
        {
            errors.Add("Missing --sdk-environment.");
        }

        if (string.Equals(SdkAuthType, "Interactive", StringComparison.OrdinalIgnoreCase))
        {
            return errors;
        }

        if (!string.Equals(SdkAuthType, "ClientSecret", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"Unsupported --sdk-auth-type '{SdkAuthType}'. Supported values: ClientSecret, ConnectionString, Interactive.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(SdkTenantId))
        {
            errors.Add("Missing --sdk-tenant-id for ClientSecret authentication.");
        }

        if (string.IsNullOrWhiteSpace(SdkClientId))
        {
            errors.Add("Missing --sdk-client-id for ClientSecret authentication.");
        }

        if (string.IsNullOrWhiteSpace(SdkClientSecret))
        {
            errors.Add("Missing --sdk-client-secret for ClientSecret authentication.");
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

    private static string? NormalizeLegacyAuthType(string? authType)
    {
        return string.Equals(authType, "PacCli", StringComparison.OrdinalIgnoreCase)
            ? null
            : authType;
    }
}
