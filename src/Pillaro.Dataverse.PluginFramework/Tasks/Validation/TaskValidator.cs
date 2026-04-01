using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using System.Collections.Generic;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation;

internal class TaskValidator : TaskValidatorBase, IExecuteValidation
{
    public static IBasicModeValidation Create(TaskContext taskContext)
    {
        return new TaskValidator(taskContext);
    }

    protected TaskValidator(TaskContext taskContext) : base(taskContext)
    {
    }

    public bool IsValid()
    {
        return ValidatorProcessor.IsValid();
    }

    public IEnumerable<string> GetValidationMessages()
    {
        return ValidatorProcessor.GetMessages();
    }
}