using Autofac;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Pillaro.Dataverse.PluginFramework.Data;
using Pillaro.Dataverse.PluginFramework.Testing.Application.Commands;
using Pillaro.Dataverse.PluginFramework.Testing.Application.Handlers;
using Pillaro.Dataverse.PluginFramework.Testing.Application.Queries;
using Pillaro.Dataverse.PluginFramework.Testing.Domain.Models;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

internal sealed class TestDataService : DataService, ITestDataService
{
    private readonly ILifetimeScope _lifetimeScope;
    private readonly ConcurrentStack<EntityReference> _entitiesToDelete = new();
    private readonly ConcurrentDictionary<string, ICleanupDeleteHandler> _cleanupHandlers = new(StringComparer.OrdinalIgnoreCase);

    public TestDataService(IDataverseConnectionService connectionService, ILifetimeScope lifetimeScope)
        : base(connectionService.GetOrganizationService())
    {
        ArgumentNullException.ThrowIfNull(lifetimeScope);
        _lifetimeScope = lifetimeScope;
    }

    public Guid CreateTestEntity(Entity entity, bool byPassPlugins = false)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!byPassPlugins)
        {
            entity.Id = Create(entity);
        }
        else
        {
            var request = new CreateRequest
            {
                Target = entity
            };
            request.Parameters.Add("BypassCustomPluginExecution", true);

            var response = (CreateResponse)OrganizationService.Execute(request);
            entity.Id = response.id;
        }

        _entitiesToDelete.Push(entity.ToEntityReference());
        return entity.Id;
    }

    public async Task<Entity> CreateAndReturnTestEntity(Entity entity, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        entity.Id = Create(entity);
        var created = OrganizationService.Retrieve(entity.LogicalName, entity.Id, new Microsoft.Xrm.Sdk.Query.ColumnSet(true));
        _entitiesToDelete.Push(created.ToEntityReference());
        return created;
    }

    public async Task WaitOnAsyncProcess(Guid entityId, int numberOfAttempts = 40, int cancellationTimeMs = 120000, CancellationToken cancellation = default)
    {
        var handler = _lifetimeScope.Resolve<WaitOnAsyncProcessHandler>();
        await handler.HandleAsync(new WaitOnAsyncProcess(entityId, numberOfAttempts, cancellationTimeMs), cancellation);
    }

    public async Task<List<AsyncProcessResult>> GetAsyncProcessResults(Guid entityId, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken cancellation = default)
    {
        var handler = _lifetimeScope.Resolve<GetAsyncProcessesHandler>();
        return await handler.HandleAsync(new GetAsyncProcesses(entityId, dateFrom, dateTo), cancellation);
    }

    public new TTestDataRepository GetRepository<TTestDataRepository>() where TTestDataRepository : IAutoRegisteredTestDataRepository
    {
        return _lifetimeScope.Resolve<TTestDataRepository>();
    }

    public void AddTestEntityToDelete(EntityReference entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _entitiesToDelete.Push(entity);
    }

    public void DeleteTestEntities()
    {
        while (_entitiesToDelete.TryPop(out var entityReference))
        {
            if (entityReference.Id == Guid.Empty)
                continue;

            if (_cleanupHandlers.TryGetValue(entityReference.LogicalName, out var cleanupHandler))
            {
                cleanupHandler.DeleteReferences(entityReference, this);
            }

            try
            {
                Delete(entityReference);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }

    public void AddCleanUpDeleteHandler(ICleanupDeleteHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_cleanupHandlers.TryAdd(handler.EntityLogicalName, handler))
        {
            throw new InvalidOperationException($"Cleanup handler for entity '{handler.EntityLogicalName}' is already registered.");
        }
    }

    public void AddCleanUpDeleteHandlers(IEnumerable<ICleanupDeleteHandler> handlers)
    {
        ArgumentNullException.ThrowIfNull(handlers);

        foreach (var handler in handlers)
        {
            AddCleanUpDeleteHandler(handler);
        }
    }

    public IReadOnlyCollection<ICleanupDeleteHandler> GetAllCleanUpDeleteHandlers()
    {
        return _cleanupHandlers.Values.ToList().AsReadOnly();
    }
}