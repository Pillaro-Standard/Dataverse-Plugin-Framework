using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Pillaro.Dataverse.PluginFramework.Data.Query;

namespace Pillaro.Dataverse.PluginFramework.Data;

public interface IDataService
{
    Guid GetInstanceId();

    int GetMultipleRequestBatchSize();

    EntityQuery<TEntity> Query<TEntity>() where TEntity : Entity;

    void WaitOnAsyncProcess(Guid entityId, int? numberOfAttempts = null);

    #region ExecuteMultiple

    void AddRequest(Guid key, OrganizationRequest request);

    void RemoveRequests(Guid key);

    IReadOnlyCollection<OrganizationRequest> GetRequests(Guid key);

    IReadOnlyDictionary<Guid, IReadOnlyCollection<OrganizationRequest>> GetAllRequests();

    IEnumerable<ExecuteMultipleResponse> ExecuteMultipleRequest(Guid key, ExecuteMultipleSettings settings = null);

    #endregion

    #region Transactional Operations

    /// <summary>
    /// Creates an entity outside of the current transaction using ExecuteMultiple.
    /// </summary>
    Guid? CreateOutsideTransaction(Entity entity, bool returnResponse = true);

    /// <summary>
    /// Updates an entity outside of the current transaction using ExecuteMultiple.
    /// </summary>
    void UpdateOutsideTransaction(Entity entity);

    #endregion

    #region Entity Helpers

    Entity GetWebResourceByName(string webResourceName, bool throwExceptionIfNotExists = true);

    DataCollection<Entity> LoadRecords(string entityName, string searchAttribute, string searchValue);

    DataCollection<Entity> LoadRecords(string entityName, string[] searchAttributes, string[] searchValues);

    Entity LoadRecord(string entityName, string[] searchAttributes, object[] searchValues);

    Entity LoadRecord(string entityName, string searchAttribute, string searchValue);

    #endregion

    #region Metadata and Option Sets

    int? GetUserUiLanguageCode(Guid userId);

    string GetOptionLocalizedName(string entityName, string attributeName, int selectedValue, int languageCode);

    int GetOptionSetValueByText(string entityName, string attributeName, string optionSetTextValue, bool ignoreDiacritics, bool caseSensitive);

    EntityMetadata GetEntityMetadata(string entityLogicalName, EntityFilters entityFilters = EntityFilters.All);

    #endregion
}