namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;

/// <summary>
/// Entity validation
/// </summary>
public interface IBasicPrimaryEntityValidation : IBasicImageValidation
{
    /// <summary>
    /// Check, if plugin task is registered for given entity type
    /// </summary>
    IBasicImageValidation ForEntity(string entityName);
    /// <summary>
    /// Check, if plugin task is registered for given entities types
    /// </summary>        
    IBasicImageValidation ForEntities(params string[] entityNames);
}