namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;

/// <summary>
/// Plugin message validation
/// </summary>
public interface IBasicMessageValidation
{
    /// <summary>
    /// Check, if plugin task is registered with given message
    /// </summary>
    IBasicPrimaryEntityValidation WithMessage(string message);
    /// <summary>
    /// Check, if plugin task is registered with one of given messages
    /// </summary>
    IBasicPrimaryEntityValidation WithMessages(params string[] messages);
}
