using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Tasks;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using System;

namespace Contoso.Dataverse.PatchingProject.Logic.Tasks.Contact
{
    public class ValidateContactName : TaskBase<Entity>
    {
        public ValidateContactName(IServiceProvider serviceProvider, TaskContext taskContext)
            : base(serviceProvider, taskContext)
        {
        }

        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Prevalidation)
                .WithMessages(new[] { "Create", "Update" })
                .ForEntity("contact")
                .EntityWithAtLeastOneAttribute(ContextEntity, "firstname", "lastname");
        }

        protected override void DoExecute()
        {
            AddLogMessageLine("Sample contact validation executed.");

            // Replace this sample with project-specific validation or business logic.
            // Keep validation in AddValidations() and executable behavior in DoExecute().
        }
    }
}
