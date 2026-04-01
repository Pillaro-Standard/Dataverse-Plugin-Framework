using Microsoft.Xrm.Sdk.Query;
using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Tasks;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using System;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Opportunity
{
    public class ValidateOwnerChangeTask : TaskBase<Logic.Opportunity>
    {
        public ValidateOwnerChangeTask(IServiceProvider serviceProvider, TaskContext taskContext)
            : base(serviceProvider, taskContext)
        {
        }

        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                // 1) Context filters
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Prevalidation)
                .WithMessage("Update")

                // 2) Entity scope
                .ForEntity(ContextEntity.LogicalName)

                // 3) Image requirement – PreImage is needed for original ownerid, statecode, closeprobability, estimatedclosedate
                .HasPreImage()

                // 4) Attribute presence – run only when ownerid is in the target
                .EntityWithAtLeastOneAttribute(ContextEntity, nameof(ContextEntity.OwnerId))

                // Rule 1: skip if ownerid didn't actually change (compare target vs pre-image)
                .WithBreakValidation(
                    "OwnerId has not changed.",
                    ctx => ContextEntity.OwnerId != null
                           && PreImage.OwnerId != null
                           && ContextEntity.OwnerId.Id != PreImage.OwnerId.Id)

                // Rule 2: skip if opportunity is closed
                .WithBreakValidation(
                    "Opportunity is not open.",
                    ctx => PreImage.StateCode.HasValue
                           && PreImage.StateCode.Value == OpportunityState.Open)

                // Rule 5: closeprobability >= 80 (cheapest data check, no query)
                .ThrowWithError(
                    "Cannot change the Opportunity owner when close probability is 80% or higher.",
                    ctx => !PreImage.CloseProbability.HasValue || PreImage.CloseProbability.Value < 80)

                // Rule 4: products + estimated close date within 7 days (one query for products)
                .ThrowWithError(
                    "Cannot change the Opportunity owner when the deal has products and the expected close date is within 7 days.",
                    ctx => !IsNearCloseWithProducts())

                // Rule 3: open activities assigned to another user (most expensive query)
                .ThrowWithError(
                    "Cannot change the Opportunity owner because there are open activities assigned to another user.",
                    ctx => !HasOpenActivitiesOwnedByOthers());
        }

        protected override void DoExecute()
        {
            AddLogMessageLine("Owner change validation passed.");
        }

        private bool IsNearCloseWithProducts()
        {
            if (!PreImage.EstimatedCloseDate.HasValue)
                return false;

            var daysUntilClose = (PreImage.EstimatedCloseDate.Value - DateTime.UtcNow).TotalDays;
            if (daysUntilClose > 7)
                return false;

            var query = new QueryExpression("opportunityproduct")
            {
                ColumnSet = new ColumnSet(false),
                TopCount = 1,
                Criteria = new FilterExpression()
            };
            query.Criteria.AddCondition("opportunityid", ConditionOperator.Equal, TaskContext.PrimaryEntityId);

            var results = DataServiceProvider.Admin.OrganizationService.RetrieveMultiple(query);
            return results.Entities.Count > 0;
        }

        private bool HasOpenActivitiesOwnedByOthers()
        {
            var newOwnerId = ContextEntity.OwnerId.Id;

            var query = new QueryExpression("activitypointer")
            {
                ColumnSet = new ColumnSet(false),
                TopCount = 1,
                Criteria = new FilterExpression()
            };
            query.Criteria.AddCondition("regardingobjectid", ConditionOperator.Equal, TaskContext.PrimaryEntityId);
            query.Criteria.AddCondition("statecode", ConditionOperator.Equal, 0); // Open
            query.Criteria.AddCondition("ownerid", ConditionOperator.NotEqual, newOwnerId);

            var results = DataServiceProvider.Admin.OrganizationService.RetrieveMultiple(query);
            return results.Entities.Count > 0;
        }
    }
}