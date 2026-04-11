using Pillaro.Dataverse.PluginFramework.Examples.Logic.Features.ForbiddenNames;
using System;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Contact
{
    public class ValidateNames : TaskBase<Logic.Contact>
    {
        public ValidateNames(IServiceProvider serviceProvider, TaskContext taskContext) : base(serviceProvider, taskContext)
        {
        }

        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Prevalidation)
                .WithMessages(["Create", "Update"])
                .ForEntity(ContextEntity.LogicalName)
                .EntityWithAtLeastOneAttribute(ContextEntity, nameof(ContextEntity.FirstName), nameof(ContextEntity.LastName));
        }

        protected override void DoExecute()
        {
            var forbiddenWords = new CustomerForbiddenNameService(SettingService).GetForbiddenNames();

            AddLogMessageLine($"Forbidden words: {string.Join(",",forbiddenWords)}");

            if (ContextEntity.Contains(nameof(ContextEntity.FirstName).ToLower()) &&
               forbiddenWords.FindIndex(x => x.Equals(ContextEntity.FirstName, StringComparison.InvariantCultureIgnoreCase)) != -1)
            {
                var msg = "First name is forbidden word, please write correct your first name";
                AddLogMessageLine(msg);
                //this exception  will be logged as warning, but user will see the error message
                throw new DataverseValidationException(msg);
            }

            if (ContextEntity.Contains(nameof(ContextEntity.LastName).ToLower()) &&
                forbiddenWords.FindIndex(x => x.Equals(ContextEntity.LastName, StringComparison.InvariantCultureIgnoreCase)) != -1)
            {
                var msg = "Last name is forbidden word, please write correct your last name";
                AddLogMessageLine(msg);

                //this exception  will be logged
                throw new DataverseValidationException(msg);
            }
        }
    }
}