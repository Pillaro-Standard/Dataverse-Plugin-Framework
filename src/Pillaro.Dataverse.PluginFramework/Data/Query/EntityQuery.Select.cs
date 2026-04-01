using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Pillaro.Dataverse.PluginFramework.Data.Query;

public class EntityQuery<TEntity, TObject> where TEntity : Entity
{
    private readonly Func<OrganizationServiceContext> _ctxFactory;
    private readonly QueryState<TEntity> _state;
    private readonly Expression<Func<TEntity, TObject>> _selector;

    internal EntityQuery(
        Func<OrganizationServiceContext> ctxFactory,
        QueryState<TEntity> state,
        Expression<Func<TEntity, TObject>> selector)
    {
        _ctxFactory = ctxFactory ?? throw new ArgumentNullException(nameof(ctxFactory));
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _selector = selector ?? throw new ArgumentNullException(nameof(selector));
    }

    public EntityQuery<TEntity, TObject> Where(Expression<Func<TEntity, bool>> predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));
        return new EntityQuery<TEntity, TObject>(_ctxFactory, _state.WithWhere(predicate), _selector);
    }

    public EntityQuery<TEntity, TObject> OrderBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        return new EntityQuery<TEntity, TObject>(_ctxFactory, _state.WithOrderBy(keySelector), _selector);
    }

    public EntityQuery<TEntity, TObject> OrderByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        return new EntityQuery<TEntity, TObject>(_ctxFactory, _state.WithOrderByDescending(keySelector), _selector);
    }

    public EntityQuery<TEntity, TObject> ThenBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        return new EntityQuery<TEntity, TObject>(_ctxFactory, _state.WithThenBy(keySelector), _selector);
    }

    public EntityQuery<TEntity, TObject> ThenByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        return new EntityQuery<TEntity, TObject>(_ctxFactory, _state.WithThenByDescending(keySelector), _selector);
    }

    public EntityQuery<TEntity, TObject> Take(int take)
    {
        if (take < 0) throw new ArgumentOutOfRangeException(nameof(take));
        return new EntityQuery<TEntity, TObject>(_ctxFactory, _state.WithTake(take), _selector);
    }

    public EntityQuery<TEntity, TObject> Skip(int skip)
    {
        if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
        return new EntityQuery<TEntity, TObject>(_ctxFactory, _state.WithSkip(skip), _selector);
    }

    /// <summary>1-based pageNumber</summary>
    public EntityQuery<TEntity, TObject> Page(int pageNumber, int pageSize)
        => new EntityQuery<TEntity, TObject>(_ctxFactory, _state.WithPage(pageNumber, pageSize), _selector);

    public List<TObject> ToList()
    {
        using var ctx = _ctxFactory();
        ctx.MergeOption = MergeOption.NoTracking;
        return QueryExecutor.Execute(ctx, _state, _selector);
    }

    public TObject First() => Take(1).ToList().First();

    public TObject FirstOrDefault() => Take(1).ToList().FirstOrDefault();

    public TObject Single() => Take(2).ToList().Single();

    public TObject SingleOrDefault() => Take(2).ToList().SingleOrDefault();

}