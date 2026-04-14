using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators.Interfaces;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators;

internal class ModeValidator : IBasicValidator
{
    private readonly PluginMode _mode;

    public ModeValidator(PluginMode mode)
    {
        _mode = mode;
    }

    public string GetName => nameof(ModeValidator);

    public bool Validate(TaskContext taskContext)
    {
        return taskContext.Mode == _mode;
    }

    public string GetMessage()
    {
        return $"Plugin mode is not {_mode}";
    }
}