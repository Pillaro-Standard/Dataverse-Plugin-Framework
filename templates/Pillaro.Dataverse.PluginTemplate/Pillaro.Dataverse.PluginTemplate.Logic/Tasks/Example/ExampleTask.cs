using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Tasks;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using System;

namespace Pillaro.Dataverse.PluginTemplate.Logic.Tasks.Example
{
    public class ExampleTask : TaskBase<Entity>
    {
        public ExampleTask(IServiceProvider serviceProvider, TaskContext taskContext)
            : base(serviceProvider, taskContext)
        {
        }

        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Prevalidation)
                .WithMessages(new[] { "Create" })
                .ForEntity(TaskContext.PrimaryEntityName);
        }

        protected override void DoExecute()
        {
            AddLogMessageLine("Hello World from Pillaro Dataverse Plugin Framework.");
        }
    }
}
