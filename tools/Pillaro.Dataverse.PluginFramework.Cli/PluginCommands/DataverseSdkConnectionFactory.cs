using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal static class DataverseSdkConnectionFactory
{
    public static IOrganizationServiceAsync2 Create(DataverseConnectionOptions options)
    {
        ServiceClient.MaxConnectionTimeout = TimeSpan.FromMinutes(30);

        if (string.Equals(options.SdkAuthType, "ConnectionString", StringComparison.OrdinalIgnoreCase))
        {
            return CreateFromConnectionString(options.SdkConnectionString!);
        }

        if (string.Equals(options.SdkAuthType, "ClientSecret", StringComparison.OrdinalIgnoreCase))
        {
            return CreateFromConnectionString(BuildClientSecretConnectionString(options));
        }

        if (string.Equals(options.SdkAuthType, "Interactive", StringComparison.OrdinalIgnoreCase))
        {
            return CreateFromConnectionString($"AuthType=OAuth;Url={options.SdkEnvironmentUrl};RedirectUri=http://localhost;LoginPrompt=Auto");
        }

        throw new NotSupportedException($"Unsupported Dataverse SDK auth type '{options.SdkAuthType}'.");
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
        return $"AuthType=ClientSecret;Url={options.SdkEnvironmentUrl};ClientId={options.SdkClientId};ClientSecret={options.SdkClientSecret};TenantId={options.SdkTenantId}";
    }
}
