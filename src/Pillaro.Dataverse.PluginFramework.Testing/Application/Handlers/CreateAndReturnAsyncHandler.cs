using MediatR;
using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Testing.Application.Commands;
using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

namespace Pillaro.Dataverse.PluginFramework.Testing.Application.Handlers;

public class CreateAndReturnAsyncHandler(IDataverseConnectionService connectionService) : IRequestHandler<CreateEntityAndReturn, Entity>
{
    public async Task<Entity> Handle(CreateEntityAndReturn request, CancellationToken cancellationToken)
    {
        return await connectionService.GetOrganizationService().CreateAndReturnAsync(request.Entity, cancellationToken);
    }
}