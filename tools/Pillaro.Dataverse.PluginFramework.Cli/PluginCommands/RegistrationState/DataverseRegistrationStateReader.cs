using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.RegistrationState;

internal static class DataverseRegistrationStateReader
{
    public static async Task<DataverseRegistrationState> ReadAsync(
        IOrganizationServiceAsync2 service,
        PluginManifestDocument manifest)
    {
        var state = new DataverseRegistrationState();
        await ReadPluginTypesAsync(service, state, manifest);
        await ReadStepsByPluginTypeAsync(service, state);
        await ReadImagesByStepAsync(service, state);
        return state;
    }

    private static async Task ReadPluginTypesAsync(
        IOrganizationServiceAsync2 service,
        DataverseRegistrationState state,
        PluginManifestDocument manifest)
    {
        var typeNames = manifest.Plugins.Select(plugin => plugin.TypeName).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (typeNames.Length == 0)
        {
            return;
        }

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
                    state.PluginTypeIdsByName[typeName] = entity.Id;
                }
            }
        }

        var missingTypes = typeNames.Where(typeName => !state.PluginTypeIdsByName.ContainsKey(typeName)).ToArray();
        if (missingTypes.Length > 0)
        {
            throw new InvalidOperationException($"Plugin types were not found in Dataverse. Ensure PAC plugin push completed successfully: {string.Join(", ", missingTypes)}");
        }
    }

    private static async Task ReadStepsByPluginTypeAsync(
        IOrganizationServiceAsync2 service,
        DataverseRegistrationState state)
    {
        if (state.PluginTypeIdsByName.Count == 0)
        {
            return;
        }

        foreach (var pluginType in state.PluginTypeIdsByName)
        {
            var query = new QueryExpression("sdkmessageprocessingstep")
            {
                ColumnSet = new ColumnSet(
                    "sdkmessageprocessingstepid",
                    "name",
                    "stage",
                    "mode",
                    "rank",
                    "filteringattributes",
                    "plugintypeid",
                    "sdkmessageid",
                    "sdkmessagefilterid")
            };

            query.Criteria.AddCondition("plugintypeid", ConditionOperator.Equal, pluginType.Value);
            query.LinkEntities.Add(new LinkEntity(
                "sdkmessageprocessingstep",
                "sdkmessage",
                "sdkmessageid",
                "sdkmessageid",
                JoinOperator.LeftOuter)
            {
                EntityAlias = "message",
                Columns = new ColumnSet("name")
            });

            query.LinkEntities.Add(new LinkEntity(
                "sdkmessageprocessingstep",
                "sdkmessagefilter",
                "sdkmessagefilterid",
                "sdkmessagefilterid",
                JoinOperator.LeftOuter)
            {
                EntityAlias = "filter",
                Columns = new ColumnSet("primaryobjecttypecode")
            });

            var response = await service.RetrieveMultipleAsync(query);
            foreach (var entity in response.Entities)
            {
                var stepId = entity.Id;
                state.StepsById[stepId] = new DataverseStepState
                {
                    StepId = stepId,
                    PluginTypeId = pluginType.Value,
                    PluginTypeName = pluginType.Key,
                    Name = entity.GetAttributeValue<string>("name"),
                    MessageName = GetAliasedString(entity, "message.name") ?? string.Empty,
                    EntityName = GetAliasedString(entity, "filter.primaryobjecttypecode"),
                    Stage = entity.GetAttributeValue<OptionSetValue>("stage")?.Value ?? 0,
                    Mode = entity.GetAttributeValue<OptionSetValue>("mode")?.Value ?? 0,
                    Rank = entity.GetAttributeValue<int?>("rank") ?? 0,
                    FilteringAttributes = SplitAttributes(entity.GetAttributeValue<string>("filteringattributes")),
                };
            }
        }
    }

    private static async Task ReadImagesByStepAsync(
        IOrganizationServiceAsync2 service,
        DataverseRegistrationState state)
    {
        var stepIds = state.StepsById.Keys.ToArray();
        if (stepIds.Length == 0)
        {
            return;
        }

        foreach (var batch in stepIds.Chunk(500))
        {
            var query = new QueryExpression("sdkmessageprocessingstepimage")
            {
                ColumnSet = new ColumnSet(
                    "sdkmessageprocessingstepimageid",
                    "sdkmessageprocessingstepid",
                    "name",
                    "imagetype",
                    "attributes")
            };

            query.Criteria.AddCondition("sdkmessageprocessingstepid", ConditionOperator.In, batch.Cast<object>().ToArray());

            var response = await service.RetrieveMultipleAsync(query);
            foreach (var entity in response.Entities)
            {
                var imageId = entity.Id;
                state.ImagesById[imageId] = new DataverseImageState
                {
                    ImageId = imageId,
                    StepId = entity.GetAttributeValue<EntityReference>("sdkmessageprocessingstepid")?.Id ?? Guid.Empty,
                    Name = entity.GetAttributeValue<string>("name") ?? string.Empty,
                    Type = ToImageType(entity.GetAttributeValue<OptionSetValue>("imagetype")?.Value),
                    Attributes = SplitAttributes(entity.GetAttributeValue<string>("attributes")),
                };
            }
        }
    }

    private static string? GetAliasedString(Entity entity, string attributeName)
    {
        if (!entity.Attributes.TryGetValue(attributeName, out var value))
        {
            return null;
        }

        return value is AliasedValue aliasedValue ? aliasedValue.Value?.ToString() : value?.ToString();
    }

    private static IReadOnlyCollection<string> SplitAttributes(string? attributes)
    {
        if (string.IsNullOrWhiteSpace(attributes))
        {
            return [];
        }

        return attributes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(attribute => !string.IsNullOrWhiteSpace(attribute))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(attribute => attribute, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string ToImageType(int? imageType)
    {
        return imageType switch
        {
            0 => "PreImage",
            1 => "PostImage",
            2 => "Both",
            _ => imageType?.ToString() ?? string.Empty,
        };
    }
}
