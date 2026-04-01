using Pillaro.Dataverse.PluginFramework.Plugins;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;

/// <summary>
/// Plugin stage validation
/// </summary>
public interface IBasicStageValidation
{
    /// <summary>
    /// Check, if plugin task stage match to given stage
    /// </summary>
    IBasicMessageValidation WithStage(PluginStage stage);
}
