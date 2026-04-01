using Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators.Interfaces;
using System;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators;

internal class CustomBreakValidator : IBreakValidator
{
    private readonly Lazy<string> _message;
    private readonly Func<TaskContext, bool> _predicate;

    public CustomBreakValidator(string message, Func<TaskContext, bool> predicate)
    {
        _message = new Lazy<string>(() => message);
        _predicate = predicate;
    }

    public CustomBreakValidator(Lazy<string> message, Func<TaskContext, bool> predicate)
    {
        _message = message;
        _predicate = predicate;
    }

    public string GetName => nameof(CustomBreakValidator);

    public bool Validate(TaskContext taskContext)
    {
        return _predicate.Invoke(taskContext);
    }

    public string GetMessage()
    {
        return _message.Value;
    }
}