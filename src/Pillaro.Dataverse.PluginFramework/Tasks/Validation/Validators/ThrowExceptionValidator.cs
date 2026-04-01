using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Exceptions;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators.Interfaces;
using System;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators;

internal class ThrowExceptionValidator : IBreakValidator
{
    private readonly Lazy<string> _message;
    private readonly Func<TaskContext, bool> _predicate;
    private readonly bool _isWarning;

    public ThrowExceptionValidator(string message, Func<TaskContext, bool> predicate, bool isWarning = false)
    {
        _message = new Lazy<string>(() => message);
        _predicate = predicate;
        _isWarning = isWarning;
    }

    public ThrowExceptionValidator(Lazy<string> message, Func<TaskContext, bool> predicate, bool isWarning = false)
    {
        _message = message;
        _predicate = predicate;
        _isWarning = isWarning;
    }

    public string GetName => nameof(ThrowExceptionValidator);

    public bool Validate(TaskContext taskContext)
    {
        var isValid = _predicate.Invoke(taskContext);
        if (isValid)
            return true;

        if (_isWarning)
            throw new DataverseValidationException(_message.Value);

        throw new InvalidPluginExecutionException(_message.Value);
    }

    public string GetMessage()
    {
        return _message.Value;
    }
}