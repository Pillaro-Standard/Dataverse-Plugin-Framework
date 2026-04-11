using System;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Task
{
    public class AutoNumbering(IServiceProvider serviceProvider, TaskContext taskContext) : TaskBase<Logic.Task>(serviceProvider, taskContext)
    {
        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Preoperation)
                .WithMessages(["Create"])
                .ForEntity(ContextEntity.LogicalName)
                ;
        }

        protected override void DoExecute()
        {
            AutoNumberingService autonumService = new(OrganizationServiceProvider.Admin);
            var response = autonumService.GetTransactionAutoNumber(TaskContext.PrimaryEntityName, ContextEntity.Id, null, null);

            AddLogMessageLine($"AutoNumbering response: {response.Number}");

            DataServiceProvider.Admin.UpdateOutsideTransaction(response.Request.Target);

            ContextEntity.Subject = $"{response.Number}: {ContextEntity.Subject}";
        }
    }
}