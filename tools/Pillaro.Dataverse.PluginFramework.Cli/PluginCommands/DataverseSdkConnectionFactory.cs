using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal static class DataverseSdkConnectionFactory
{
    public static IOrganizationServiceAsync2 Create(DataverseConnectionOptions options)
    {
        ServiceClient.MaxConnectionTimeout = TimeSpan.FromMinutes(30);
        return CreateFromConnectionString(options.SdkConnectionString!);
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
}
