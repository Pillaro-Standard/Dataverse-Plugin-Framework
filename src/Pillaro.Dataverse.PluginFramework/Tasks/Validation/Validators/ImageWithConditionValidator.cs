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
        if (!_predicateResult || _imageValidationRes == null)
            return string.Empty;

        return _imageValidator.GetMessage();
    }

    public bool IsPredicateValid(TaskContext taskContext)
    {
        _predicateResult = _predicate.Invoke(taskContext);
        return _predicateResult;
    }

    public string GetPredicateMessage()
    {
        return _predicateResult
            ? "Conditional image validation is applicable."
            : "Conditional image validation is not applicable.";
    }
}