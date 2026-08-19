using Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Task;
using Pillaro.Dataverse.PluginFramework.PluginRegistrations;
using Pillaro.Dataverse.PluginFramework.Plugins;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Plugins
{
    public class TaskPlugin : PluginBase
    {
        public TaskPlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
            RegisterTask<Tasks.Task.AutoNumbering>(PluginStage.Preoperation, ["Create"], Task.EntityLogicalName, PluginMode.Synchronous);
            RegisterTask<SummarySync>(PluginStage.Postoperation, ["Create", "Update"], Task.EntityLogicalName, PluginMode.Synchronous);
        }

        public override void Register(IPluginRegistration registration)
        {
            registration
                .OnCreate<Task>("f94d984d-0f31-f111-88b4-000d3ab2695d")
                .PreOperation()
                .Synchronous()
                .WithName($"{StepPrefix} task Create PreOperation Synchronous")
                .Rank(1)
                .WithFilteringAttributes(Task.Fields.Subject);

            registration
                .OnCreate<Task>("a14d984d-0f31-f111-88b4-000d3ab2695d")
                .PostOperation()
                .Synchronous()
                .WithName($"{StepPrefix} task Create PostOperation Synchronous")
                .Rank(2);

            registration
                .OnUpdate<Task>("b24d984d-0f31-f111-88b4-000d3ab2695d")
                .PostOperation()
                .Synchronous()
                .WithName($"{StepPrefix} task Update PostOperation Synchronous")
                .Rank(3)
                .WhenChanged(
                    Task.Fields.RegardingObjectId,
                    Task.Fields.ScheduledEnd,
                    Task.Fields.StateCode,
                    Task.Fields.StatusCode)
                .WithPreImage(
                    "b34d984d-0f31-f111-88b4-000d3ab2695d",
                    "image",
                    Task.Fields.RegardingObjectId,
                    Task.Fields.ScheduledEnd,
                    Task.Fields.StateCode,
                    Task.Fields.StatusCode,
                    Task.Fields.ActualEnd)
                .WithPostImage(
                    "b44d984d-0f31-f111-88b4-000d3ab2695d",
                    "image",
                    Task.Fields.RegardingObjectId,
                    Task.Fields.ScheduledEnd,
                    Task.Fields.StateCode,
                    Task.Fields.StatusCode,
                    Task.Fields.ActualEnd);
        }
    }
}