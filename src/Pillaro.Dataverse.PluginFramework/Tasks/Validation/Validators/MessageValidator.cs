using Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators;

internal class MessageValidator : IBasicValidator
{
    private readonly IEnumerable<string> _messages;

    public MessageValidator(IEnumerable<string> messages)
    {
        _messages = messages;
    }

    public string GetName => nameof(MessageValidator);

    public bool Validate(TaskContext taskContext)
    {
        return _messages.Select(x => x.ToLower()).Contains(taskContext.Message.ToLower());
    }

    public string GetMessage()
    {
        return $"Plugin message is not {String.Join(",", _messages)}";
    }
}