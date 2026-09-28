using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Tasks;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using System;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Contact
{
    /// <summary>
    /// Writes the new job title into the description of the contact.
    /// Shows the update queue in a pre-stage: the queued values are merged into the message target,
    /// so they are saved by the operation that is already running instead of a second update.
    /// </summary>
    public class RecordJobTitleChange : TaskBase<Logic.Contact>
    {
        public RecordJobTitleChange(IServiceProvider serviceProvider, TaskContext taskContext)
            : base(serviceProvider, taskContext)
        {
        }

        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Preoperation)
                .WithMessage("Update")
                .ForEntity(Logic.Contact.EntityLogicalName)
                .EntityWithAtLeastOneAttribute(ContextEntity, Logic.Contact.Fields.JobTitle);
        }

        protected override void DoExecute()
        {
            var jobTitle = ContextEntity.JobTitle;

            AddLogMessageLine($"Recording job title '{jobTitle}' in the contact description.");

            TaskContext.AddEntityToUpdate(new Logic.Contact
            {
                Id = ContextEntityReference.Id,
                Description = string.IsNullOrWhiteSpace(jobTitle)
                    ? "Job title removed"
                    : $"Job title: {jobTitle}"
            });
        }
    }
}
