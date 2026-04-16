namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators.Interfaces;

internal interface IPredicateValidator : IValidator
{
    bool IsPredicateValid(TaskContext taskContext);

    string GetPredicateMessage();
}