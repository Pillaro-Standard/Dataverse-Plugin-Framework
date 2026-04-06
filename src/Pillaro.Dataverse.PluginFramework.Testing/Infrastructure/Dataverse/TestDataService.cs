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
using Xunit;

namespace Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

internal sealed class TestDataService : DataService, ITestDataService
{
    private readonly ILifetimeScope _lifetimeScope;
    private readonly ConcurrentStack<EntityReference> _entitiesToDelete = new();
    private readonly ConcurrentDictionary<string, ICleanupDeleteHandler> _cleanupHandlers = new(StringComparer.OrdinalIgnoreCase);

    private volatile ITestOutputHelper? _output;

    public TestDataService(IDataverseConnectionService connectionService, ILifetimeScope lifetimeScope)
        : base(connectionService.GetOrganizationService())
    {
        ArgumentNullException.ThrowIfNull(lifetimeScope);
        _lifetimeScope = lifetimeScope;
    }

    public void SetOutput(ITestOutputHelper output)
    {
        ArgumentNullException.ThrowIfNull(output);
        _output = output;
    }

    public Guid CreateTestEntity(Entity entity, bool byPassPlugins = false)
    {
        ArgumentNullException.ThrowIfNull(entity);

        if (!byPassPlugins)
        {
            entity.Id = Create(entity);
            WriteOutput($"Created entity '{entity.LogicalName}' with Id '{entity.Id}'.");
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
            WriteOutput($"Created entity '{entity.LogicalName}' with Id '{entity.Id}' (bypassed plugins).");
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
        WriteOutput($"Created and retrieved entity '{created.LogicalName}' with Id '{created.Id}'.");
        return created;
    }

    public async Task WaitOnAsyncProcess(Guid entityId, int numberOfAttempts = 40, int cancellationTimeMs = 120000, CancellationToken cancellation = default)
    {
        WriteOutput($"Waiting on async process for entity Id '{entityId}' (attempts: {numberOfAttempts}, timeout: {cancellationTimeMs}ms).");
        var handler = _lifetimeScope.Resolve<WaitOnAsyncProcessHandler>();
        await handler.HandleAsync(new WaitOnAsyncProcess(entityId, numberOfAttempts, cancellationTimeMs), cancellation);
        WriteOutput($"Async process for entity Id '{entityId}' completed.");
    }

    public async Task<List<AsyncProcessResult>> GetAsyncProcessResults(Guid entityId, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken cancellation = default)
    {
        WriteOutput($"Fetching async process results for entity Id '{entityId}'.");
        var handler = _lifetimeScope.Resolve<GetAsyncProcessesHandler>();
        var results = await handler.HandleAsync(new GetAsyncProcesses(entityId, dateFrom, dateTo), cancellation);
        WriteOutput($"Retrieved {results.Count} async process result(s) for entity Id '{entityId}'.");
        return results;
    }

    public new TTestDataRepository GetRepository<TTestDataRepository>() where TTestDataRepository : IAutoRegisteredTestDataRepository
    {
        return _lifetimeScope.Resolve<TTestDataRepository>();
    }

    public void AddTestEntityToDelete(EntityReference entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        _entitiesToDelete.Push(entity);
        WriteOutput($"Registered entity '{entity.LogicalName}' with Id '{entity.Id}' for cleanup.");
    }

    public void DeleteTestEntities()
    {
        WriteOutput("Starting test entity cleanup.");

        while (_entitiesToDelete.TryPop(out var entityReference))
        {
            if (entityReference.Id == Guid.Empty)
                continue;

            if (_cleanupHandlers.TryGetValue(entityReference.LogicalName, out var cleanupHandler))
            {
                WriteOutput($"Running cleanup handler for '{entityReference.LogicalName}' with Id '{entityReference.Id}'.");
                cleanupHandler.DeleteReferences(entityReference, this);
            }

            try
            {
                Delete(entityReference);
                WriteOutput($"Deleted entity '{entityReference.LogicalName}' with Id '{entityReference.Id}'.");
            }
            catch (Exception ex)
            {
                WriteOutput($"Failed to delete entity '{entityReference.LogicalName}' with Id '{entityReference.Id}': {ex.Message}");
                Debug.WriteLine(ex);
            }
        }

        WriteOutput("Test entity cleanup completed.");
    }

    public void AddCleanUpDeleteHandler(ICleanupDeleteHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        if (!_cleanupHandlers.TryAdd(handler.EntityLogicalName, handler))
        {
            throw new InvalidOperationException($"Cleanup handler for entity '{handler.EntityLogicalName}' is already registered.");
        }

        WriteOutput($"Registered cleanup handler for entity '{handler.EntityLogicalName}'.");
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
        WriteOutput($"Returning {_cleanupHandlers.Count} registered cleanup handler(s).");
        return _cleanupHandlers.Values.ToList().AsReadOnly();
    }

    private void WriteOutput(string message)
    {
        try
        {
            _output?.WriteLine(message);
        }
        catch (InvalidOperationException)
        {
            // ITestOutputHelper throws when the test has already completed.
            Debug.WriteLine(message);
        }
    }
}