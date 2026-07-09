using System.Reflection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.RegistrationState;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal static class DataversePluginAssemblyDeployer
{
    private const int SourceTypeDatabase = 0;
    private const int IsolationModeSandbox = 2;

    public static async Task<PluginAssemblyDeploymentResult> DeployAsync(
        IOrganizationServiceAsync2 service,
        string assemblyPath,
        PluginManifestDocument manifest,
        string solutionName)
    {
        if (string.IsNullOrWhiteSpace(assemblyPath))
        {
            throw new ArgumentException("Assembly path is required.", nameof(assemblyPath));
        }

        if (manifest.Plugins.Count == 0)
        {
            throw new InvalidOperationException("Plugin manifest does not contain any plugin types to deploy.");
        }

        var fullAssemblyPath = Path.GetFullPath(assemblyPath);
        var assemblyBytes = await File.ReadAllBytesAsync(fullAssemblyPath);
        var assemblyName = AssemblyName.GetAssemblyName(fullAssemblyPath);
        if (string.IsNullOrWhiteSpace(assemblyName.Name))
        {
            throw new InvalidOperationException($"Could not resolve assembly name from '{fullAssemblyPath}'.");
        }

        var assemblyRecord = await FindPluginAssemblyAsync(service, assemblyName.Name);
        var assemblyId = assemblyRecord?.Id ?? Guid.Empty;

        var pluginAssembly = CreatePluginAssemblyEntity(assemblyName, assemblyBytes, assemblyId);
        if (assemblyRecord == null)
        {
            assemblyId = await service.CreateAsync(pluginAssembly);
        }
        else
        {
            await service.UpdateAsync(pluginAssembly);
        }

        var result = new PluginAssemblyDeploymentResult();

        result.SolutionComponents.Add(new SolutionComponentDeploymentResult
        {
            ComponentName = assemblyName.Name,
            ComponentType = "pluginassembly",
            Action = await DataverseSolutionComponentService.EnsureAddedWithSubcomponentRetryAsync(
                service,
                solutionName,
                DataverseSolutionComponentTypes.PluginAssembly,
                assemblyId),
        });

        foreach (var plugin in manifest.Plugins.OrderBy(plugin => plugin.TypeName, StringComparer.OrdinalIgnoreCase))
        {
            var typeRecord = await FindPluginTypeAsync(service, assemblyId, plugin.TypeName);
            var pluginTypeId = typeRecord?.Id ?? Guid.Empty;

            if (typeRecord == null)
            {
                var pluginType = CreatePluginTypeEntity(assemblyId, plugin.TypeName, pluginTypeId);
                pluginTypeId = await service.CreateAsync(pluginType);
            }

            result.PluginTypeIdsByName[plugin.TypeName] = pluginTypeId;
        }

        return result;
    }

    private static Entity CreatePluginAssemblyEntity(AssemblyName assemblyName, byte[] assemblyBytes, Guid id)
    {
        var entity = new Entity("pluginassembly") { Id = id };
        entity["name"] = assemblyName.Name;
        entity["content"] = Convert.ToBase64String(assemblyBytes);
        entity["isolationmode"] = new OptionSetValue(IsolationModeSandbox);
        entity["sourcetype"] = new OptionSetValue(SourceTypeDatabase);
        entity["version"] = assemblyName.Version?.ToString() ?? "1.0.0.0";
        entity["culture"] = string.IsNullOrWhiteSpace(assemblyName.CultureName) ? "neutral" : assemblyName.CultureName;

        var publicKeyToken = GetPublicKeyToken(assemblyName);
        if (!string.IsNullOrWhiteSpace(publicKeyToken))
        {
            entity["publickeytoken"] = publicKeyToken;
        }

        return entity;
    }

    private static Entity CreatePluginTypeEntity(Guid assemblyId, string typeName, Guid id)
    {
        var entity = new Entity("plugintype") { Id = id };
        entity["pluginassemblyid"] = new EntityReference("pluginassembly", assemblyId);
        entity["typename"] = typeName;
        entity["name"] = GetShortTypeName(typeName);
        entity["friendlyname"] = typeName;
        return entity;
    }

    private static async Task<Entity?> FindPluginAssemblyAsync(IOrganizationServiceAsync2 service, string assemblyName)
    {
        var query = new QueryExpression("pluginassembly")
        {
            ColumnSet = new ColumnSet("pluginassemblyid", "name")
        };
        query.Criteria.AddCondition("name", ConditionOperator.Equal, assemblyName);

        var response = await service.RetrieveMultipleAsync(query);
        if (response.Entities.Count > 1)
        {
            throw new InvalidOperationException($"Plugin assembly name '{assemblyName}' is not unique in Dataverse. Found {response.Entities.Count} records.");
        }

        return response.Entities.FirstOrDefault();
    }

    private static async Task<Entity?> FindPluginTypeAsync(IOrganizationServiceAsync2 service, Guid assemblyId, string typeName)
    {
        var query = new QueryExpression("plugintype")
        {
            ColumnSet = new ColumnSet("plugintypeid", "typename", "pluginassemblyid")
        };
        query.Criteria.AddCondition("typename", ConditionOperator.Equal, typeName);
        query.Criteria.AddCondition("pluginassemblyid", ConditionOperator.Equal, assemblyId);

        var response = await service.RetrieveMultipleAsync(query);
        if (response.Entities.Count > 1)
        {
            throw new InvalidOperationException($"Plugin type '{typeName}' is not unique for plugin assembly '{assemblyId}'. Found {response.Entities.Count} records.");
        }

        return response.Entities.FirstOrDefault();
    }

    private static string GetShortTypeName(string typeName)
    {
        var lastDot = typeName.LastIndexOf('.');
        return lastDot < 0 ? typeName : typeName[(lastDot + 1)..];
    }

    private static string? GetPublicKeyToken(AssemblyName assemblyName)
    {
        var token = assemblyName.GetPublicKeyToken();
        return token == null || token.Length == 0
            ? null
            : string.Concat(token.Select(item => item.ToString("x2")));
    }
}

internal sealed class PluginAssemblyDeploymentResult
{
    public Dictionary<string, Guid> PluginTypeIdsByName { get; } = new(StringComparer.OrdinalIgnoreCase);

    public List<SolutionComponentDeploymentResult> SolutionComponents { get; } = [];
}

internal sealed class SolutionComponentDeploymentResult
{
    public string ComponentType { get; init; } = string.Empty;

    public string ComponentName { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;
}
