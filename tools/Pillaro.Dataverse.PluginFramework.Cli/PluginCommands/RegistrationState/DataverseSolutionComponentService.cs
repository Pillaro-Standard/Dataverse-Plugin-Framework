using Microsoft.Crm.Sdk.Messages;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Query;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.RegistrationState;

internal static class DataverseSolutionComponentService
{
    public static async Task<string> EnsureAddedAsync(
        IOrganizationServiceAsync2 service,
        string solutionName,
        int componentType,
        Guid componentId)
    {
        if (string.IsNullOrWhiteSpace(solutionName))
        {
            throw new ArgumentException("Solution name is required.", nameof(solutionName));
        }

        var solutionId = await ResolveSolutionIdAsync(service, solutionName);
        if (await ContainsComponentAsync(service, solutionId, componentType, componentId))
        {
            return "OK";
        }

        await service.ExecuteAsync(new AddSolutionComponentRequest
        {
            SolutionUniqueName = solutionName,
            ComponentType = componentType,
            ComponentId = componentId,
            AddRequiredComponents = false,
            DoNotIncludeSubcomponents = true,
        });

        return "ADD";
    }

    public static async Task<string> EnsureAddedWithRequiredComponentsAsync(
        IOrganizationServiceAsync2 service,
        string solutionName,
        int componentType,
        Guid componentId)
    {
        if (string.IsNullOrWhiteSpace(solutionName))
        {
            throw new ArgumentException("Solution name is required.", nameof(solutionName));
        }

        var solutionId = await ResolveSolutionIdAsync(service, solutionName);
        if (await ContainsComponentAsync(service, solutionId, componentType, componentId))
        {
            return "OK";
        }

        await service.ExecuteAsync(new AddSolutionComponentRequest
        {
            SolutionUniqueName = solutionName,
            ComponentType = componentType,
            ComponentId = componentId,
            AddRequiredComponents = true,
            DoNotIncludeSubcomponents = false,
        });

        return "ADD";
    }

    public static async Task<string> EnsureAddedWithSubcomponentRetryAsync(
        IOrganizationServiceAsync2 service,
        string solutionName,
        int componentType,
        Guid componentId)
    {
        try
        {
            return await EnsureAddedAsync(service, solutionName, componentType, componentId);
        }
        catch (Exception ex) when (RequiresSubcomponentRetry(ex))
        {
            return await EnsureAddedWithRequiredComponentsAsync(service, solutionName, componentType, componentId);
        }
    }

    private static async Task<Guid> ResolveSolutionIdAsync(IOrganizationServiceAsync2 service, string solutionName)
    {
        var query = new QueryExpression("solution")
        {
            ColumnSet = new ColumnSet("solutionid", "uniquename")
        };
        query.Criteria.AddCondition("uniquename", ConditionOperator.Equal, solutionName);

        var response = await service.RetrieveMultipleAsync(query);
        if (response.Entities.Count == 0)
        {
            throw new InvalidOperationException($"Solution '{solutionName}' was not found. Create the solution or set the top-level 'solution' in PillaroSettings.json to an existing solution unique name.");
        }

        if (response.Entities.Count > 1)
        {
            throw new InvalidOperationException($"Solution unique name '{solutionName}' is not unique in Dataverse. Found {response.Entities.Count} records.");
        }

        return response.Entities[0].Id;
    }

    private static async Task<bool> ContainsComponentAsync(
        IOrganizationServiceAsync2 service,
        Guid solutionId,
        int componentType,
        Guid componentId)
    {
        var query = new QueryExpression("solutioncomponent")
        {
            ColumnSet = new ColumnSet("solutioncomponentid"),
            TopCount = 1,
        };
        query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionId);
        query.Criteria.AddCondition("componenttype", ConditionOperator.Equal, componentType);
        query.Criteria.AddCondition("objectid", ConditionOperator.Equal, componentId);

        var response = await service.RetrieveMultipleAsync(query);
        return response.Entities.Count > 0;
    }

    private static bool RequiresSubcomponentRetry(Exception ex)
    {
        return ex.Message.Contains("DoNotIncludeSubcomponents", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("Subcomponent", StringComparison.OrdinalIgnoreCase);
    }
}
