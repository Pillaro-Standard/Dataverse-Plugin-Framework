using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.RegistrationState;

internal static class DataverseRegistrationUpserter
{
    private const int StepComponentType = 92;
    private const int ImageComponentType = 93;

    public static async Task ApplyAsync(
        IOrganizationServiceAsync2 service,
        PluginManifestDocument manifest,
        PluginRegistrationDiff diff,
        string solutionName)
    {
        if (string.IsNullOrWhiteSpace(solutionName))
        {
            throw new ArgumentException("Solution name is required.", nameof(solutionName));
        }

        var pluginTypeIds = await LoadPluginTypeIdsAsync(service, manifest);
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
            await AddToSolutionAsync(service, solutionName, StepComponentType, step.StepId);
        }

        foreach (var imageChange in diff.ImageChanges.Where(change => change.Action is PluginDiffAction.Create or PluginDiffAction.Update))
        {
            var step = manifest.Plugins.SelectMany(plugin => plugin.Steps).Single(item => item.StepId == imageChange.StepId);
            var image = step.Images.Single(item => item.ImageId == imageChange.ImageId);

            await UpsertImageAsync(service, step, image, imageChange.Action);
            await AddToSolutionAsync(service, solutionName, ImageComponentType, image.ImageId);
        }
    }

    private static async Task AddToSolutionAsync(IOrganizationServiceAsync2 service, string solutionName, int componentType, Guid componentId)
    {
        var request = new AddSolutionComponentRequest
        {
            SolutionUniqueName = solutionName,
            ComponentType = componentType,
            ComponentId = componentId,
            AddRequiredComponents = false,
            DoNotIncludeSubcomponents = true,
        };

        await service.ExecuteAsync(request);
    }

    private static async Task<Dictionary<string, Guid>> LoadPluginTypeIdsAsync(IOrganizationServiceAsync2 service, PluginManifestDocument manifest)
    {
        var typeNames = manifest.Plugins.Select(plugin => plugin.TypeName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var batch in typeNames.Chunk(500))
        {
            var query = new QueryExpression("plugintype")
            {
                ColumnSet = new ColumnSet("plugintypeid", "typename")
            };
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
            throw new InvalidOperationException($"Plugin types were not found in Dataverse. Ensure 'pac plugin push' has completed successfully: {string.Join(", ", missingTypes)}");
        }

        return result;
    }

    private static async Task<Dictionary<string, Guid>> LoadMessageIdsAsync(IOrganizationServiceAsync2 service, PluginManifestDocument manifest)
    {
        var messageNames = manifest.Plugins.SelectMany(plugin => plugin.Steps).Select(step => step.MessageName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var batch in messageNames.Chunk(500))
        {
            var query = new QueryExpression("sdkmessage")
            {
                ColumnSet = new ColumnSet("sdkmessageid", "name")
            };
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

        var missingMessages = messageNames.Where(messageName => !result.ContainsKey(messageName)).ToArray();
        if (missingMessages.Length > 0)
        {
            throw new InvalidOperationException($"SDK messages were not found in Dataverse: {string.Join(", ", missingMessages)}");
        }

        return result;
    }

    private static async Task<Dictionary<string, Guid>> LoadMessageFilterIdsAsync(
        IOrganizationServiceAsync2 service,
        PluginManifestDocument manifest,
        IReadOnlyDictionary<string, Guid> messageIds)
    {
        var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var stepsWithEntity = manifest.Plugins.SelectMany(plugin => plugin.Steps).Where(step => !string.IsNullOrWhiteSpace(step.EntityName)).ToArray();

        foreach (var step in stepsWithEntity)
        {
            var key = GetMessageFilterKey(step.MessageName, step.EntityName!);
            if (result.ContainsKey(key))
            {
                continue;
            }

            var query = new QueryExpression("sdkmessagefilter")
            {
                ColumnSet = new ColumnSet("sdkmessagefilterid", "primaryobjecttypecode", "sdkmessageid")
            };
            query.Criteria.AddCondition("primaryobjecttypecode", ConditionOperator.Equal, step.EntityName);
            query.Criteria.AddCondition("sdkmessageid", ConditionOperator.Equal, messageIds[step.MessageName]);

            var response = await service.RetrieveMultipleAsync(query);
            var filter = response.Entities.FirstOrDefault();
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
        var entity = new Entity("sdkmessageprocessingstep")
        {
            Id = step.StepId
        };

        entity["name"] = BuildStepName(plugin, step);
        entity["plugintypeid"] = new EntityReference("plugintype", pluginTypeIds[plugin.TypeName]);
        entity["sdkmessageid"] = new EntityReference("sdkmessage", messageIds[step.MessageName]);
        entity["stage"] = new OptionSetValue(step.Stage);
        entity["mode"] = new OptionSetValue(step.Mode);
        entity["rank"] = step.Rank;
        entity["filteringattributes"] = step.FilteringAttributes.Count == 0 ? null : string.Join(",", step.FilteringAttributes);

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

    private static async Task UpsertImageAsync(
        IOrganizationServiceAsync2 service,
        PluginManifestStep step,
        PluginManifestImage image,
        PluginDiffAction action)
    {
        var entity = new Entity("sdkmessageprocessingstepimage")
        {
            Id = image.ImageId
        };

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

    private static string BuildStepName(PluginManifestPlugin plugin, PluginManifestStep step)
    {
        return $"{plugin.TypeName}: {step.MessageName} {step.EntityName ?? string.Empty} {step.StageName} {step.ModeName}".Trim();
    }

    private static string GetMessageFilterKey(string messageName, string entityName)
    {
        return $"{messageName}:{entityName}";
    }

    private static int ToImageTypeValue(string imageType)
    {
        if (string.Equals(imageType, "PreImage", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (string.Equals(imageType, "PostImage", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (string.Equals(imageType, "Both", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        throw new InvalidOperationException($"Unsupported image type '{imageType}'.");
    }
}
