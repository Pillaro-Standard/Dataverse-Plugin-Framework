using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Plugins.Validation.Validators.Interfaces;
using System;
using System.Collections.Generic;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators;

/// <summary>
/// Entity attributes validation with condition
/// </summary>
internal class EntityAttributesWithConditionValidator : IPredicateValidator
{
    private readonly Func<TaskContext, bool> _predicate;
    private readonly EntityAttributesValidator _entityAttributesValidator;
    private bool _predicateResult;
    private bool? _attrValidation;

    public EntityAttributesWithConditionValidator(IEnumerable<string> attributes, Entity entity, bool containsAll, Func<TaskContext, bool> predicate)
    {
        _predicate = predicate;
        _entityAttributesValidator = new EntityAttributesValidator(entity, attributes, containsAll);
    }

    public string GetName => nameof(EntityAttributesWithConditionValidator);
    public bool Validate(TaskContext taskContext)
    {
        _attrValidation = _entityAttributesValidator.Validate(taskContext);
        return _attrValidation.Value;
    }

    public string GetMessage()
    {
        if (_attrValidation == null)
            return GetPredicateMessage();

        var mess = _entityAttributesValidator.GetMessage();
        return $"{GetPredicateMessage()} {mess}";
    }

    public bool IsPredicateValid(TaskContext taskContext)
    {
        _predicateResult = _predicate.Invoke(taskContext);
        return _predicateResult;
    }

    public string GetPredicateMessage()
    {
        if (_predicateResult)
            return "Predicate is valid";

        return "Predicate is not valid";
    }
}