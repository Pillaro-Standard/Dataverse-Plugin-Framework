using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Pillaro.Dataverse.PluginFramework.Data.Query;
using Pillaro.Dataverse.PluginFramework.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Threading;

namespace Pillaro.Dataverse.PluginFramework.Data;

/// <summary>
/// Central entry point for Dataverse operations over a single security context.
/// Create two instances when both User and Admin contexts are needed.
/// </summary>
public class DataService : IDataService
{
    private readonly ConcurrentDictionary<Guid, List<OrganizationRequest>> _multipleRequests = new();
    private readonly Guid _instanceId;
    private readonly int _multipleRequestBatchSize;
    private readonly int _waitOnAsyncProcessMaxLoop;

    public IOrganizationService OrganizationService { get; }

    public DataService(IOrganizationService organizationService, int multipleRequestBatchSize = 1000, int waitOnAsyncProcessMaxLoop = 40)
    {
        OrganizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
        _multipleRequestBatchSize = multipleRequestBatchSize;
        _waitOnAsyncProcessMaxLoop = waitOnAsyncProcessMaxLoop;
        _instanceId = Guid.NewGuid();

        Debug.WriteLine($"DataService.InstanceId: {_instanceId}");
    }

    public Guid GetInstanceId()
    {
        return _instanceId;
    }

    public int GetMultipleRequestBatchSize()
    {
        return _multipleRequestBatchSize;
    }

    public EntityQuery<TEntity> Query<TEntity>() where TEntity : Entity
    {
        return new EntityQuery<TEntity>(() => new OrganizationServiceContext(OrganizationService));
    }

    public TDataServiceRepository GetRepository<TDataServiceRepository>() where TDataServiceRepository : DataServiceRepository
    {
        return (TDataServiceRepository)Activator.CreateInstance(typeof(TDataServiceRepository), this);
    }

    public WhoAmIResponse WhoAmI()
    {
        return (WhoAmIResponse)OrganizationService.Execute(new WhoAmIRequest());
    }

    public void WaitOnAsyncProcess(Guid entityId, int? numberOfAttempts = null)
    {
        var attempts = 0;
        var maxAttempts = numberOfAttempts ?? _waitOnAsyncProcessMaxLoop;

        while (true)
        {
            Thread.Sleep(1000);

            QueryExpression query = new("asyncoperation")
            {
                ColumnSet = new ColumnSet("statecode"),
                Criteria = new FilterExpression()
            };

            query.Criteria.AddCondition("regardingobjectid", ConditionOperator.Equal, entityId);
            query.Criteria.AddCondition("statecode", ConditionOperator.NotEqual, 3);

            var operations = OrganizationService.RetrieveMultiple(query);
            if (operations == null || operations.Entities.Count == 0)
            {
                return;
            }

            var operation = operations.Entities[0];
            if (!operation.Contains("statecode") || operation["statecode"] == null)
            {
                return;
            }

            var stateCode = ((OptionSetValue)operation["statecode"]).Value;
            if (stateCode == 3)
            {
                return;
            }

            attempts++;
            if (attempts > maxAttempts)
            {
                throw new Exception($"Problem with status {stateCode} async operation.");
            }
        }
    }

    #region ExecuteMultiple

    public void AddRequest(Guid key, OrganizationRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var requests = _multipleRequests.GetOrAdd(key, _ => []);
        lock (requests)
        {
            requests.Add(request);
        }
    }

    public void RemoveRequests(Guid key)
    {
        List<OrganizationRequest> removed;
        _multipleRequests.TryRemove(key, out removed);
    }

    public IReadOnlyCollection<OrganizationRequest> GetRequests(Guid key)
    {
        List<OrganizationRequest> requests;
        if (!_multipleRequests.TryGetValue(key, out requests))
        {
            return Array.Empty<OrganizationRequest>();
        }

        lock (requests)
        {
            return requests.ToList().AsReadOnly();
        }
    }

