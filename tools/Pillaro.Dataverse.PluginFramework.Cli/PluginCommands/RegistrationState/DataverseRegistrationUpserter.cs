using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.RegistrationState;

internal static class DataverseRegistrationUpserter
{
    public static async Task ApplyAsync(
        IOrganizationServiceAsync2 service,
        PluginManifestDocument manifest,
        PluginRegistrationDiff diff,
        string solutionName,
        IReadOnlyDictionary<string, Guid>? pluginTypeIdsByName = null)
    {
        if (string.IsNullOrWhiteSpace(solutionName))
        {
            throw new ArgumentException("Solution name is required.", nameof(solutionName));
        }

        var pluginTypeIds = pluginTypeIdsByName == null
            ? await LoadPluginTypeIdsAsync(service, manifest)
            : new Dictionary<string, Guid>(pluginTypeIdsByName, StringComparer.OrdinalIgnoreCase);
        var messageIds = await LoadMessageIdsAsync(service, manifest);
        var messageFilterIds = await LoadMessageFilterIdsAsync(service, manifest, messageIds);

        foreach (var imageChange in diff.ImageChanges.Where(change => change.Action == PluginDiffAction.Delete))
        {
            await service.DeleteAsync("sdkmessageprocessingstepimage", imageChange.ImageId);
        }

        foreach (var stepChange in diff.StepChanges.Where(change => change.Action == PluginDiffAction.Delete))
        {
            await service.DeleteAsync("sdkmessageprocessingstep", stepChange.StepId);
        }

        foreach (var stepChange in diff.StepChanges.Where(change => change.Action is PluginDiffAction.Create or PluginDiffAction.Update))
        {
            var plugin = manifest.Plugins.Single(item => item.TypeName == stepChange.PluginTypeName);
            var step = plugin.Steps.Single(item => item.StepId == stepChange.StepId);
            await UpsertStepAsync(service, plugin, step, pluginTypeIds, messageIds, messageFilterIds, stepChange.Action);

            var stepImages = diff.ImageChanges.Where(ic => ic.StepId == step.StepId && ic.Action is PluginDiffAction.Create or PluginDiffAction.Update).ToList();
            foreach (var imageChange in stepImages)
            {
                var image = step.Images.Single(item => item.ImageId == imageChange.ImageId);
                await UpsertImageAsync(service, step, image, imageChange.Action);
            }
        }
    }

    public static async Task EnsureDesiredSolutionMembershipAsync(
        IOrganizationServiceAsync2 service,
        PluginManifestDocument manifest,
        string solutionName)
    {
        foreach (var step in manifest.Plugins.SelectMany(plugin => plugin.Steps).OrderBy(step => step.StepId))
        {
            await DataverseSolutionComponentService.EnsureAddedWithSubcomponentRetryAsync(
                service,
                solutionName,
                DataverseSolutionComponentTypes.SdkMessageProcessingStep,
                step.StepId);
        }
    }

    private static async Task<Dictionary<string, Guid>> LoadPluginTypeIdsAsync(IOrganizationServiceAsync2 service, PluginManifestDocument manifest)
    {
        var typeNames = manifest.Plugins.Select(plugin => plugin.TypeName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var batch in typeNames.Chunk(500))
        {
            var query = new QueryExpression("plugintype") { ColumnSet = new ColumnSet("plugintypeid", "typename") };
            query.Criteria.AddCondition("typename", ConditionOperator.In, batch.Cast<object>().ToArray());
            var response = await service.RetrieveMultipleAsync(query);
            foreach (var entity in response.Entities)
            {
                var typeName = entity.GetAttributeValue<string>("typename");
                if (!string.IsNullOrWhiteSpace(typeName))
                {
                    result[typeName] = entity.Id;
                }
            }
        }

        var missingTypes = typeNames.Where(typeName => !result.ContainsKey(typeName)).ToArray();
        if (missingTypes.Length > 0)
        {
            throw new InvalidOperationException($"Plugin types were not found in Dataverse. Ensure the plugin assembly/type deployment completed successfully before deploying steps/images: {string.Join(", ", missingTypes)}");
        }

        return result;
    }

    private static async Task<Dictionary<string, Guid>> LoadMessageIdsAsync(IOrganizationServiceAsync2 service, PluginManifestDocument manifest)
    {
        var names = manifest.Plugins.SelectMany(plugin => plugin.Steps).Select(step => step.MessageName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var batch in names.Chunk(500))
        {
            var query = new QueryExpression("sdkmessage") { ColumnSet = new ColumnSet("sdkmessageid", "name") };
            query.Criteria.AddCondition("name", ConditionOperator.In, batch.Cast<object>().ToArray());
            var response = await service.RetrieveMultipleAsync(query);
            foreach (var entity in response.Entities)
            {
                var name = entity.GetAttributeValue<string>("name");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    result[name] = entity.Id;
                }
            }
        }

