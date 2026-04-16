using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators.Interfaces;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators;

/// <summary>
/// Entity attributes validation with condition
/// </summary>
internal class EntityAttributesWithConditionValidator(IEnumerable<string> attributes, Entity entity, bool containsAll, Func<TaskContext, bool> predicate) : IPredicateValidator
{
    private readonly Func<TaskContext, bool> _predicate = predicate;
    private readonly EntityAttributesValidator _entityAttributesValidator = new(entity, attributes, containsAll);
    private bool _predicateResult;
    private bool? _attrValidation;

    public string GetName => nameof(EntityAttributesWithConditionValidator);

    public bool Validate(TaskContext taskContext)
    {
        _attrValidation = _entityAttributesValidator.Validate(taskContext);
        return _attrValidation.Value;
    }

    public string GetMessage()
    {
        if (!_predicateResult)
            return "Conditional validation was skipped because its predicate was not satisfied.";

        if (_attrValidation == null)
            return string.Empty;

        return _entityAttributesValidator.GetMessage();
    }

    public bool IsPredicateValid(TaskContext taskContext)
    {
        _predicateResult = _predicate.Invoke(taskContext);
        return _predicateResult;
    }

    public string GetPredicateMessage()
    {
        return _predicateResult
            ? "Conditional validation is applicable."
            : "Conditional validation is not applicable.";
    }
}