using System;
using Microsoft.Xrm.Sdk;

namespace Pillaro.Dataverse.PluginFramework.Utilities;

public static class EnumExtensions
{
    public static T ToEnum<T>(this string value, bool ignoreCase = false) where T : struct
    {
        if (!typeof(T).IsEnum)
            throw new ArgumentException("T must be an enum type.", nameof(T));

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null, empty, or whitespace.", nameof(value));

        if (!Enum.TryParse(value, ignoreCase, out T result))
            throw new ArgumentException($"'{value}' is not a valid value for enum '{typeof(T).Name}'.", nameof(value));

        if (!Enum.IsDefined(typeof(T), result))
            throw new ArgumentException($"'{value}' is not defined in enum '{typeof(T).Name}'.", nameof(value));

        return result;
    }

    public static T ToEnum<T>(this int value) where T : struct
    {
        if (!typeof(T).IsEnum)
            throw new ArgumentException("T must be an enum type.", nameof(T));

        T result = (T)Enum.ToObject(typeof(T), value);

        if (!Enum.IsDefined(typeof(T), result))
            throw new ArgumentException($"'{value}' is not defined in enum '{typeof(T).Name}'.", nameof(value));

        return result;
    }

    public static T ToEnum<T>(this OptionSetValue value) where T : struct
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        return value.Value.ToEnum<T>();
    }

    public static bool TryToEnum<T>(this string value, out T result, bool ignoreCase = false) where T : struct
    {
        result = default(T);

        if (!typeof(T).IsEnum || string.IsNullOrWhiteSpace(value))
            return false;

        if (!Enum.TryParse(value, ignoreCase, out T parsed))
            return false;

        if (!Enum.IsDefined(typeof(T), parsed))
            return false;

        result = parsed;
        return true;
    }

    public static bool TryToEnum<T>(this int value, out T result) where T : struct
    {
        result = default(T);

        if (!typeof(T).IsEnum)
            return false;

        var parsed = (T)Enum.ToObject(typeof(T), value);

        if (!Enum.IsDefined(typeof(T), parsed))
            return false;

        result = parsed;
        return true;
    }

    public static bool TryToEnum<T>(this OptionSetValue value, out T result) where T : struct
    {
        result = default(T);

        if (value == null)
            return false;

        return value.Value.TryToEnum(out result);
    }
}