        var missing = names.Where(name => !result.ContainsKey(name)).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException($"SDK messages were not found in Dataverse: {string.Join(", ", missing)}");
        }

        return result;
    }

    private static async Task<Dictionary<string, Guid>> LoadMessageFilterIdsAsync(IOrganizationServiceAsync2 service, PluginManifestDocument manifest, IReadOnlyDictionary<string, Guid> messageIds)
    {
        var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var step in manifest.Plugins.SelectMany(plugin => plugin.Steps).Where(step => !string.IsNullOrWhiteSpace(step.EntityName)))
        {
            var key = GetMessageFilterKey(step.MessageName, step.EntityName!);
            if (result.ContainsKey(key))
            {
                continue;
            }

            var query = new QueryExpression("sdkmessagefilter") { ColumnSet = new ColumnSet("sdkmessagefilterid", "primaryobjecttypecode", "sdkmessageid") };
            query.Criteria.AddCondition("primaryobjecttypecode", ConditionOperator.Equal, step.EntityName);
            query.Criteria.AddCondition("sdkmessageid", ConditionOperator.Equal, messageIds[step.MessageName]);
            var filter = (await service.RetrieveMultipleAsync(query)).Entities.FirstOrDefault();
            if (filter == null)
            {
                throw new InvalidOperationException($"SDK message filter was not found for message '{step.MessageName}' and entity '{step.EntityName}'.");
            }

            result[key] = filter.Id;
        }

        return result;
    }

    private static async Task UpsertStepAsync(
        IOrganizationServiceAsync2 service,
        PluginManifestPlugin plugin,
        PluginManifestStep step,
        IReadOnlyDictionary<string, Guid> pluginTypeIds,
        IReadOnlyDictionary<string, Guid> messageIds,
        IReadOnlyDictionary<string, Guid> messageFilterIds,
        PluginDiffAction action)
    {
        var entity = new Entity("sdkmessageprocessingstep") { Id = step.StepId };
        var stepName = !string.IsNullOrWhiteSpace(step.Name) ? step.Name : null;

        // On Create, Dataverse requires a name - generate a readable default if not explicitly set.
        if (stepName != null || action == PluginDiffAction.Create)
        {
            entity["name"] = stepName ?? $"{step.MessageName} {step.EntityName ?? string.Empty} {step.StageName} {step.ModeName}".Trim();
        }
        entity["plugintypeid"] = new EntityReference("plugintype", pluginTypeIds[plugin.TypeName]);
        entity["sdkmessageid"] = new EntityReference("sdkmessage", messageIds[step.MessageName]);
        entity["stage"] = new OptionSetValue(step.Stage);
        entity["mode"] = new OptionSetValue(step.Mode);
        entity["rank"] = step.Rank;
        entity["filteringattributes"] = step.FilteringAttributes.Count == 0 ? null : string.Join(",", step.FilteringAttributes);
        entity["configuration"] = string.IsNullOrWhiteSpace(step.UnsecureConfiguration) ? null : step.UnsecureConfiguration;
        if (!string.IsNullOrWhiteSpace(step.EntityName))
        {
            entity["sdkmessagefilterid"] = new EntityReference("sdkmessagefilter", messageFilterIds[GetMessageFilterKey(step.MessageName, step.EntityName)]);
        }

        if (action == PluginDiffAction.Create)
        {
            entity["sdkmessageprocessingstepid"] = step.StepId;
            await service.CreateAsync(entity);
        }
        else
        {
            await service.UpdateAsync(entity);
        }
    }

    private static async Task UpsertImageAsync(IOrganizationServiceAsync2 service, PluginManifestStep step, PluginManifestImage image, PluginDiffAction action)
    {
        var entity = new Entity("sdkmessageprocessingstepimage") { Id = image.ImageId };
        entity["sdkmessageprocessingstepid"] = new EntityReference("sdkmessageprocessingstep", step.StepId);
        entity["name"] = image.Name;
        entity["entityalias"] = image.Name;
        entity["messagepropertyname"] = "Target";
        entity["imagetype"] = new OptionSetValue(ToImageTypeValue(image.Type));
        entity["attributes"] = string.Join(",", image.Attributes);
        if (action == PluginDiffAction.Create)
        {
            entity["sdkmessageprocessingstepimageid"] = image.ImageId;
            await service.CreateAsync(entity);
        }
        else
        {
            await service.UpdateAsync(entity);
        }
    }

    private static string GetMessageFilterKey(string messageName, string entityName) => $"{messageName}:{entityName}";

    private static async Task<Guid> EnsureSecureConfigAsync(IOrganizationServiceAsync2 service, string secureConfiguration)
    {
        var query = new QueryExpression("sdkmessageprocessingstepsecureconfig")
        {
            ColumnSet = new ColumnSet("sdkmessageprocessingstepsecureconfigid"),
            TopCount = 1
        };
        query.Criteria.AddCondition("secureconfig", ConditionOperator.Equal, secureConfiguration);
        var existing = await service.RetrieveMultipleAsync(query);
        if (existing.Entities.Count > 0)
        {
            return existing.Entities[0].Id;
        }

        var entity = new Entity("sdkmessageprocessingstepsecureconfig")
        {
            ["secureconfig"] = secureConfiguration
        };
        return await service.CreateAsync(entity);
    }

    private static int ToImageTypeValue(string imageType)
    {
        if (string.Equals(imageType, "PreImage", StringComparison.OrdinalIgnoreCase)) return 0;
        if (string.Equals(imageType, "PostImage", StringComparison.OrdinalIgnoreCase)) return 1;
        if (string.Equals(imageType, "Both", StringComparison.OrdinalIgnoreCase)) return 2;
        throw new InvalidOperationException($"Unsupported image type '{imageType}'.");
    }

    private static string GetActionLabel(PluginDiffAction action)
    {
        return action switch
        {
            PluginDiffAction.Create => "CREATE",
            PluginDiffAction.Update => "UPDATE",
            PluginDiffAction.Delete => "DELETE",
            PluginDiffAction.Unchanged => "OK",
            _ => action.ToString().ToUpperInvariant(),
        };
    }
}
