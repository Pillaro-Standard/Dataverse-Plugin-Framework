using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Pillaro.Dataverse.PluginFramework.Tests.Tests.Tasks;

/// <summary>
/// In-memory plugin execution doubles, so task and plugin behavior can be tested without Dataverse.
/// </summary>
internal class FakeServiceProvider : IServiceProvider
{
    private readonly IPluginExecutionContext _pluginExecutionContext;

    public FakeServiceProvider(IPluginExecutionContext pluginExecutionContext, RecordingOrganizationService? organizationService = null)
    {
        _pluginExecutionContext = pluginExecutionContext;
        OrganizationService = organizationService ?? new RecordingOrganizationService();
        OrganizationServiceFactory = new FakeOrganizationServiceFactory(OrganizationService);
    }

    public RecordingOrganizationService OrganizationService { get; }

    public FakeOrganizationServiceFactory OrganizationServiceFactory { get; }

    public ITracingService TracingService { get; } = new FakeTracingService();

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(IPluginExecutionContext))
            return _pluginExecutionContext;

        if (serviceType == typeof(IOrganizationServiceFactory))
            return OrganizationServiceFactory;

        if (serviceType == typeof(ITracingService))
            return TracingService;

        return null;
    }
}

internal class FakeOrganizationServiceFactory : IOrganizationServiceFactory
{
    private readonly RecordingOrganizationService _organizationService;

    public FakeOrganizationServiceFactory(RecordingOrganizationService organizationService)
    {
        _organizationService = organizationService;
    }

    public List<Guid?> RequestedUserIds { get; } = [];

    public IOrganizationService CreateOrganizationService(Guid? userId)
    {
        RequestedUserIds.Add(userId);
        return _organizationService;
    }
}

internal class FakeTracingService : ITracingService
{
    public List<string> Traces { get; } = [];

    public void Trace(string format, params object[] args)
    {
        Traces.Add(args is { Length: > 0 } ? string.Format(format, args) : format);
    }
}

/// <summary>
/// Records the writes a plugin performs. Everything else throws, which is what the framework
/// logging expects from an environment without the log entities.
/// </summary>
internal class RecordingOrganizationService : IOrganizationService
{
    public List<Entity> UpdatedEntities { get; } = [];

    public void Update(Entity entity)
    {
        UpdatedEntities.Add(entity);
    }

    public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        => throw new NotSupportedException();

    public Guid Create(Entity entity) => throw new NotSupportedException();

    public void Delete(string entityName, Guid id) => throw new NotSupportedException();

    public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        => throw new NotSupportedException();

    public OrganizationResponse Execute(OrganizationRequest request) => throw new NotSupportedException();

    public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet) => throw new NotSupportedException();

    public EntityCollection RetrieveMultiple(QueryBase query) => throw new NotSupportedException();
}

internal class FakePluginExecutionContext : IPluginExecutionContext
{
    public int Stage { get; set; }

    public IPluginExecutionContext? ParentContext { get; set; }

    public int Mode { get; set; }

    public int IsolationMode { get; set; }

    public int Depth { get; set; } = 1;

    public string? MessageName { get; set; }

    public string? PrimaryEntityName { get; set; }

    public Guid? RequestId { get; set; }

    public string? SecondaryEntityName { get; set; }

    public ParameterCollection InputParameters { get; set; } = [];

    public ParameterCollection OutputParameters { get; set; } = [];

    public ParameterCollection SharedVariables { get; set; } = [];

    public Guid UserId { get; set; }

    public Guid InitiatingUserId { get; set; }

    public Guid BusinessUnitId { get; set; }

    public Guid OrganizationId { get; set; }

    public string? OrganizationName { get; set; }

    public Guid PrimaryEntityId { get; set; }

    public EntityImageCollection PreEntityImages { get; set; } = [];

    public EntityImageCollection PostEntityImages { get; set; } = [];

    public EntityReference? OwningExtension { get; set; }

    public Guid CorrelationId { get; set; } = Guid.NewGuid();

    public bool IsExecutingOffline { get; set; }

    public bool IsOfflinePlayback { get; set; }

    public bool IsInTransaction { get; set; }

    public Guid OperationId { get; set; }

    public DateTime OperationCreatedOn { get; set; } = DateTime.UtcNow;
}
