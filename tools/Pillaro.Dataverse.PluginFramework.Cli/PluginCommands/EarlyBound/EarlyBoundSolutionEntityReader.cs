using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.EarlyBound;

internal static class EarlyBoundSolutionEntityReader
{
    private const int EntityComponentType = 1;

    public static async Task<IReadOnlyCollection<string>> ReadEntityLogicalNamesAsync(IOrganizationServiceAsync2 service, string solutionName)
    {
        if (string.IsNullOrWhiteSpace(solutionName))
        {
            return [];
        }

        var metadataIds = await ReadSolutionEntityMetadataIdsAsync(service, solutionName);
        if (metadataIds.Count == 0)
        {
            return [];
        }

        var metadataResponse = (RetrieveAllEntitiesResponse)await service.ExecuteAsync(new RetrieveAllEntitiesRequest
        {
            EntityFilters = EntityFilters.Entity,
            RetrieveAsIfPublished = true,
        });

        return metadataResponse.EntityMetadata
            .Where(entity => entity.MetadataId.HasValue && metadataIds.Contains(entity.MetadataId.Value))
            .Select(entity => entity.LogicalName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static async Task<HashSet<Guid>> ReadSolutionEntityMetadataIdsAsync(IOrganizationServiceAsync2 service, string solutionName)
    {
        var query = new QueryExpression("solutioncomponent")
        {
            ColumnSet = new ColumnSet("objectid", "componenttype")
        };

        query.Criteria.AddCondition("componenttype", ConditionOperator.Equal, EntityComponentType);

        query.LinkEntities.Add(new LinkEntity(
            "solutioncomponent",
            "solution",
            "solutionid",
            "solutionid",
            JoinOperator.Inner)
        {
            EntityAlias = "solution",
            Columns = new ColumnSet("uniquename"),
            LinkCriteria =
            {
                Conditions =
                {
                    new ConditionExpression("uniquename", ConditionOperator.Equal, solutionName)
                }
            }
        });

        var response = await service.RetrieveMultipleAsync(query);
        return response.Entities
            .Select(entity => entity.GetAttributeValue<Guid?>("objectid"))
            .Where(id => id.HasValue && id.Value != Guid.Empty)
            .Select(id => id!.Value)
            .ToHashSet();
    }
}
