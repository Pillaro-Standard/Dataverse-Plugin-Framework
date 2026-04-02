using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Pillaro.Dataverse.PluginFramework.Data.Query;

public class EntityQuery<TEntity> where TEntity : Entity
{
    private readonly Func<OrganizationServiceContext> _ctxFactory;
    private readonly QueryState<TEntity> _state;

    public EntityQuery(Func<OrganizationServiceContext> ctxFactory)
        : this(ctxFactory, new QueryState<TEntity>())
    {
    }

    internal EntityQuery(
        Func<OrganizationServiceContext> ctxFactory,
        QueryState<TEntity> state)
    {
        _ctxFactory = ctxFactory ?? throw new ArgumentNullException(nameof(ctxFactory));
        _state = state ?? throw new ArgumentNullException(nameof(state));
    }

    public EntityQuery<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        return new EntityQuery<TEntity>(_ctxFactory, _state.WithWhere(predicate));
    }

    public EntityQuery<TEntity> OrderBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        return new EntityQuery<TEntity>(_ctxFactory, _state.WithOrderBy(keySelector));
    }

    public EntityQuery<TEntity> OrderByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        return new EntityQuery<TEntity>(_ctxFactory, _state.WithOrderByDescending(keySelector));
    }

    public EntityQuery<TEntity> ThenBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        return new EntityQuery<TEntity>(_ctxFactory, _state.WithThenBy(keySelector));
    }

    public EntityQuery<TEntity> ThenByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        return new EntityQuery<TEntity>(_ctxFactory, _state.WithThenByDescending(keySelector));
    }

    public EntityQuery<TEntity> Take(int take)
    {
        if (take < 0) throw new ArgumentOutOfRangeException(nameof(take));
        return new EntityQuery<TEntity>(_ctxFactory, _state.WithTake(take));
    }

    public EntityQuery<TEntity> Skip(int skip)
    {
        if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
        return new EntityQuery<TEntity>(_ctxFactory, _state.WithSkip(skip));
    }

    /// <summary>1-based pageNumber</summary>
    public EntityQuery<TEntity> Page(int pageNumber, int pageSize)
        => new(_ctxFactory, _state.WithPage(pageNumber, pageSize));

    public EntityQuery<TEntity, TObject> Select<TObject>(Expression<Func<TEntity, TObject>> selector)
    {
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        return new EntityQuery<TEntity, TObject>(_ctxFactory, _state, selector);
    }

    public List<TEntity> ToList()
    {
        using var ctx = _ctxFactory();
        ctx.MergeOption = MergeOption.NoTracking;
        return QueryExecutor.Execute(ctx, _state);
    }

    public TEntity First() => Take(1).ToList().First();

    public TEntity FirstOrDefault() => Take(1).ToList().FirstOrDefault();

    public TEntity Single() => Take(2).ToList().Single();

    public TEntity SingleOrDefault() => Take(2).ToList().SingleOrDefault();
}