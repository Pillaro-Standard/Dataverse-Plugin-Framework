using Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators.Interfaces;
using System;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators;

internal class ImageWithConditionValidator : IPredicateValidator
{
    private readonly Func<TaskContext, bool> _predicate;
    private readonly ImageValidator _imageValidator;
    private bool _predicateResult;
    private bool? _imageValidationRes;
    public string GetName => nameof(ImageWithConditionValidator);

    public ImageWithConditionValidator(Func<TaskContext, bool> predicate, bool isPreimage, string imageName)
    {
        _predicate = predicate;
        _imageValidator = new ImageValidator(imageName, isPreimage);
    }

    public bool Validate(TaskContext taskContext)
    {
        _imageValidationRes = _imageValidator.Validate(taskContext);
        return _imageValidationRes.Value;
    }

    public string GetMessage()
    {
        if (_imageValidationRes == null)
            return GetPredicateMessage();

        var mes = _imageValidator.GetMessage();
        return $"{GetPredicateMessage()}. {mes}";
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