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
    public class ArchiveDeletedContact : TaskBase<Logic.Contact>
    {
        public ArchiveDeletedContact(IServiceProvider serviceProvider, TaskContext taskContext)
            : base(serviceProvider, taskContext)
        {
        }

        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Postoperation)
                .WithMessage(DataverseMessages.Delete)
                .ForEntity(Logic.Contact.EntityLogicalName)
                .HasPreImage();
        }

        protected override void DoExecute()
        {
            // There is no context entity on Delete, the pre-image carries the deleted values.
            var parentCustomer = PreImage.ParentCustomerId;

            if (parentCustomer == null
                || !string.Equals(parentCustomer.LogicalName, Logic.Account.EntityLogicalName, StringComparison.OrdinalIgnoreCase))
            {
                AddLogMessageLine("Deleted contact has no parent account, nothing to record.");
                return;
            }

            var deletedContact = BuildContactName(PreImage);

            AddLogMessageLine($"Recording deleted contact '{deletedContact}' on account '{parentCustomer.Id}'.");

            // Queued instead of written directly, so the account is written once even when
            // several tasks of this execution contribute to it.
            TaskContext.AddEntityToUpdate(new Logic.Account
            {
                Id = parentCustomer.Id,
                Description = $"Deleted contact: {deletedContact}"
            });
        }

        private static string BuildContactName(Logic.Contact contact)
        {
            var name = $"{contact.FirstName} {contact.LastName}".Trim();

            return string.IsNullOrWhiteSpace(name) ? contact.Id.ToString("D") : name;
        }
    }
}
