using AutoMapper;
using MediatR;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Pillaro.Dataverse.PluginFramework.Testing.Application.Queries;
using Pillaro.Dataverse.PluginFramework.Testing.Domain.Models;
using Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;

namespace Pillaro.Dataverse.PluginFramework.Testing.Application.Handlers;

internal class GetAsyncProcessesHandler(IDataverseConnectionService connectionService, IMapper mapper) : IRequestHandler<GetAsyncProcesses, List<AsyncProcessResult>>
{
    public Task<List<AsyncProcessResult>> Handle(GetAsyncProcesses request, CancellationToken cancellationToken)
    {
        List<AsyncProcessResult> ret = [];

        QueryExpression queryExpression = new("asyncoperation");
        queryExpression.ColumnSet.AddColumns("asyncoperationid", "statecode", "statuscode", "message", "createdon", "completedon", "retrycount");
        queryExpression.Criteria = new FilterExpression();
        queryExpression.Criteria.AddCondition("regardingobjectid", ConditionOperator.Equal, request.EntityId);
        queryExpression.Criteria.AddCondition("completedon", ConditionOperator.GreaterEqual, request.DateFrom);
        queryExpression.Criteria.AddCondition("completedon", ConditionOperator.LessEqual, request.DateTo);

        EntityCollection entityCollection = connectionService.GetOrganizationService().RetrieveMultiple(queryExpression);
        if (entityCollection.Entities.Count == 0)
            return Task.FromResult(ret);

        ret.AddRange(entityCollection.Entities.Select(entity => mapper.Map<AsyncProcessResult>(entity)));

        return Task.FromResult(ret);
    }
}