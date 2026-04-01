
using Pillaro.Dataverse.PluginFramework.Tasks;

namespace Pillaro.Dataverse.PluginFramework.Plugins.Validation.Validators.Interfaces;

internal interface IPredicateValidator : IValidator
{
    bool IsPredicateValid(TaskContext taskContext);

    string GetPredicateMessage();
}