using Pillaro.Dataverse.PluginFramework.PluginRegistrations;
using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Tasks;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using System;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Contact
{
    /// <summary>
    /// Records a deleted contact on its parent account.
    /// Shows the two things a delete step depends on: the pre-image, which is the only source of the
    /// values of a record that no longer exists, and the update queue, which writes the account once.
    /// </summary>
    public class ArchiveDeletedContact(IServiceProvider serviceProvider, TaskContext taskContext)
        : TaskBase<Logic.Contact>(serviceProvider, taskContext)
    {
        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Postoperation)
                .WithMessage(DataverseMessages.Delete)
                .ForEntity(Logic.Contact.EntityLogicalName)
                .HasPreImage()
                // A contact without a parent account is not a failure, only nothing to record:
                // the task ends as NotValid with this reason instead of a Success that did nothing.
                .WithValidation("Deleted contact has no parent account, nothing to record.", _ =>
                    string.Equals(PreImage?.ParentCustomerId?.LogicalName, Logic.Account.EntityLogicalName, StringComparison.OrdinalIgnoreCase));
        }

        protected override void DoExecute()
        {
            // There is no context entity on Delete, the pre-image carries the deleted values.
            var parentCustomer = PreImage.ParentCustomerId;

            var deletedContact = BuildContactName(PreImage);

            AddLogMessageLine($"Recording deleted contact '{deletedContact}' on account '{parentCustomer.Id}'.");

            // Queued instead of written directly, so the account is written once even when
            // several tasks of this execution contribute to it.
            // Written as the initiating user on purpose: on a post-operation Delete the pipeline
            // runs as SYSTEM, so the default ServiceUser.User would put SYSTEM in the audit
            // instead of the person who deleted the contact.
            TaskContext.AddEntityToUpdate(
                new Logic.Account
                {
                    Id = parentCustomer.Id,
                    Description = $"Deleted contact: {deletedContact}"
                },
                ServiceUser.InitiatingUser);
        }

        private static string BuildContactName(Logic.Contact contact)
        {
            var name = $"{contact.FirstName} {contact.LastName}".Trim();

            return string.IsNullOrWhiteSpace(name) ? contact.Id.ToString("D") : name;
        }
    }
}
