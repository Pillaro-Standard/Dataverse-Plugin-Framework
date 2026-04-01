using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Pillaro.Dataverse.PluginFramework.Data.Query;

internal static class QueryExecutor
{
    public static IQueryable<TEntity> BuildQuery<TEntity>(
        OrganizationServiceContext ctx,
        QueryState<TEntity> state)
        where TEntity : Entity
    {
        IQueryable<TEntity> q = ctx.CreateQuery<TEntity>();

        if (state.Predicate != null)
            q = q.Where(state.Predicate);

        var order = state.EnsureOrderIfPaging();
        if (order != null)
            q = order(q);

        return q;
    }

    public static List<TEntity> Execute<TEntity>(
        OrganizationServiceContext ctx,
        QueryState<TEntity> state)
        where TEntity : Entity
    {
        var q = BuildQuery(ctx, state);

        try
        {
            if (state.Skip.HasValue) q = q.Skip(state.Skip.Value);
            if (state.Take.HasValue) q = q.Take(state.Take.Value);
            return q.ToList();
        }
        catch (NotSupportedException)
        {
            var safeTake = (state.Skip ?? 0) + (state.Take ?? int.MaxValue / 4);
            var list = q.Take(safeTake).ToList();

            if (state.Skip.HasValue) list = list.Skip(state.Skip.Value).ToList();
            if (state.Take.HasValue) list = list.Take(state.Take.Value).ToList();

            return list;
        }
    }

    public static List<TResult> Execute<TEntity, TResult>(
        OrganizationServiceContext ctx,
        QueryState<TEntity> state,
        Expression<Func<TEntity, TResult>> selector)
        where TEntity : Entity
    {
        var q = BuildQuery(ctx, state);

        try
        {
            if (state.Skip.HasValue) q = q.Skip(state.Skip.Value);
            if (state.Take.HasValue) q = q.Take(state.Take.Value);

            return q.Select(selector).ToList();
        }
        catch (NotSupportedException)
        {
            var safeTake = (state.Skip ?? 0) + (state.Take ?? int.MaxValue / 4);
            var list = q.Take(safeTake)
                        .ToList()
                        .Select(selector.Compile())
                        .ToList();

            if (state.Skip.HasValue) list = list.Skip(state.Skip.Value).ToList();
            if (state.Take.HasValue) list = list.Take(state.Take.Value).ToList();

            return list;
        }
    }
}