    public IReadOnlyDictionary<Guid, IReadOnlyCollection<OrganizationRequest>> GetAllRequests()
    {
        return _multipleRequests.ToDictionary(
            pair => pair.Key,
            pair =>
            {
                lock (pair.Value)
                {
                    return (IReadOnlyCollection<OrganizationRequest>)pair.Value.ToList().AsReadOnly();
                }
            });
    }

    public IEnumerable<ExecuteMultipleResponse> ExecuteMultipleRequest(Guid key, ExecuteMultipleSettings settings = null)
    {
        List<OrganizationRequest> requests;
        if (!_multipleRequests.TryGetValue(key, out requests))
        {
            return Enumerable.Empty<ExecuteMultipleResponse>();
        }

        List<OrganizationRequest> batchRequests;
        lock (requests)
        {
            batchRequests = requests.ToList();
        }

        if (!batchRequests.Any())
        {
            return Enumerable.Empty<ExecuteMultipleResponse>();
        }

        List<ExecuteMultipleResponse> responses = [];
        var processedRecords = 0;
        var totalRecords = batchRequests.Count;

        while (processedRecords < totalRecords)
        {
            var remainingRecords = totalRecords - processedRecords;
            var batchSize = _multipleRequestBatchSize > remainingRecords ? remainingRecords : _multipleRequestBatchSize;

            var currentBatch = batchRequests
                .Skip(processedRecords)
                .Take(batchSize)
                .ToArray();

            var request = CreateDefaultMultipleRequest();
            if (settings != null)
            {
                request.Settings = settings;
            }

            request.Requests.AddRange(currentBatch);
            responses.Add((ExecuteMultipleResponse)OrganizationService.Execute(request));

            processedRecords += batchSize;
        }

        RemoveRequests(key);
        return responses;
    }

    private static ExecuteMultipleRequest CreateDefaultMultipleRequest()
    {
        return new ExecuteMultipleRequest
        {
            Settings = new ExecuteMultipleSettings
            {
                ContinueOnError = true,
                ReturnResponses = true
            },
            Requests = []
        };
    }

    #endregion

    #region CRUD

    public Guid Create(Entity entity)
    {
        return OrganizationService.Create(entity);
    }

    public Guid? CreateOutsideTransaction(Entity entity, bool returnResponse = true)
    {
        Guid? returnValue = new Guid?();

        if (entity != null)
        {
            ExecuteMultipleRequest request = new()
            {
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = true,
                    ReturnResponses = returnResponse
                },
                Requests = []
            };

            request.Requests.Add(new CreateRequest
            {
                Target = entity
            });

            ExecuteMultipleResponse responseWithResults = OrganizationService.Execute(request) as ExecuteMultipleResponse;

            if (responseWithResults?.Responses != null && responseWithResults.Responses.Any())
            {
                var emri = responseWithResults.Responses.First();

                if (emri.Fault != null)
                {
                    throw new FaultException<OrganizationServiceFault>(emri.Fault);
                }

                returnValue = (Guid)emri.Response.Results["id"];
            }
        }

