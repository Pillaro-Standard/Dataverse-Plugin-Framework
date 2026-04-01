using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Data;
using Pillaro.Dataverse.PluginFramework.Testing.Domain.Interfaces;
using Pillaro.Dataverse.PluginFramework.Testing.Domain.Models;

namespace Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

/// <summary>
/// Provides operations for managing test data in Dataverse.
/// </summary>
/// <remarks>
/// This service encapsulates common operations used in integration tests,
/// such as entity creation, async process handling, and cleanup of created data.
///
/// It is part of the infrastructure layer and works directly with Dataverse.
/// </remarks>
public interface ITestDataService : IDataService, IAutoRegisteredService
{
    Guid CreateTestEntity(Entity entity, bool byPassPlugins = false);

    Task<Entity> CreateAndReturnTestEntity(Entity entity, CancellationToken cancellation = default);

    void AddTestEntityToDelete(EntityReference entity);

    TDataRepository GetRepository<TDataRepository>() where TDataRepository : IAutoRegisteredTestDataRepository;

    Task WaitOnAsyncProcess(Guid entityId, int numberOfAttempts = 40, int cancellationTimeMs = 120000, CancellationToken cancellation = default);


    Task<List<AsyncProcessResult>> GetAsyncProcessResults(Guid entityId, DateTime? dateFrom = null, DateTime? dateTo = null, CancellationToken cancellation = default);

    void DeleteTestEntities();

    void AddCleanUpDeleteHandler(ICleanupDeleteHandler handler);

    void AddCleanUpDeleteHandlers(IEnumerable<ICleanupDeleteHandler> handlers);

    IReadOnlyCollection<ICleanupDeleteHandler> GetAllCleanUpDeleteHandlers();
}