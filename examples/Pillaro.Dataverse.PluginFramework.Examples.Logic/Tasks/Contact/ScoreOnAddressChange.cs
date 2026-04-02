using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Data;
using Pillaro.Dataverse.PluginFramework.Tasks;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using System;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Contact
{
    public class ScoreOnAddressChange : TaskBase<Logic.Contact>
    {
        private static readonly string[] AddressAttributes =
        {
            "address1_line1",
            "address1_line2",
            "address1_line3",
            "address1_city",
            "address1_postalcode",
            "address1_stateorprovince",
            "address1_country"
        };

        public ScoreOnAddressChange(
            IServiceProvider serviceProvider,
            TaskContext taskContext
        ) : base(serviceProvider, taskContext)
        {
        }

        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Preoperation)
                .WithMessages(new[] { "Create", "Update" })
                .ForEntity(ContextEntity.LogicalName)
                .EntityWithAtLeastOneAttribute(ContextEntity, AddressAttributes);
        }

        protected override void DoExecute()
        {
            int? currentScore = null;

            if (TaskContext.Message.Equals("Update", StringComparison.InvariantCultureIgnoreCase))
            {
                currentScore = DataServiceProvider.Admin.Query<Logic.Contact>()
                    .Where(c => c.Id == TaskContext.PrimaryEntityId)
                    .Select(c => c.NumberOfChildren)
                    .FirstOrDefault();
            }

            // "random" score (0-100)
            Random rnd = new(Guid.NewGuid().GetHashCode());
            var newScore = rnd.Next(0, 101);

            if (currentScore.HasValue && newScore == currentScore.Value)
                newScore = (newScore + 1) % 101;

            AddLogMessageLine($"CurrentScore={currentScore?.ToString() ?? "null"}, newScore={newScore}");

            ContextEntity.NumberOfChildren = newScore;
        }
    }
}