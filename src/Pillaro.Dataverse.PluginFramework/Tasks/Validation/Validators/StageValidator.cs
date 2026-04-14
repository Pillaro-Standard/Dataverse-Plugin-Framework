using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators.Interfaces;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators;

internal class StageValidator : IBasicValidator
{
    private readonly PluginStage _stage;

    public StageValidator(PluginStage stage)
    {
        _stage = stage;
    }

    public string GetName => nameof(StageValidator);

    public bool Validate(TaskContext taskContext)
    {
        return taskContext.Stage == _stage;
    }

    public string GetMessage()
    {
        return $"Plugin stage is not {_stage}";
    }
}