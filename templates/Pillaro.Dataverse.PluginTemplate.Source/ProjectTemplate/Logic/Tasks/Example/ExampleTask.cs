using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Tasks;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using System;

namespace $safeprojectname$.Logic.Tasks.Example;

public class ExampleTask(IServiceProvider serviceProvider, TaskContext taskContext) : TaskBase<Entity>(serviceProvider, taskContext)
{
    // Logical names are written as string literals because this template ships without
    // early-bound entity classes - those depend on the target environment and are generated
    // by Tools/EarlyBound in the Logic project. Once the classes exist, change TaskBase<Entity>
    // to TaskBase<Contact> and replace these literals with Contact.Fields.FirstName and
    // Contact.Fields.LastName. Never use nameof(...) for an attribute name: it returns the
    // property name, not the logical name, and a wrong name fails silently at runtime.
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
