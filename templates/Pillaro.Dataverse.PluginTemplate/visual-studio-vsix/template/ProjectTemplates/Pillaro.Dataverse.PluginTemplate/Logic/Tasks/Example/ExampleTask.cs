using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Tasks;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using System;

namespace $safeprojectname$.Tasks.Example;

public class ExampleTask(IServiceProvider serviceProvider, TaskContext taskContext) : TaskBase<Entity>(serviceProvider, taskContext)
{
    private static readonly string[] Attributes =
    [
        "firstname",
        "lastname",
    ];

    protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
    {
        return validator
            .WithMode(PluginMode.Synchronous)
            .WithStage(PluginStage.Prevalidation)
            .WithMessages([ "Create", "Update" ])
            .ForEntity(TaskContext.PrimaryEntityName)
            .EntityWithAtLeastOneAttribute(ContextEntity, Attributes);
    }

    protected override void DoExecute()
    {
        AddLogMessageLine("Hello World from Pillaro Dataverse Plugin Framework.");
    }
}