using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Pillaro.Dataverse.PluginFramework.Logging.Enums;
using Pillaro.Dataverse.PluginFramework.Logging.Models;
using Pillaro.Dataverse.PluginFramework.Plugins;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Pillaro.Dataverse.PluginFramework.Tasks;

public class TaskContext
{
    public IPluginExecutionContext PluginExecutionContext { get; }

    public string PrimaryEntityName { get; }

    public Guid PrimaryEntityId { get; }

    public Guid InitiatingUserId { get; }
    public Guid UserId { get; }

    public PluginStage Stage { get; }

    public PluginMode Mode { get; }

    public string Message { get; }

    public string SolutionName { get; }

    public Dictionary<string, object> UnsecureConfig { get; }

    public Dictionary<string, object> SecureConfig { get; }

    public string Version { get; set; }

    public int CountOfTasks { get; set; }
    public int TaskOrder { get; set; }

    private readonly Dictionary<string, Entity> _entitiesToUpdate = [];
    private IList<Log> _logs = [];
    private readonly Dictionary<string, object> _items = [];

    public TaskContext(string unsecuredConfig, string securedConfig, IPluginExecutionContext pluginExecutionContext)
    {
        PluginExecutionContext = pluginExecutionContext ?? throw new ArgumentNullException(nameof(pluginExecutionContext));

        PrimaryEntityName = PluginExecutionContext.PrimaryEntityName;
        PrimaryEntityId = PluginExecutionContext.PrimaryEntityId;
        InitiatingUserId = PluginExecutionContext.InitiatingUserId;
        UserId = PluginExecutionContext.UserId;
        Stage = (PluginStage)PluginExecutionContext.Stage;
        Message = PluginExecutionContext.MessageName;
        Mode = (PluginMode)PluginExecutionContext.Mode;

        if (!string.IsNullOrEmpty(unsecuredConfig))
            UnsecureConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(unsecuredConfig);

        if (!string.IsNullOrEmpty(securedConfig))
            SecureConfig = JsonConvert.DeserializeObject<Dictionary<string, object>>(securedConfig);
    }

    public TValue? GetSecuredValue<TValue>(string key) where TValue : struct
    {
        if (SecureConfig == null)
            return null;

        if (!SecureConfig.ContainsKey(key))
            return null;

        return (TValue)Convert.ChangeType(SecureConfig[key], typeof(TValue));
    }

    public string GetSecuredValue(string key)
    {
        if (SecureConfig == null)
            return null;

        if (!SecureConfig.ContainsKey(key))
            return null;

        return SecureConfig[key]?.ToString();
    }

    public TValue? GetUnsecuredValue<TValue>(string key) where TValue : struct
    {
        if (UnsecureConfig == null)
            return null;

        if (!UnsecureConfig.ContainsKey(key))
            return null;

        return (TValue)Convert.ChangeType(UnsecureConfig[key], typeof(TValue));
    }

    public string GetUnsecuredValue(string key)
    {
        if (UnsecureConfig == null)
            return null;

        if (!UnsecureConfig.ContainsKey(key))
            return null;

        return UnsecureConfig[key]?.ToString();
    }

    public void AddItem(string key, object value)
    {
        _items.Add(key, value);
    }

    public T GetItem<T>(string key)
    {
        return (T)_items[key];
    }

    public bool ExistsItem(string key)
    {
        return _items.ContainsKey(key);
    }

    public IReadOnlyList<Entity> EntitiesToUpdate =>
        new ReadOnlyCollection<Entity>(_entitiesToUpdate.Values.ToList());

    public void AddEntityToUpdate(Entity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (string.IsNullOrWhiteSpace(entity.LogicalName))
            throw new ArgumentException("Entity logical name is required.", nameof(entity));

        if (entity.Id == Guid.Empty)
            throw new ArgumentException("Entity Id is required.", nameof(entity));

        if (!entity.Attributes.Any())
            return;

        var key = GetEntityUpdateKey(entity.LogicalName, entity.Id);

        if (_entitiesToUpdate.TryGetValue(key, out var existingEntity))
        {
            _entitiesToUpdate[key] = MergeEntities(existingEntity, entity);
            return;
        }

        _entitiesToUpdate[key] = CloneEntity(entity);
    }

    public Entity GetActualEntityToUpdate(string entityName, Guid id)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            throw new ArgumentException("Entity name is required.", nameof(entityName));

        if (id == Guid.Empty)
            throw new ArgumentException("Entity Id is required.", nameof(id));

        var key = GetEntityUpdateKey(entityName, id);

        if (_entitiesToUpdate.TryGetValue(key, out var entity))
            return CloneEntity(entity);

        return new Entity(entityName) { Id = id };
    }

    public Log SaveEntityToUpdate(IOrganizationService organizationService, TaskContext taskContext)
    {
        if (organizationService == null)
            throw new ArgumentNullException(nameof(organizationService));

        if (taskContext == null)
            throw new ArgumentNullException(nameof(taskContext));

        if (!_entitiesToUpdate.Any())
            return null;

        Log log = new(LogSeverity.Debug, new LogExecutionContext(taskContext.PluginExecutionContext), "SaveEntityToUpdate")
        {
            StartUtc = DateTime.UtcNow,
            LogDetails = []
        };

        foreach (var entity in _entitiesToUpdate.Values)
        {
            if (!entity.Attributes.Any())
                continue;

            log.LogDetails.Add(
                new LogDetail(
                    $"Entity to update : '{entity.LogicalName}:{entity.Id}'",
                    JsonConvert.SerializeObject(entity)));

            organizationService.Update(entity);
        }

        return log;
    }

    public void AddLog(Log log)
    {
        if (log == null)
            throw new ArgumentNullException(nameof(log));

        _logs.Add(log);
    }

    public IEnumerable<Log> GetLogs()
    {
        return _logs.Select(item => (Log)item.Clone()).ToList();
    }

    private static string GetEntityUpdateKey(string logicalName, Guid id)
    {
        return $"{logicalName}:{id:D}";
    }

    private static Entity MergeEntities(Entity targetEntity, Entity entityToMerge)
    {
        if (targetEntity == null)
            throw new ArgumentNullException(nameof(targetEntity));

        if (entityToMerge == null)
            throw new ArgumentNullException(nameof(entityToMerge));

        if (!string.Equals(targetEntity.LogicalName, entityToMerge.LogicalName, StringComparison.Ordinal))
            throw new InvalidOperationException("Entities must have the same logical name.");

        if (targetEntity.Id != entityToMerge.Id)
            throw new InvalidOperationException("Entities must have the same Id.");

        Entity result = new(targetEntity.LogicalName)
        {
            Id = targetEntity.Id
        };

        foreach (var attribute in targetEntity.Attributes)
        {
            result.Attributes[attribute.Key] = attribute.Value;
        }

        foreach (var attribute in entityToMerge.Attributes)
        {
            result.Attributes[attribute.Key] = attribute.Value;
        }

        return result;
    }

    private static Entity CloneEntity(Entity source)
    {
        if (source == null)
            throw new ArgumentNullException(nameof(source));

        Entity clone = new(source.LogicalName)
        {
            Id = source.Id
        };

        foreach (var attribute in source.Attributes)
        {
            clone.Attributes[attribute.Key] = attribute.Value;
        }

        foreach (var formattedValue in source.FormattedValues)
        {
            clone.FormattedValues[formattedValue.Key] = formattedValue.Value;
        }

        foreach (var keyAttribute in source.KeyAttributes)
        {
            clone.KeyAttributes[keyAttribute.Key] = keyAttribute.Value;
        }

        clone.RowVersion = source.RowVersion;

        return clone;
    }
}