using Pillaro.Dataverse.PluginFramework.Examples.Logic.Features.ForbiddenNames;
using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Data;
using Pillaro.Dataverse.PluginFramework.Exceptions;
using Pillaro.Dataverse.PluginFramework.Tasks;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using System;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Contact
{
    public class ForbiddenNamesTask : TaskBase<Logic.Contact>
    {
        public ForbiddenNamesTask(IServiceProvider serviceProvider, TaskContext taskContext) : base(serviceProvider, taskContext)
        {
        }

        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Prevalidation)
                .WithMessages(new[] { "Create", "Update" })
                .ForEntity(ContextEntity.LogicalName)
                .EntityWithAtLeastOneAttribute(ContextEntity, "firstname", "lastname");
        }

        protected override void DoExecute()
        {
            var forbiddenWords = new CustomerForbiddenNameService(SettingService).GetForbiddenNames();

            var conts = DataServiceProvider.Admin.Query<Logic.Contact>()
                .Select(c => new { c.FirstName, c.LastName })
                .ToList();

            AddLogMessageLine($"Count: ${conts.Count}");


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