using MediatR;
using Microsoft.Xrm.Sdk;

namespace Pillaro.Dataverse.PluginFramework.Testing.Application.Commands;

public class CreateEntityAndReturn : IRequest<Entity>
{
    public Entity Entity { get; }

    public CreateEntityAndReturn(Entity entity)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        Entity = entity;
    }
}