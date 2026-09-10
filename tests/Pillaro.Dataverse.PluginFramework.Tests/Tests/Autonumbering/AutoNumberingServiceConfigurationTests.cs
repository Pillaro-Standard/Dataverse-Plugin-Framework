using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Pillaro.Dataverse.PluginFramework.AutoNumbering;

namespace Pillaro.Dataverse.PluginFramework.Tests.Tests.Autonumbering;

/// <summary>
/// Configuration lookup in <see cref="AutoNumberingService.GetTransactionAutoNumber"/>, which the
/// Task autonumbering in the examples uses, exercised against a fake
/// organization service. Unlike <see cref="GetAutoNumberActionTests"/>, which calls the Custom API
/// server-side, these need no Dataverse environment and so cannot be affected by data left behind
/// by another run.
/// </summary>
public class AutoNumberingServiceConfigurationTests
{
    [Fact]
    public void More_than_one_matching_configuration_is_an_error()
    {
        // Previously the first row won and the duplicate stayed hidden, which made the number
        // depend on whichever row the platform returned first. The Custom API task already
        // reported this, so the library reports it too.
        var service = new StubOrganizationService(
            new Entity("pl_autonumbering", Guid.NewGuid()),
            new Entity("pl_autonumbering", Guid.NewGuid()));

        var sut = new AutoNumberingService(service);

        var ex = Assert.Throws<InvalidPluginExecutionException>(
            () => sut.GetTransactionAutoNumber("contact", Guid.NewGuid(), null, null));

        Assert.Contains("More than one primary autonumbering configuration for entity 'contact'", ex.Message);
    }

    [Fact]
    public void Configuration_lookup_asks_only_for_active_configurations()
    {
        // No rows, so the lookup returns nothing and GetAutoNumber reports the missing
        // configuration. The query it built is what this asserts on.
        var service = new StubOrganizationService();
        var sut = new AutoNumberingService(service);

        Assert.Throws<InvalidPluginExecutionException>(
            () => sut.GetTransactionAutoNumber("contact", Guid.NewGuid(), null, null));

        var query = Assert.IsType<QueryExpression>(Assert.Single(service.Queries));

        var stateCondition = Assert.Single(
            query.Criteria.Conditions.Where(c => c.AttributeName == "statecode"));

        Assert.Equal(ConditionOperator.Equal, stateCondition.Operator);
        Assert.Equal(0, Assert.Single(stateCondition.Values));
    }

    private sealed class StubOrganizationService(params Entity[] results) : IOrganizationService
    {
        private readonly Entity[] _results = results;

        public List<QueryBase> Queries { get; } = [];

        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            Queries.Add(query);

            var collection = new EntityCollection();

            // TopCount is honoured so the stub behaves like the platform: the service asks for two
            // rows to tell "exactly one" from "more than one".
            var take = query is QueryExpression { TopCount: not null } expression
                ? Math.Min(expression.TopCount!.Value, _results.Length)
                : _results.Length;

            for (var i = 0; i < take; i++)
            {
                collection.Entities.Add(_results[i]);
            }

            return collection;
        }

        public Guid Create(Entity entity) => throw new NotSupportedException();

        public void Update(Entity entity) => throw new NotSupportedException();

        public void Delete(string entityName, Guid id) => throw new NotSupportedException();

        public OrganizationResponse Execute(OrganizationRequest request) => throw new NotSupportedException();

        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet) => throw new NotSupportedException();

        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
            => throw new NotSupportedException();

        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
            => throw new NotSupportedException();
    }
}
