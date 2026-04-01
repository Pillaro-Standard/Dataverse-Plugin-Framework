using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Pillaro.Dataverse.PluginFramework.Data.Query;

public static class ExpressionExtensions
{
    public static Expression<Func<T, bool>> And<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        if (left == null) return right ?? (x => true);
        if (right == null) return left;
        return Compose(left, right, Expression.AndAlso);
    }

    public static Expression<Func<T, bool>> Or<T>(
        this Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        if (left == null) return right ?? (x => false);
        if (right == null) return left;
        return Compose(left, right, Expression.OrElse);
    }

    public static Expression<Func<T, bool>> AndAll<T>(
        this IEnumerable<Expression<Func<T, bool>>> predicates)
    {
        if (predicates == null) throw new ArgumentNullException(nameof(predicates));

        Expression<Func<T, bool>> acc = null;

        foreach (var predicate in predicates)
        {
            if (predicate == null) continue;
            acc = acc.And(predicate);
        }

        return acc ?? (x => true);
    }

    public static Expression<Func<T, bool>> OrAny<T>(
        this IEnumerable<Expression<Func<T, bool>>> predicates)
    {
        if (predicates == null) throw new ArgumentNullException(nameof(predicates));

        Expression<Func<T, bool>> acc = null;

        foreach (var predicate in predicates)
        {
            if (predicate == null) continue;
            acc = acc.Or(predicate);
        }

        return acc ?? (x => false);
    }

    private static Expression<Func<T, bool>> Compose<T>(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right,
        Func<Expression, Expression, BinaryExpression> merge)
    {
        if (left == null) throw new ArgumentNullException(nameof(left));
        if (right == null) throw new ArgumentNullException(nameof(right));
        if (merge == null) throw new ArgumentNullException(nameof(merge));

        var parameter = Expression.Parameter(typeof(T), "x");

        var leftBody = new ReplaceVisitor(left.Parameters[0], parameter).Visit(left.Body);
        var rightBody = new ReplaceVisitor(right.Parameters[0], parameter).Visit(right.Body);

        return Expression.Lambda<Func<T, bool>>(merge(leftBody, rightBody), parameter);
    }

    private sealed class ReplaceVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _source;
        private readonly ParameterExpression _target;

        public ReplaceVisitor(ParameterExpression source, ParameterExpression target)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _target = target ?? throw new ArgumentNullException(nameof(target));
        }

        protected override Expression VisitParameter(ParameterExpression node)
            => node == _source ? _target : base.VisitParameter(node);
    }
}