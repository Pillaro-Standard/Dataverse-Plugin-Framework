using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Pillaro.Dataverse.PluginFramework.Plugins.Features.Autonumbering;
using Pillaro.Dataverse.PluginFramework.Tasks;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;

namespace Pillaro.Dataverse.PluginFramework.Plugins.Tasks.Autonumbering
{
    public class GetAutoNumber : TaskBase<Entity>
    {
        private const int RetryAttempts = 5;
        private const int RetryDelayMilliseconds = 300;

        private readonly AutoNumberFormatRenderer _renderer;

        public GetAutoNumber(IServiceProvider serviceProvider, TaskContext taskContext) : base(serviceProvider, taskContext)
        {
            _renderer = new AutoNumberFormatRenderer();
        }

        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator.WithMode(PluginMode.Synchronous).WithStage(PluginStage.Postoperation).WithMessages("pl_AutoNumbering_GetNewNumber");
        }

        protected override void DoExecute()
        {
            var inputParameters = TaskContext.PluginExecutionContext.InputParameters;
            var entityInput = inputParameters.ContainsKey("Entity") ? inputParameters["Entity"] : null;

            if (!(entityInput is EntityReference entityReference))
                throw new InvalidPluginExecutionException($"InputParameters['Entity'] must be EntityReference. Actual: {entityInput?.GetType().FullName ?? "null"}");

            var entityName = entityReference.LogicalName;
            var entityId = entityReference.Id;
            var parentEntityId = inputParameters.ContainsKey("ParentEntityId") ? inputParameters["ParentEntityId"] as Guid? : null;

            AddLogMessageLine($"GetAutoNumber started. EntityName='{entityName}', EntityId='{entityId}', ParentEntityId='{parentEntityId}'.");

            var number = GenerateNumber(entityName, entityId, parentEntityId);

            if (TaskContext.PluginExecutionContext.OutputParameters != null)
                TaskContext.PluginExecutionContext.OutputParameters["Number"] = number;

            AddLogMessageLine($"GetAutoNumber finished. Output Number='{number}'.");
        }

        private string GenerateNumber(string entityName, Guid entityId, Guid? parentEntityId)
        {
            for (var attempt = 1; attempt <= RetryAttempts; attempt++)
            {
                try
                {
                    return GenerateNumberOnce(entityName, entityId, parentEntityId, attempt);
                }
                catch (FaultException<OrganizationServiceFault> ex) when (IsRetryableConcurrencyFault(ex))
                {
                    AddLogMessageLine($"Concurrency conflict. Attempt='{attempt}/{RetryAttempts}'. Fault='{ex.Detail?.Message ?? ex.Message}'.");

                    if (attempt >= RetryAttempts)
                        throw new InvalidOperationException($"Auto number generation failed after {RetryAttempts} attempts due to concurrency conflicts.", ex);

                    Thread.Sleep(RetryDelayMilliseconds);
                }
            }

            throw new InvalidOperationException("Auto number generation failed.");
        }

        private string GenerateNumberOnce(string entityName, Guid entityId, Guid? parentEntityId, int attempt)
        {
            var service = OrganizationServiceProvider.Admin;
            var primaryConfig = LoadPrimaryConfig(service, entityName);

            if (primaryConfig == null)
                throw new InvalidPluginExecutionException($"Primary autonumbering configuration does not exist for entity '{entityName}'.");

            AddLogMessageLine($"PrimaryConfig.Id='{primaryConfig.Id}', ParentLookupAttribute='{primaryConfig.pl_ParentLookupAttribute ?? "null"}'");

            var currentConfig = ResolveCurrentConfig(service, entityName, parentEntityId, primaryConfig);
            var effectiveConfig = currentConfig.pl_UseParentConfiguration == crd8e_pl_autonumbering_pl_useparentconfiguration.Ano ? primaryConfig : currentConfig;

            if (string.IsNullOrWhiteSpace(effectiveConfig.pl_FormatString))
                throw new InvalidPluginExecutionException($"pl_FormatString is empty for configuration Id='{effectiveConfig.Id}', entity '{entityName}'.");

            var nextNumber = (currentConfig.pl_Number ?? 0) + 1;
            var generated = BuildNumber(service, entityName, entityId, nextNumber, effectiveConfig);

            AddLogMessageLine($"Generated='{generated}', NextCounter='{nextNumber}', CurrentAutoNumId='{currentConfig.Id}', ConfigId='{effectiveConfig.Id}', Attempt='{attempt}'.");

            SaveNextCounter(service, currentConfig, nextNumber);
            return generated;
        }

