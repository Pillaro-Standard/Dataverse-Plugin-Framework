using Microsoft.Xrm.Sdk;
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
            Columns = new ColumnSet("uniquename")
        });

        query.Criteria.AddCondition("solution.uniquename", ConditionOperator.Equal, solutionName);

        var response = await service.RetrieveMultipleAsync(query);
        var metadataIds = response.Entities
            .Select(entity => entity.GetAttributeValue<Guid?>("objectid"))
            .Where(id => id.HasValue && id.Value != Guid.Empty)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        if (metadataIds.Length == 0)
        {
            return [];
        }

        var entityNames = new List<string>();
        foreach (var metadataId in metadataIds)
        {
            var metadataQuery = new QueryExpression("entity")
            {
                ColumnSet = new ColumnSet("logicalname", "metadataid")
            };
            metadataQuery.Criteria.AddCondition("metadataid", ConditionOperator.Equal, metadataId);

            var metadataResponse = await service.RetrieveMultipleAsync(metadataQuery);
            var logicalName = metadataResponse.Entities.FirstOrDefault()?.GetAttributeValue<string>("logicalname");
            if (!string.IsNullOrWhiteSpace(logicalName))
            {
                entityNames.Add(logicalName);
            }
        }

        return entityNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
