using MediatR;

namespace Pillaro.Dataverse.PluginFramework.Testing.Application.Commands;

public class WaitOnAsyncProcess : IRequest
{
    public Guid EntityId { get; }
    public int NumberOfAttempts { get; }
    public int CancellationTimeMs { get; }

    public WaitOnAsyncProcess(Guid entityId, int numberOfAttempts, int cancellationTimeMs)
    {
        if (entityId == Guid.Empty)
            throw new ArgumentException("Entity id cannot be empty.", nameof(entityId));
        if (numberOfAttempts <= 0)
            throw new ArgumentOutOfRangeException(nameof(numberOfAttempts));
        if (cancellationTimeMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(cancellationTimeMs));

        EntityId = entityId;
        NumberOfAttempts = numberOfAttempts;
        CancellationTimeMs = cancellationTimeMs;
    }
}