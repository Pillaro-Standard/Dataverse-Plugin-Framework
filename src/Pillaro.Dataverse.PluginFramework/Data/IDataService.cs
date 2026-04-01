using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Pillaro.Dataverse.PluginFramework.Data.Query;
using System;
using System.Collections.Generic;

namespace Pillaro.Dataverse.PluginFramework.Data;

public interface IDataService
{
    IOrganizationService OrganizationService { get; }

    Guid GetInstanceId();

    int GetMultipleRequestBatchSize();

    EntityQuery<TEntity> Query<TEntity>() where TEntity : Entity;

    WhoAmIResponse WhoAmI();

    #region ExecuteMultiple

    void AddRequest(Guid key, OrganizationRequest request);

    void RemoveRequests(Guid key);

    IReadOnlyCollection<OrganizationRequest> GetRequests(Guid key);

    IReadOnlyDictionary<Guid, IReadOnlyCollection<OrganizationRequest>> GetAllRequests();

    IEnumerable<ExecuteMultipleResponse> ExecuteMultipleRequest(Guid key, ExecuteMultipleSettings? settings = null);

    #endregion

    #region CRUD

    Guid Create(Entity entity);

    Guid? CreateOutsideTransaction(Entity entity, bool returnResponse = true);

    void Update(Entity entity);

    void UpdateOutsideTransaction(Entity entity);

    void Delete(Entity entity);

    void Delete(string entityName, Guid id);

    void Delete(EntityReference entity);

    void Delete<TEntity>(IEnumerable<TEntity> entityCollection) where TEntity : Entity;

    void Delete(EntityCollection entityCollection);

    SetStateResponse SetState(EntityReference entityReference, int stateCode, int? statusCode);

    #endregion

    #region Entity helpers

    Entity GetWebResourceByName(string webResourceName, bool throwExceptionIfNotExists = true);

    DataCollection<Entity> LoadRecords(string entityName, string searchAttribute, string searchValue);

    DataCollection<Entity> LoadRecords(string entityName, string[] searchAttributes, string[] searchValues);

    Entity LoadRecord(string entityName, string[] searchAttributes, object[] searchValues);

    Entity LoadRecord(string entityName, string searchAttribute, string searchValue);

    #endregion

    #region Metadata and option sets

    int? GetUserUiLanguageCode(Guid userId);

    string GetOptionLocalizedName(string entityName, string attributeName, int selectedValue, int languageCode);

    int GetOptionSetValueByText(string entityName, string attributeName, string optionSetTextValue, bool ignoreDiacritics, bool caseSensitive);

    EntityMetadata GetEntityMetadata(string entityLogicalName, EntityFilters entityFilters = EntityFilters.All);

    #endregion
}