using System.Reflection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal static class PluginAssemblyResolver
{
    public static async Task<Guid> ResolvePluginAssemblyIdAsync(IOrganizationServiceAsync2 service, string assemblyPath)
    {
        var assemblyName = AssemblyName.GetAssemblyName(assemblyPath).Name;
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            throw new InvalidOperationException($"Could not resolve assembly name from '{assemblyPath}'.");
        }

        var query = new QueryExpression("pluginassembly")
        {
            ColumnSet = new ColumnSet("pluginassemblyid", "name")
        };
        query.Criteria.AddCondition("name", ConditionOperator.Equal, assemblyName);

        var response = await service.RetrieveMultipleAsync(query);
        if (response.Entities.Count == 0)
        {
            throw new InvalidOperationException($"Plugin assembly '{assemblyName}' was not found in Dataverse. Register the assembly once before running automated deploy.");
        }

        if (response.Entities.Count > 1)
        {
            throw new InvalidOperationException($"Plugin assembly name '{assemblyName}' is not unique in Dataverse. Found {response.Entities.Count} records.");
        }

        return response.Entities[0].Id;
    }
}
