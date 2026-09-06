using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using Pillaro.Dataverse.PluginFramework.Logging.Models;
using Pillaro.Dataverse.PluginFramework.Plugins;
using System.Collections.ObjectModel;

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

    public string UnsecureConfig { get; }

    public string SecureConfig { get; }

    public string Version { get; set; } = "Unknown";

    public int CountOfTasks { get; set; }
    public int TaskOrder { get; set; }

    private readonly Dictionary<string, QueuedEntityUpdate> _entitiesToUpdate = [];
    private readonly IList<Log> _logs = [];
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
        UnsecureConfig = unsecuredConfig;
        SecureConfig = securedConfig;
    }

    public void AddItem(string key, object value)
    {
        _items[key] = value;
    }

    public T GetItem<T>(string key)
    {
        if (!_items.TryGetValue(key, out var value))
            throw new KeyNotFoundException($"Item with key '{key}' was not found.");

        if (value is not T typedValue)
            throw new InvalidCastException(
                $"Item with key '{key}' is of type '{value?.GetType().FullName}', not '{typeof(T).FullName}'.");

        return typedValue;
    }

    public bool ExistsItem(string key)
    {
        return _items.ContainsKey(key);
    }

    /// <summary>
    /// Entities queued by the tasks of the current plugin execution.
    /// They are written by <c>PluginBase</c> after all tasks have run, once per record and service user.
    /// </summary>
    public IReadOnlyList<Entity> EntitiesToUpdate =>
        new ReadOnlyCollection<Entity>([.. _entitiesToUpdate.Values.Select(o => o.Entity)]);

    internal IReadOnlyList<QueuedEntityUpdate> QueuedEntityUpdates =>
        new ReadOnlyCollection<QueuedEntityUpdate>([.. _entitiesToUpdate.Values]);

    /// <summary>
    /// Queues an entity to be written at the end of the plugin execution.
    /// Attributes queued for the same record by several tasks are merged, so the record is written
    /// only once and does not trigger the registered steps repeatedly.
    /// In a pre-stage, values for the record the plugin is running on are merged into the message
    /// target instead, so they take part in the current operation without any additional write.
    /// </summary>
    /// <param name="entity">Record and attributes to write. Logical name and id are required.</param>
    /// <param name="serviceUser">
    /// The user the write is performed as. The default <see cref="ServiceUser.User"/> keeps the audit
    /// on the user who triggered the operation; use <see cref="ServiceUser.Admin"/> for values the
    /// calling user is not allowed to write. A record queued for several service users is written once
    /// per service user, and only the <see cref="ServiceUser.User"/> part can be merged into the
    /// message target in a pre-stage.
    /// </param>
    public void AddEntityToUpdate(Entity entity, ServiceUser serviceUser = ServiceUser.User)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (string.IsNullOrWhiteSpace(entity.LogicalName))
            throw new ArgumentException("Entity logical name is required.", nameof(entity));

        if (entity.Id == Guid.Empty)
            throw new ArgumentException("Entity Id is required.", nameof(entity));

        if (!entity.Attributes.Any())
            return;

        var key = GetEntityUpdateKey(entity.LogicalName, entity.Id, serviceUser);

        if (_entitiesToUpdate.TryGetValue(key, out var existingUpdate))
        {
            _entitiesToUpdate[key] = new QueuedEntityUpdate(
                MergeEntities(existingUpdate.Entity, entity),
                serviceUser);
            return;
        }

        _entitiesToUpdate[key] = new QueuedEntityUpdate(CloneEntity(entity), serviceUser);
    }

    /// <summary>
    /// Returns the attributes queued for the given record and service user so far, so a later task can
    /// see what an earlier one queued. When nothing is queued yet, an empty entity with the given
    /// identity is returned.
    /// </summary>
    public Entity GetActualEntityToUpdate(string entityName, Guid id, ServiceUser serviceUser = ServiceUser.User)
    {
        if (string.IsNullOrWhiteSpace(entityName))
            throw new ArgumentException("Entity name is required.", nameof(entityName));

        if (id == Guid.Empty)
            throw new ArgumentException("Entity Id is required.", nameof(id));

        var key = GetEntityUpdateKey(entityName, id, serviceUser);

        if (_entitiesToUpdate.TryGetValue(key, out var queuedUpdate))
            return CloneEntity(queuedUpdate.Entity);

        return new Entity(entityName) { Id = id };
    }


    internal void ClearEntitiesToUpdate()
    {
        _entitiesToUpdate.Clear();
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

    private static string GetEntityUpdateKey(string logicalName, Guid id, ServiceUser serviceUser)
    {
        return $"{serviceUser}:{logicalName}:{id:D}";
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

internal class QueuedEntityUpdate
{
    public QueuedEntityUpdate(Entity entity, ServiceUser serviceUser)
    {
        Entity = entity;
        ServiceUser = serviceUser;
    }

    public Entity Entity { get; }

    public ServiceUser ServiceUser { get; }
}
