using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Tasks;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using System;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Opportunity
{
    public class ScoreOpportunity : TaskBase<Logic.Opportunity>
    {
        public ScoreOpportunity(IServiceProvider serviceProvider, TaskContext taskContext) : base(serviceProvider, taskContext)
        {
        }

        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Preoperation)
                .WithMessages(new[] { "Create", "Update" })
                .ForEntity(ContextEntity.LogicalName);
        }

        protected override void DoExecute()
        {
            var attrName = nameof(ContextEntity.Description).ToLower();

            int? currentScore = null;

            
            // Random score 0-100, seeded by Guid for variability
            Random rnd = new Random(Guid.NewGuid().GetHashCode());
            var newScore = rnd.Next(0, 101);

            if (currentScore.HasValue && newScore == currentScore.Value)
                newScore = (newScore + 1) % 101;

            AddLogMessageLine($"CurrentScore={currentScore?.ToString() ?? "null"}, newScore={newScore}");

            // Store score into description
            ContextEntity.Description = newScore.ToString();
        }
    }
}