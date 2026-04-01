using Pillaro.Dataverse.PluginFramework.Tasks;

namespace Pillaro.Dataverse.PluginFramework.Plugins.Validation.Validators.Interfaces;

internal interface IValidator
{
    string GetName { get; }
    bool Validate(TaskContext taskContext);
    string GetMessage();
}