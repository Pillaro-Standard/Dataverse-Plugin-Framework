using Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Task;
using Pillaro.Dataverse.PluginFramework.PluginRegistrations;
using Pillaro.Dataverse.PluginFramework.Plugins;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Plugins
{
    public class TaskPlugin : PluginBase
    {
        private const string SolutionName = "PillaroPluginFrameworkExamples";

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
                .InSolution(SolutionName)
                .WithName("Pillaro Examples Pre Create Task")
                .Rank(1)
                .WithFilteringAttributes("subject");

            registration
                .OnCreate<Task>("a14d984d-0f31-f111-88b4-000d3ab2695d")
                .PostOperation()
                .Synchronous()
                .InSolution(SolutionName)
                .WithName("Pillaro Examples Post Create Task")
                .Rank(2);

            registration
                .OnUpdate<Task>("b24d984d-0f31-f111-88b4-000d3ab2695d")
                .PostOperation()
                .Synchronous()
                .InSolution(SolutionName)
                .WithName("Pillaro Examples Post Update Task")
                .Rank(3)
                .WhenChanged(
                    "regardingobjectid",
                    "scheduledend",
                    "statecode",
                    "statuscode")
                .WithPreImage(
                    "b34d984d-0f31-f111-88b4-000d3ab2695d",
                    "image",
                    "regardingobjectid",
                    "scheduledend",
                    "statecode",
                    "statuscode",
                    "actualend")
                .WithPostImage(
                    "b44d984d-0f31-f111-88b4-000d3ab2695d",
                    "image",
                    "regardingobjectid",
                    "scheduledend",
                    "statecode",
                    "statuscode",
                    "actualend");
        }
    }
}