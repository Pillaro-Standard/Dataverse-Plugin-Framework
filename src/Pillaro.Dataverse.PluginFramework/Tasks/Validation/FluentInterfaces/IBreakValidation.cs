using System;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;

/// <summary>
/// Custom data validation. Runs only in case that all previous validations were successful.
/// </summary>
public interface IBreakValidation : ICompleteValidation
{
    /// <summary>
    /// Check, if given predicate is valid. First not valid of these validation stops triggering followings validations.
    /// Use for logic and validations WITH performance impact or if you don't want to run this validation in case that some of previous validations is not success
    /// </summary>
    IBreakValidation WithBreakValidation(string message, Func<TaskContext, bool> predicate);
    /// <summary>
    /// Check, if given predicate is valid. First not valid of these validation stops triggering followings validations.
    /// Use for logic and validations WITH performance impact or if you don't want to run this validation in case that some of previous validations is not success.
    /// The input message is Lazy string, so it can be activated during validation
    /// </summary>
    IBreakValidation WithBreakValidation(Lazy<string> message, Func<TaskContext, bool> predicate);
    /// <summary>
    /// Checks predicate. If it is not valid, throw InvalidPluginExecutionException filled by error message for user
    /// </summary>
    IBreakValidation ThrowWithError(string message, Func<TaskContext, bool> predicate);
    /// <summary>
    /// Checks predicate. If it is not valid, throw InvalidPluginExecutionException filled by error message for user
    /// The input message is Lazy string, so it can be activated during validation
    /// </summary>
    IBreakValidation ThrowWithError(Lazy<string> message, Func<TaskContext, bool> predicate);
    /// <summary>
    /// Checks predicate. If it is not valid, throw DataverseValidationException filled by error message for user. This error will be logged as Warning
    /// </summary>
    IBreakValidation ThrowWithWarning(string message, Func<TaskContext, bool> predicate);
    /// <summary>
    /// Checks predicate. If it is not valid, throw DataverseValidationException filled by error message for user. This error will be logged as Warning
    /// The input message is Lazy string, so it can be activated during validation
    /// </summary>
    IBreakValidation ThrowWithWarning(Lazy<string> message, Func<TaskContext, bool> predicate);
}