        return returnValue;
    }

    public void Update(Entity entity)
    {
        OrganizationService.Update(entity);
    }

    public void UpdateOutsideTransaction(Entity entity)
    {
        if (entity != null)
        {
            ExecuteMultipleRequest request = new()
            {
                Settings = new ExecuteMultipleSettings()
                {
                    ContinueOnError = true,
                    ReturnResponses = false
                },
                Requests = []
            };

            request.Requests.Add(new UpdateRequest { Target = entity });

            ExecuteMultipleResponse responseWithResults = OrganizationService.Execute(request) as ExecuteMultipleResponse;
            if (responseWithResults?.Responses != null && responseWithResults.Responses.Any())
            {
                var emri = responseWithResults.Responses.First();

                if (emri.Fault != null)
                {
                    throw new FaultException<OrganizationServiceFault>(emri.Fault);
                }
            }
        }
    }

    public void Delete(Entity entity)
    {
        OrganizationService.Delete(entity.LogicalName, entity.Id);
    }

    public void Delete(string entityName, Guid id)
    {
        OrganizationService.Delete(entityName, id);
    }

    public void Delete(EntityReference entity)
    {
        OrganizationService.Delete(entity.LogicalName, entity.Id);
    }

    public void Delete<TEntity>(IEnumerable<TEntity> entityCollection) where TEntity : Entity
    {
        foreach (var entity in entityCollection)
        {
            OrganizationService.Delete(entity.LogicalName, entity.Id);
        }
    }

    public void Delete(EntityCollection entityCollection)
    {
        foreach (var entity in entityCollection.Entities)
        {
            OrganizationService.Delete(entity.LogicalName, entity.Id);
        }
    }

    public SetStateResponse SetState(EntityReference entityReference, int stateCode, int? statusCode)
    {
        SetStateRequest request = new()
        {
            State = new OptionSetValue(stateCode),
            Status = new OptionSetValue(statusCode ?? -1),
            EntityMoniker = entityReference
        };

        return (SetStateResponse)OrganizationService.Execute(request);
    }

    #endregion

    #region Entity helpers

    public Entity GetWebResourceByName(string webResourceName, bool throwExceptionIfNotExists = true)
    {
        QueryByAttribute request = new()
        {
            EntityName = "webresource",
            ColumnSet = new ColumnSet(
                "canbedeleted",
                "componentstate",
                "content",
                "contentjson",
                "createdby",
                "createdon",
                "dependencyxml",
                "description",
                "displayname",
                "introducedversion",
                "iscustomizable",
                "ishidden",
                "ismanaged",
                "languagecode",
                "modifiedon",
                "name",
                "organizationid",
                "solutionid",
                "versionnumber",
                "webresourceid",
                "webresourceidunique",
                "webresourcetype",
                "createdby",
                "modifiedby")
        };

        request.Attributes.AddRange("name");
        request.Values.AddRange(webResourceName);

        var webResourceCollection = OrganizationService.RetrieveMultiple(request);
        if (webResourceCollection.Entities.Any())
        {
            return webResourceCollection.Entities[0];
        }

        if (throwExceptionIfNotExists)
        {
            throw new InvalidPluginExecutionException($"Webresource '{webResourceName}' does not exist");
        }

        return null;
    }

    public DataCollection<Entity> LoadRecords(string entityName, string searchAttribute, string searchValue)
    {
        QueryExpression query = new()
        {
            EntityName = entityName,
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression()
        };

        query.Criteria.AddCondition(searchAttribute, ConditionOperator.Equal, searchValue);
        return OrganizationService.RetrieveMultiple(query).Entities;
    }

    public DataCollection<Entity> LoadRecords(string entityName, string[] searchAttributes, string[] searchValues)
    {
        if (searchAttributes == null) throw new ArgumentNullException(nameof(searchAttributes));
        if (searchValues == null) throw new ArgumentNullException(nameof(searchValues));
        if (searchAttributes.Length != searchValues.Length) throw new ArgumentException("searchAttributes and searchValues must have same length.");

        var query = BuildQuery(entityName, searchAttributes, searchValues.Cast<object>().ToArray());
        return OrganizationService.RetrieveMultiple(query).Entities;
    }

    public Entity LoadRecord(string entityName, string[] searchAttributes, object[] searchValues)
    {
        if (searchAttributes == null) throw new ArgumentNullException(nameof(searchAttributes));
        if (searchValues == null) throw new ArgumentNullException(nameof(searchValues));
        if (searchAttributes.Length != searchValues.Length) throw new ArgumentException("searchAttributes and searchValues must have same length.");

        var query = BuildQuery(entityName, searchAttributes, searchValues);
        return OrganizationService.RetrieveMultiple(query).Entities.FirstOrDefault();
    }

    public Entity LoadRecord(string entityName, string searchAttribute, string searchValue)
    {
        QueryExpression query = new()
        {
            EntityName = entityName,
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression()
        };

        query.Criteria.AddCondition(searchAttribute, ConditionOperator.Equal, searchValue);
        return OrganizationService.RetrieveMultiple(query).Entities.FirstOrDefault();
    }

    private static QueryExpression BuildQuery(string entityName, string[] searchAttributes, object[] searchValues)
    {
        QueryExpression query = new()
        {
            EntityName = entityName,
            ColumnSet = new ColumnSet(true),
            Criteria = new FilterExpression()
        };

        for (var i = 0; i < searchAttributes.Length; i++)
        {
            if (searchValues[i] == null)
            {
                query.Criteria.AddCondition(searchAttributes[i], ConditionOperator.Null);
            }
            else
            {
                query.Criteria.AddCondition(searchAttributes[i], ConditionOperator.Equal, searchValues[i]);
            }
        }

        return query;
    }

    #endregion

    #region Metadata and option sets

    public int? GetUserUiLanguageCode(Guid userId)
    {
        QueryExpression query = new("usersettings")
        {
            ColumnSet = new ColumnSet("uilanguageid"),
            Criteria =
            {
                Conditions =
                {
                    new ConditionExpression("systemuserid", ConditionOperator.Equal, userId)
                }
            },
            TopCount = 1
        };

        var entity = OrganizationService.RetrieveMultiple(query)
                                        ?.Entities
                                        ?.FirstOrDefault();

        return entity?.GetAttributeValue<int?>("uilanguageid");
    }

    public string GetOptionLocalizedName(string entityName, string attributeName, int selectedValue, int languageCode)
    {
        RetrieveAttributeRequest request = new()
        {
            EntityLogicalName = entityName,
            LogicalName = attributeName.ToLower()
        };

        RetrieveAttributeResponse response = (RetrieveAttributeResponse)OrganizationService.Execute(request);
        PicklistAttributeMetadata metadata = (PicklistAttributeMetadata)response.AttributeMetadata;
        var option = metadata.OptionSet.Options.First(o => o.Value == selectedValue);
        var localizedLabel = option.Label.LocalizedLabels.FirstOrDefault(l => l.LanguageCode == languageCode);

        if (!string.IsNullOrEmpty(localizedLabel != null ? localizedLabel.Label : null))
        {
            return localizedLabel.Label;
        }

        if (option.Label.LocalizedLabels != null && option.Label.LocalizedLabels.Count > 0)
        {
            var firstLabel = option.Label.LocalizedLabels.First();
            if (firstLabel != null)
            {
                return firstLabel.Label;
            }
        }

        return string.Empty;
    }

    public int GetOptionSetValueByText(string entityName, string attributeName, string optionSetTextValue, bool ignoreDiacritics, bool caseSensitive)
    {
        RetrieveAttributeRequest request = new()
        {
            EntityLogicalName = entityName,
            LogicalName = attributeName.ToLower()
        };

        RetrieveAttributeResponse response = (RetrieveAttributeResponse)OrganizationService.Execute(request);
        if (response == null)
        {
            return -1;
        }

        PicklistAttributeMetadata metadata = response.AttributeMetadata as PicklistAttributeMetadata;
        if (metadata == null)
        {
            return -1;
        }

        foreach (var option in metadata.OptionSet.Options)
        {
            var optionLabel = option.Label.UserLocalizedLabel.Label;
            var comparedValue = optionSetTextValue;

            if (ignoreDiacritics)
            {
                optionLabel = StringExtensions.RemoveDiacritics(optionLabel);
                comparedValue = StringExtensions.RemoveDiacritics(comparedValue);
            }

            if (caseSensitive)
            {
                optionLabel = optionLabel.ToLower();
                comparedValue = comparedValue.ToLower();
            }

            if (optionLabel == comparedValue && option.Value.HasValue)
            {
                return option.Value.Value;
            }
        }

        return -1;
    }

    public EntityMetadata GetEntityMetadata(string entityLogicalName, EntityFilters entityFilters = EntityFilters.All)
    {
        RetrieveEntityRequest request = new()
        {
            LogicalName = entityLogicalName,
            EntityFilters = entityFilters
        };

        RetrieveEntityResponse response = (RetrieveEntityResponse)OrganizationService.Execute(request);
        return response.EntityMetadata;
    }

    #endregion
}