        private pl_AutoNumbering ResolveCurrentConfig(IOrganizationService service, string entityName, Guid? parentEntityId, pl_AutoNumbering primaryConfig)
        {
            if (string.IsNullOrWhiteSpace(primaryConfig.pl_ParentLookupAttribute))
                return primaryConfig;

            if (parentEntityId == null || parentEntityId == Guid.Empty)
                throw new InvalidPluginExecutionException($"ParentEntityId must be provided for entity '{entityName}' because parent lookup '{primaryConfig.pl_ParentLookupAttribute}' is configured.");

            var childConfig = LoadChildConfig(service, entityName, parentEntityId.Value);
            if (childConfig != null)
                return childConfig;

            AddLogMessageLine($"Child config does not exist for ParentLookupId='{parentEntityId}'. Creating new.");

            var newChild = new pl_AutoNumbering
            {
                pl_EntityName = primaryConfig.pl_EntityName,
                pl_ParentLookupId = parentEntityId.Value.ToString(),
                pl_Number = 0,
                pl_ParentAutoNumberingId = primaryConfig.ToEntityReference(),
                pl_UseParentConfiguration = crd8e_pl_autonumbering_pl_useparentconfiguration.Ano
            };

            try
            {
                newChild.Id = service.Create(newChild);
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                AddLogMessageLine($"Child config create collision for ParentLookupId='{parentEntityId}'. Fault='{ex.Detail?.Message ?? ex.Message}'.");
                childConfig = LoadChildConfig(service, entityName, parentEntityId.Value);
                if (childConfig != null)
                    return childConfig;
                throw;
            }

            return service.Retrieve(newChild.LogicalName, newChild.Id, new ColumnSet(true)).ToEntity<pl_AutoNumbering>();
        }

        private string BuildNumber(IOrganizationService service, string entityName, Guid entityId, int nextNumber, pl_AutoNumbering config)
        {
            var rendererConfig = new AutoNumberFormatRenderer.FormatConfig(config.pl_DigitCount ?? 0, config.pl_DateFormat1, config.pl_DateFormat2, config.pl_DateFormat3);
            var plan = _renderer.Analyze(config.pl_FormatString, rendererConfig, nextNumber);

            if (!plan.HasDynamicTokens)
                return plan.PartialFormat;

            var rootEntity = service.Retrieve(entityName, entityId, new ColumnSet(plan.RootAttributes.ToArray()));
            if (rootEntity == null)
                throw new InvalidPluginExecutionException($"Record '{entityName}' Id='{entityId}' could not be loaded.");

            var parentEntities = LoadParentEntities(service, rootEntity, plan.ParentLookups);
            return _renderer.Render(plan, rootEntity, parentEntities);
        }

        private static Dictionary<string, Entity> LoadParentEntities(IOrganizationService service, Entity rootEntity, Dictionary<string, HashSet<string>> parentLookups)
        {
            var result = new Dictionary<string, Entity>(StringComparer.OrdinalIgnoreCase);

            foreach (var lookup in parentLookups)
            {
                if (!rootEntity.Contains(lookup.Key) || !(rootEntity[lookup.Key] is EntityReference parentRef))
                    throw new InvalidPluginExecutionException($"Lookup '{lookup.Key}' is missing or not an EntityReference on '{rootEntity.LogicalName}' Id='{rootEntity.Id}'.");

                var parentEntity = service.Retrieve(parentRef.LogicalName, parentRef.Id, new ColumnSet(lookup.Value.ToArray()));
                if (parentEntity == null)
                    throw new InvalidPluginExecutionException($"Could not load parent '{parentRef.LogicalName}' Id='{parentRef.Id}' for lookup '{lookup.Key}'.");

                result[lookup.Key] = parentEntity;
            }

            return result;
        }

        private static void SaveNextCounter(IOrganizationService service, pl_AutoNumbering config, int nextNumber)
        {
            service.Execute(new UpdateRequest
            {
                Target = new pl_AutoNumbering { Id = config.Id, pl_Number = nextNumber, RowVersion = config.RowVersion },
                ConcurrencyBehavior = ConcurrencyBehavior.IfRowVersionMatches
            });
        }

        private pl_AutoNumbering LoadPrimaryConfig(IOrganizationService service, string entityName)
        {
            using (var svc = new ServiceContext(service))
            {
                svc.MergeOption = Microsoft.Xrm.Sdk.Client.MergeOption.NoTracking;
                var result = svc.pl_AutoNumberingSet.Where(x => x.pl_EntityName == entityName && x.pl_ParentAutoNumberingId == null && x.pl_ParentLookupId == null).Take(2).ToList();

                if (result.Count > 1)
                    throw new InvalidPluginExecutionException($"More than one primary autonumbering configuration exists for entity '{entityName}'.");

                return result.Count == 0 ? null : result[0];
            }
        }

        private pl_AutoNumbering LoadChildConfig(IOrganizationService service, string entityName, Guid parentLookupId)
        {
            using (var svc = new ServiceContext(service))
            {
                svc.MergeOption = Microsoft.Xrm.Sdk.Client.MergeOption.NoTracking;
                var parentLookupIdText = parentLookupId.ToString();
                var result = svc.pl_AutoNumberingSet.Where(x => x.pl_EntityName == entityName && x.pl_ParentLookupId == parentLookupIdText).Take(2).ToList();

                if (result.Count > 1)
                    throw new InvalidPluginExecutionException($"More than one child configuration exists for entity '{entityName}' and ParentLookupId='{parentLookupId}'.");

                return result.Count == 0 ? null : result[0];
            }
        }

        private static bool IsRetryableConcurrencyFault(FaultException<OrganizationServiceFault> ex)
        {
            var errorCode = ex.Detail?.ErrorCode;
            return errorCode == -2147088254 || errorCode == -2147088253;
        }
    }
}