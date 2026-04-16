using Pillaro.Dataverse.PluginFramework.Plugins;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;

/// <summary>
/// Plugin mode validation
/// </summary>
public interface IBasicModeValidation
{
    /// <summary>
    /// Check, if plugin task is registered with given mode
    /// </summary>
    IBasicStageValidation WithMode(PluginMode mode);
}
