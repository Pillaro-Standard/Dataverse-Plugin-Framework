using System;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;

/// <summary>
/// Custom validation. Use for validations WITHOUT performance impact
/// All these validations will be always triggered
/// </summary>
public interface ICustomValidation : IBreakValidation
{
    /// <summary>
    /// Check, if given predicate is valid.
    /// </summary>
    ICustomValidation WithValidation(string message, Func<TaskContext, bool> predicate);

    /// <summary>
    /// Check, if given predicate is valid.
    /// The input message is Lazy string, so it can be activated during validation
    /// </summary>
    ICustomValidation WithValidation(Lazy<string> message, Func<TaskContext, bool> predicate);
}
