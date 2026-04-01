using MediatR;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Pillaro.Dataverse.PluginFramework.Testing.Application.Commands;
using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

namespace Pillaro.Dataverse.PluginFramework.Testing.Application.Handlers;

internal class WaitOnAsyncProcessHandler(IDataverseConnectionService connectionService) : IRequestHandler<WaitOnAsyncProcess>
{
    public async Task Handle(WaitOnAsyncProcess request, CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(request.CancellationTimeMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        for (var attempt = 1; attempt <= request.NumberOfAttempts; attempt++)
        {
            linkedCts.Token.ThrowIfCancellationRequested();

            var queryExpression = new QueryExpression("asyncoperation");
            queryExpression.ColumnSet.AddColumns("statecode");
            queryExpression.Criteria = new FilterExpression();
            queryExpression.Criteria.AddCondition("regardingobjectid", ConditionOperator.Equal, request.EntityId);
            queryExpression.Criteria.AddCondition("statecode", ConditionOperator.NotEqual, 3);

            var entityCollection = connectionService.GetOrganizationService().RetrieveMultiple(queryExpression);
            if (entityCollection.Entities.Count == 0)
                return;

            var entity = entityCollection[0];
            if (!entity.Attributes.TryGetValue("statecode", out var stateCodeObj) || stateCodeObj is not OptionSetValue stateCode)
                return;

            if (attempt == request.NumberOfAttempts)
                throw new TimeoutException($"Async operation did not finish after {request.NumberOfAttempts} attempts. Last state code: {stateCode.Value}.");

            await Task.Delay(1000, linkedCts.Token);
        }
    }
}