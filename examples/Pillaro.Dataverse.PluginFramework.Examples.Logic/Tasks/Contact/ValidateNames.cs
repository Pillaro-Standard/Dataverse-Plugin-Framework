using Pillaro.Dataverse.PluginFramework.Examples.Logic.Features.ForbiddenNames;
using Pillaro.Dataverse.PluginFramework.Exceptions;
using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Tasks;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using System;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Contact
{
    public class ValidateNames(IServiceProvider serviceProvider, TaskContext taskContext) : TaskBase<Logic.Contact>(serviceProvider, taskContext)
    {
        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Prevalidation)
                .WithMessages(["Create", "Update"])
                .ForEntity(ContextEntity.LogicalName)
                .EntityWithAtLeastOneAttribute(ContextEntity, Logic.Contact.Fields.FirstName, Logic.Contact.Fields.LastName);
        }

        protected override void DoExecute()
        {
            var forbiddenWords = new CustomerForbiddenNameService(SettingService).GetForbiddenNames();
            
            AddLogMessageLine($"Forbidden words: {string.Join(",",forbiddenWords)}");
            
            if (ContextEntity.Contains(Logic.Contact.Fields.FirstName) &&
               forbiddenWords.FindIndex(x => x.Equals(ContextEntity.FirstName, StringComparison.InvariantCultureIgnoreCase)) != -1)
            {
                var msg = "First name is forbidden word, please write correct your first name";
                AddLogMessageLine(msg);
                //expected business outcome: the user sees the message, the task is logged as Success/Info
                throw new DataverseValidationException(msg);
            }

            if (ContextEntity.Contains(Logic.Contact.Fields.LastName) &&
                forbiddenWords.FindIndex(x => x.Equals(ContextEntity.LastName, StringComparison.InvariantCultureIgnoreCase)) != -1)
            {
                var msg = "Last name is forbidden word, please write correct your last name";
                AddLogMessageLine(msg);

                //expected business outcome: the user sees the message, the task is logged as Success/Info
                throw new DataverseValidationException(msg);
            }
        }
    }
}