using Microsoft.Xrm.Sdk;
using System;

namespace Pillaro.Dataverse.PluginFramework.Tasks;

public static class ExecutionContextParameters
{
    public const string AssigneeParam = "Assignee";
    public const string TargetParam = "Target";

    public const string StateParam = "State";
    public const string StatusParam = "Status";

    public static EntityReference GetEntityReferenceAssignee(IExecutionContext context, bool throwException = true)
    {
        return GetEntityReference(context, AssigneeParam, throwException);
    }

    public static EntityReference GetEntityReferenceTarget(IExecutionContext context, bool throwException = true)
    {
        return GetEntityReference(context, TargetParam, throwException);
    }

    public static T GetEntityTarget<T>(IExecutionContext context, bool throwException = true)
        where T : Entity
    {
        return GetEntity<T>(context, TargetParam, throwException);
    }

    public static EntityReference GetEntityReference(IExecutionContext context, string param, bool throwException = true)
    {
        ValidateContext(context);
        ValidateParamName(param);

        if (!context.InputParameters.ContainsKey(param))
        {
            if (throwException)
                throw new InvalidPluginExecutionException($"Input parameter '{param}' was not found in execution context.");

            return null;
        }

        var value = context.InputParameters[param];

        if (value == null)
        {
            if (throwException)
                throw new InvalidPluginExecutionException($"Input parameter '{param}' is null.");

            return null;
        }

        if (value is EntityReference entityReference)
            return entityReference;

        if (value is Entity entity)
            return entity.ToEntityReference();

        if (throwException)
            throw new InvalidPluginExecutionException(
                $"Input parameter '{param}' is of type '{value.GetType().FullName}', expected '{typeof(EntityReference).FullName}' or '{typeof(Entity).FullName}'.");

        return null;
    }

    public static T GetParam<T>(IExecutionContext context, string param, bool throwException = true)
    {
        ValidateContext(context);
        ValidateParamName(param);

        if (!context.InputParameters.ContainsKey(param))
        {
            if (throwException)
                throw new InvalidPluginExecutionException($"Input parameter '{param}' was not found in execution context.");

            return default(T);
        }

        var value = context.InputParameters[param];

        if (value == null)
        {
            if (throwException)
                throw new InvalidPluginExecutionException($"Input parameter '{param}' is null.");

            return default(T);
        }

        if (value is T typedValue)
            return typedValue;

        if (throwException)
            throw new InvalidPluginExecutionException(
                $"Input parameter '{param}' is of type '{value.GetType().FullName}', expected '{typeof(T).FullName}'.");

        return default(T);
    }

    public static T GetEntity<T>(IExecutionContext context, string param, bool throwException = true)
        where T : Entity
    {
        ValidateContext(context);
        ValidateParamName(param);

        if (!context.InputParameters.ContainsKey(param))
        {
            if (throwException)
                throw new InvalidPluginExecutionException($"Input parameter '{param}' was not found in execution context.");

            return null;
        }

        var value = context.InputParameters[param];

        if (value == null)
        {
            if (throwException)
                throw new InvalidPluginExecutionException($"Input parameter '{param}' is null.");

            return null;
        }

        if (!(value is Entity entity))
        {
            if (throwException)
                throw new InvalidPluginExecutionException(
                    $"Input parameter '{param}' is of type '{value.GetType().FullName}', expected '{typeof(Entity).FullName}'.");

            return null;
        }

        return entity.ToEntity<T>();
    }

    public static bool TryGetParam<T>(IExecutionContext context, string param, out T result)
    {
        result = default(T);

        if (context == null || string.IsNullOrWhiteSpace(param))
            return false;

        if (!context.InputParameters.ContainsKey(param))
            return false;

        var value = context.InputParameters[param];

        if (!(value is T typedValue))
            return false;

        result = typedValue;
        return true;
    }

    public static bool TryGetEntity<T>(IExecutionContext context, string param, out T result)
        where T : Entity
    {
        result = null;

        if (context == null || string.IsNullOrWhiteSpace(param))
            return false;

        if (!context.InputParameters.ContainsKey(param))
            return false;

        if (!(context.InputParameters[param] is Entity entity))
            return false;

        result = entity.ToEntity<T>();
        return result != null;
    }

    public static bool TryGetEntityReference(IExecutionContext context, string param, out EntityReference result)
    {
        result = null;

        if (context == null || string.IsNullOrWhiteSpace(param))
            return false;

        if (!context.InputParameters.ContainsKey(param))
            return false;

        var value = context.InputParameters[param];

        if (value is EntityReference entityReference)
        {
            result = entityReference;
            return true;
        }

        if (value is Entity entity)
        {
            result = entity.ToEntityReference();
            return true;
        }

        return false;
    }

    private static void ValidateContext(IExecutionContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
    }

    private static void ValidateParamName(string param)
    {
        if (string.IsNullOrWhiteSpace(param))
            throw new ArgumentException("Parameter name cannot be null, empty, or whitespace.", nameof(param));
    }
}