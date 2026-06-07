using Microsoft.Xrm.Sdk;
using System.Linq.Expressions;
using System.Reflection;

namespace Pillaro.Dataverse.PluginFramework.PluginRegistrations;

internal static class TypedAttributeSelector
{
    public static IReadOnlyCollection<string> GetLogicalNames<TEntity>(IReadOnlyCollection<Expression<Func<TEntity, object>>> attributes)
        where TEntity : Entity
    {
        if (attributes == null || attributes.Count == 0)
        {
            throw new ArgumentException("At least one attribute expression must be provided.", nameof(attributes));
        }

        return attributes
            .Select(GetLogicalName)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string GetLogicalName<TEntity>(Expression<Func<TEntity, object>> expression)
        where TEntity : Entity
    {
        if (expression == null)
        {
            throw new ArgumentNullException(nameof(expression));
        }

        var member = GetMemberExpression(expression.Body);
        if (member?.Member is not PropertyInfo property)
        {
            throw new ArgumentException($"Expression '{expression}' must point to an early-bound entity property.", nameof(expression));
        }

        var attribute = property.GetCustomAttribute<AttributeLogicalNameAttribute>();
        if (attribute == null || string.IsNullOrWhiteSpace(attribute.LogicalName))
        {
            throw new ArgumentException($"Property '{typeof(TEntity).FullName}.{property.Name}' must be decorated with AttributeLogicalNameAttribute.", nameof(expression));
        }

        return attribute.LogicalName;
    }

    private static MemberExpression GetMemberExpression(Expression expression)
    {
        if (expression is MemberExpression memberExpression)
        {
            return memberExpression;
        }

        if (expression is UnaryExpression unaryExpression && unaryExpression.NodeType == ExpressionType.Convert)
        {
            return GetMemberExpression(unaryExpression.Operand);
        }

        return null;
    }
}
