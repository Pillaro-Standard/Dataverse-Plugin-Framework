using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Data;
using Pillaro.Dataverse.PluginFramework.Testing.Domain.Interfaces;
using Pillaro.Dataverse.PluginFramework.Testing.Domain.Models;
using Xunit;

namespace Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

public interface ITestDataService : IDataService, IAutoRegisteredService
{
    void SetOutput(ITestOutputHelper output);

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