using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Pillaro.Dataverse.PluginFramework.Extensions;

/// <summary>
/// Extension methods for IOrganizationService that provide convenient overloads and helper methods.
/// </summary>
public static class OrganizationServiceExtensions
{
    /// <summary>
    /// Deletes an entity using its EntityReference.
    /// </summary>
    public static void Delete(this IOrganizationService service, EntityReference entityReference)
    {
        if (entityReference == null)
        {
            throw new ArgumentNullException(nameof(entityReference));
        }

        service.Delete(entityReference.LogicalName, entityReference.Id);
    }

    /// <summary>
    /// Deletes an entity using the entity object.
    /// </summary>
    public static void Delete(this IOrganizationService service, Entity entity)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        service.Delete(entity.LogicalName, entity.Id);
    }

    /// <summary>
    /// Deletes multiple entities in a collection.
    /// </summary>
    public static void Delete<TEntity>(this IOrganizationService service, IEnumerable<TEntity> entityCollection) where TEntity : Entity
    {
        if (entityCollection == null)
        {
            throw new ArgumentNullException(nameof(entityCollection));
        }

        foreach (var entity in entityCollection)
        {
            service.Delete(entity.LogicalName, entity.Id);
        }
    }

    /// <summary>
    /// Deletes all entities in an EntityCollection.
    /// </summary>
    public static void Delete(this IOrganizationService service, EntityCollection entityCollection)
    {
        if (entityCollection == null)
        {
            throw new ArgumentNullException(nameof(entityCollection));
        }

        foreach (var entity in entityCollection.Entities)
        {
            service.Delete(entity.LogicalName, entity.Id);
        }
    }

    /// <summary>
    /// Sets the state and status of an entity.
    /// </summary>
    public static SetStateResponse SetState(this IOrganizationService service, EntityReference entityReference, int stateCode, int? statusCode)
    {
        if (entityReference == null)
        {
            throw new ArgumentNullException(nameof(entityReference));
        }

        SetStateRequest request = new()
        {
            State = new OptionSetValue(stateCode),
            Status = new OptionSetValue(statusCode ?? -1),
            EntityMoniker = entityReference
        };

        return (SetStateResponse)service.Execute(request);
    }

    /// <summary>
    /// Executes WhoAmI request to get current user information.
    /// </summary>
    public static WhoAmIResponse WhoAmI(this IOrganizationService service)
    {
        return (WhoAmIResponse)service.Execute(new WhoAmIRequest());
    }
}