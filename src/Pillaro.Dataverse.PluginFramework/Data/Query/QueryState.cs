using Microsoft.Xrm.Sdk;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Pillaro.Dataverse.PluginFramework.Data.Query;

internal sealed class QueryState<TEntity> where TEntity : Entity
{
    public Expression<Func<TEntity, bool>> Predicate { get; }
    public Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> OrderBy { get; }
    public int? Take { get; }
    public int? Skip { get; }

    public QueryState(
        Expression<Func<TEntity, bool>> predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> orderBy = null,
        int? take = null,
        int? skip = null)
    {
        Predicate = predicate;
        OrderBy = orderBy;
        Take = take;
        Skip = skip;
    }

    public QueryState<TEntity> WithWhere(Expression<Func<TEntity, bool>> predicate)
    {
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        return new QueryState<TEntity>(
            Predicate == null ? predicate : Predicate.And(predicate),
            OrderBy,
            Take,
            Skip);
    }

    public QueryState<TEntity> WithTake(int take)
    {
        if (take < 0) throw new ArgumentOutOfRangeException(nameof(take));
        return new QueryState<TEntity>(Predicate, OrderBy, take, Skip);
    }

    public QueryState<TEntity> WithSkip(int skip)
    {
        if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
        return new QueryState<TEntity>(Predicate, OrderBy, Take, skip);
    }

    public QueryState<TEntity> WithPage(int pageNumber, int pageSize)
    {
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize));

        return new QueryState<TEntity>(
            Predicate,
            OrderBy,
            pageSize,
            (pageNumber - 1) * pageSize);
    }

    public QueryState<TEntity> WithOrderBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        return new QueryState<TEntity>(Predicate, q => q.OrderBy(keySelector), Take, Skip);
    }

    public QueryState<TEntity> WithOrderByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
        return new QueryState<TEntity>(Predicate, q => q.OrderByDescending(keySelector), Take, Skip);
    }

    public QueryState<TEntity> WithThenBy<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

        var prev = OrderBy;
        return new QueryState<TEntity>(
            Predicate,
            q => (prev?.Invoke(q) ?? q.OrderBy(x => x.Id)).ThenBy(keySelector),
            Take,
            Skip);
    }

    public QueryState<TEntity> WithThenByDescending<TKey>(Expression<Func<TEntity, TKey>> keySelector)
    {
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

        var prev = OrderBy;
        return new QueryState<TEntity>(
            Predicate,
            q => (prev?.Invoke(q) ?? q.OrderBy(x => x.Id)).ThenByDescending(keySelector),
            Take,
            Skip);
    }

    public Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>> EnsureOrderIfPaging()
    {
        if ((Skip.HasValue || Take.HasValue) && OrderBy == null)
            return q => q.OrderBy(e => e.Id);

        return OrderBy;
    }
}