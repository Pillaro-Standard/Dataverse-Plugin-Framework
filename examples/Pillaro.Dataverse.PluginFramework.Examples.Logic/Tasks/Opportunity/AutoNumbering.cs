using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.AutoNumbering;
using Pillaro.Dataverse.PluginFramework.Tasks;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using System;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Opportunity
{
    public class Autonumbering : TaskBase<Logic.Opportunity>
    {
        public Autonumbering(IServiceProvider serviceProvider, TaskContext taskContext) : base(serviceProvider, taskContext)
        {
        }

        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Preoperation)
                .WithMessages(new[] { "Create" })
                .ForEntity(ContextEntity.LogicalName)
                ;
        }

        protected override void DoExecute()
        {
            AutoNumberingService autonumService = new(OrganizationServiceProvider.Admin);
            var response = autonumService.GetTransactionAutoNumber(TaskContext.PrimaryEntityName, ContextEntity.Id, null, null);

            AddLogMessageLine($"AutoNumbering response: {response.Number}");

            DataServiceProvider.Admin.UpdateOutsideTransaction(response.Request.Target);

            ContextEntity.Name = $"{response.Number}: {ContextEntity.Name}";
        }
    }
}