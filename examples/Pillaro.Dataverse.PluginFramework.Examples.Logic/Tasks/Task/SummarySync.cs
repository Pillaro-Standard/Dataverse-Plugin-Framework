using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Tasks;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Tasks.Task
{
    public class SummarySync(IServiceProvider serviceProvider, TaskContext taskContext) : TaskBase<Logic.Task>(serviceProvider, taskContext)
    {
        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Postoperation)
                .WithMessages(["Create", "Update"])
                .ForEntity(ContextEntity.LogicalName)
                .HasPreImageWhen(ctx => ctx.Message == "Update")
                .HasPostImageWhen(ctx => ctx.Message == "Update")
                .EntityWithAtLeastOneAttributeWhen(
                    ctx => ctx.Message == "Update",
                    ContextEntity,
                    nameof(ContextEntity.RegardingObjectId), 
                    nameof(ContextEntity.ScheduledEnd), 
                    nameof(ContextEntity.ScheduledStart), 
                    nameof(ContextEntity.StateCode),
                    nameof(ContextEntity.StatusCode));
        }

        protected override void DoExecute()
        {
            var currentRegarding = ResolveCurrentRegarding();
            var previousRegarding = ResolvePreviousRegarding();

            if (IsSupportedEntity(currentRegarding))
            {
                RecalculateDescription(currentRegarding);
            }

            if (IsSupportedEntity(previousRegarding) && !AreSameReference(currentRegarding, previousRegarding))
            {
                RecalculateDescription(previousRegarding);
            }
        }

        private EntityReference ResolveCurrentRegarding()
        {
            if (TaskContext.Message == "Create")
                return ContextEntity.RegardingObjectId;

            return PostImage?.RegardingObjectId;
        }

        private EntityReference ResolvePreviousRegarding()
        {
            if (TaskContext.Message != "Update")
                return null;

            return PreImage?.RegardingObjectId;
        }

        private static bool IsSupportedEntity(EntityReference entityRef)
        {
            if (entityRef == null)
                return false;

            return entityRef.LogicalName == Logic.Contact.EntityLogicalName;
        }

        private static bool AreSameReference(EntityReference a, EntityReference b)
        {
            if (a == null || b == null)
                return false;

            return a.LogicalName == b.LogicalName && a.Id == b.Id;
        }

        private void RecalculateDescription(EntityReference regarding)
        {
            var relatedTasks = DataServiceProvider.Admin
                .Query<Logic.Task>()
                .Where(t => t.RegardingObjectId.Id == regarding.Id)
                .Select(t => new Logic.Task
                {
                    ActivityId = t.ActivityId,
                    StateCode = t.StateCode,
                    ScheduledEnd = t.ScheduledEnd,
                    ActualEnd = t.ActualEnd
                })
                .ToList();

            DateTime? latestPlannedDate = null;
            DateTime? latestCompletedDate = null;

            foreach (var task in relatedTasks)
            {
                if (task.StateCode == task_statecode.Open && task.ScheduledEnd.HasValue)
                {
                    if (!latestPlannedDate.HasValue || task.ScheduledEnd.Value > latestPlannedDate.Value)
                        latestPlannedDate = task.ScheduledEnd.Value;
                }

                if (task.StateCode == task_statecode.Completed && task.ActualEnd.HasValue)
                {
                    if (!latestCompletedDate.HasValue || task.ActualEnd.Value > latestCompletedDate.Value)
                        latestCompletedDate = task.ActualEnd.Value;
                }
            }

            var description = BuildDescription(latestPlannedDate, latestCompletedDate);
            OrganizationServiceProvider.Admin.Update(new Logic.Contact
            {
                Id = regarding.Id,
                Description = description,
            });
         
            AddLogMessageLine($"Updated {regarding.LogicalName} {regarding.Id} Description.");
        }

        private static string BuildDescription(DateTime? latestPlanned, DateTime? latestCompleted)
        {
            var lines = new List<string>();

            if (latestPlanned.HasValue)
                lines.Add($"Last planned activity: {latestPlanned.Value:yyyy-MM-dd}");

            if (latestCompleted.HasValue)
                lines.Add($"Last completed activity: {latestCompleted.Value:yyyy-MM-dd}");

            return string.Join(Environment.NewLine, lines);
        }
    }
}