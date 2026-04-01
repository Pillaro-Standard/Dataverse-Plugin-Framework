using System.Collections.Generic;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;

/// <summary>
/// Execute validation process
/// </summary>
internal interface IExecuteValidation
{
    /// <summary>
    /// Executes all registered validadions
    /// </summary>
    bool IsValid();

    /// <summary>
    /// Returns all not valid messages
    /// </summary>
    IEnumerable<string> GetValidationMessages();
}
