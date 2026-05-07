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
        var stepIds = manifest.Plugins.SelectMany(plugin => plugin.Steps).Select(step => step.StepId).Distinct().ToArray();
        var imageIds = manifest.Plugins.SelectMany(plugin => plugin.Steps).SelectMany(step => step.Images).Select(image => image.ImageId).Distinct().ToArray();

        await ReadStepsAsync(service, state, stepIds);
        await ReadImagesAsync(service, state, imageIds);

        return state;
    }

    private static async Task ReadStepsAsync(
        IOrganizationServiceAsync2 service,
        DataverseRegistrationState state,
        IReadOnlyCollection<Guid> stepIds)
    {
        if (stepIds.Count == 0)
        {
            return;
        }

        foreach (var batch in stepIds.Chunk(500))
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
                    "sdkmessageid",
                    "sdkmessagefilterid")
            };

            query.Criteria.AddCondition("sdkmessageprocessingstepid", ConditionOperator.In, batch.Cast<object>().ToArray());
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

    private static async Task ReadImagesAsync(
        IOrganizationServiceAsync2 service,
        DataverseRegistrationState state,
        IReadOnlyCollection<Guid> imageIds)
    {
        if (imageIds.Count == 0)
        {
            return;
        }

        foreach (var batch in imageIds.Chunk(500))
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

            query.Criteria.AddCondition("sdkmessageprocessingstepimageid", ConditionOperator.In, batch.Cast<object>().ToArray());

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
