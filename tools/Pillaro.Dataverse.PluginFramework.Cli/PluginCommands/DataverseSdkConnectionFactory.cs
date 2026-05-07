using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal static class DataverseSdkConnectionFactory
{
    public static IOrganizationServiceAsync2 Create(DataverseConnectionOptions options)
    {
        if (options.UsesPacCli)
        {
            throw new NotSupportedException("Direct Dataverse SDK access from PAC CLI profile is not implemented. Use ConnectionString or ClientSecret for SDK-backed diff/deploy operations.");
        }

        ServiceClient.MaxConnectionTimeout = TimeSpan.FromMinutes(30);

        if (string.Equals(options.AuthType, "ConnectionString", StringComparison.OrdinalIgnoreCase))
        {
            return CreateFromConnectionString(options.ConnectionString!);
        }

        if (string.Equals(options.AuthType, "ClientSecret", StringComparison.OrdinalIgnoreCase))
        {
            return CreateFromConnectionString(BuildClientSecretConnectionString(options));
        }

        if (string.Equals(options.AuthType, "Interactive", StringComparison.OrdinalIgnoreCase))
        {
            return CreateFromConnectionString($"AuthType=OAuth;Url={options.EnvironmentUrl};RedirectUri=http://localhost;LoginPrompt=Auto");
        }

        throw new NotSupportedException($"Unsupported Dataverse auth type '{options.AuthType}'.");
    }

    private static ServiceClient CreateFromConnectionString(string connectionString)
    {
        var client = new ServiceClient(connectionString);
        if (!client.IsReady)
        {
            throw new InvalidOperationException($"Dataverse ServiceClient is not ready. {client.LastError}");
        }

        return client;
    }

    private static string BuildClientSecretConnectionString(DataverseConnectionOptions options)
    {
        return $"AuthType=ClientSecret;Url={options.EnvironmentUrl};ClientId={options.ClientId};ClientSecret={options.ClientSecret};TenantId={options.TenantId}";
    }
}
