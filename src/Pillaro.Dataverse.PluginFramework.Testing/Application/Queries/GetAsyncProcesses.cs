using MediatR;
using Pillaro.Dataverse.PluginFramework.Testing.Domain.Models;

namespace Pillaro.Dataverse.PluginFramework.Testing.Application.Queries;

public class GetAsyncProcesses : IRequest<List<AsyncProcessResult>>
{
    public Guid EntityId { get; }
    public DateTime DateFrom { get; }
    public DateTime DateTo { get; }

    public GetAsyncProcesses(Guid entityId, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity id cannot be empty.", nameof(entityId));

        dateFrom ??= DateTime.UtcNow.AddMinutes(-5);
        dateTo ??= DateTime.UtcNow;

        if (dateTo < dateFrom)
            throw new ArgumentOutOfRangeException(nameof(dateTo), "dateTo must be greater than or equal to dateFrom.");

        EntityId = entityId;
        DateFrom = dateFrom.Value;
        DateTo = dateTo.Value;
    }
}