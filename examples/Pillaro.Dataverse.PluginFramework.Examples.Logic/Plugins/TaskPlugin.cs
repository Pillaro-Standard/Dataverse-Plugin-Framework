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
                .WithName("Pillaro Examples Pre Create Task")
                .Rank(1)
                // Typed attribute selection is available on Create steps too, not only on Update.
                .WithFilteringAttributes(t => t.Subject);

            registration
                .OnCreate<Task>("a14d984d-0f31-f111-88b4-000d3ab2695d")
                .PostOperation()
                .Synchronous()
                .WithName("Pillaro Examples Post Create Task")
                .Rank(2);

            registration
                .OnUpdate<Task>("b24d984d-0f31-f111-88b4-000d3ab2695d")
                .PostOperation()
                .Synchronous()
                .WithName("Pillaro Examples Post Update Task")
                .Rank(3)
                .WhenChanged(
                    t => t.RegardingObjectId,
                    t => t.ScheduledEnd,
                    t => t.StateCode,
                    t => t.StatusCode)
                // A pre-image and a post-image with the same key and the same attributes are one
                // Both image. SummarySync still reads it as PreEntityImages["image"] and
                // PostEntityImages["image"], so the task code is unchanged.
                .WithBothImage(
                    "b34d984d-0f31-f111-88b4-000d3ab2695d",
                    "image",
                    t => t.RegardingObjectId,
                    t => t.ScheduledEnd,
                    t => t.StateCode,
                    t => t.StatusCode,
                    t => t.ActualEnd);
        }
    }
}