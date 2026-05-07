using Microsoft.Xrm.Sdk;
using System.Reflection;

namespace Pillaro.Dataverse.PluginFramework.PluginRegistrations;

public static class PluginRegistrationDiscovery
{
    private const string RegisterMethodName = "Register";

    public static PluginRegistrationDescriptor Discover<TPlugin>()
        where TPlugin : IPlugin
    {
        return Discover(typeof(TPlugin));
    }

    public static PluginRegistrationDescriptor Discover(Type pluginType)
    {
        if (pluginType == null)
        {
            throw new ArgumentNullException(nameof(pluginType));
        }

        if (!typeof(IPlugin).IsAssignableFrom(pluginType))
        {
            throw new ArgumentException($"Type '{pluginType.FullName}' must implement '{nameof(IPlugin)}'.", nameof(pluginType));
        }

        var builderType = typeof(PluginRegistrationBuilder<>).MakeGenericType(pluginType);
        var builder = (IPluginRegistration)Activator.CreateInstance(builderType)!;

        var registerMethod = GetRegisterMethod(pluginType);
        registerMethod.Invoke(null, [builder]);

        return (PluginRegistrationDescriptor)builderType
            .GetMethod(nameof(PluginRegistrationBuilder<IPlugin>.Build), BindingFlags.Instance | BindingFlags.Public)!
            .Invoke(builder, null)!;
    }

    public static PluginRegistrationDiscoveryResult DiscoverFromAssembly(Assembly assembly)
    {
        if (assembly == null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        var pluginTypes = assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(IPlugin).IsAssignableFrom(type))
            .OrderBy(type => type.FullName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var registeredPluginTypes = pluginTypes.Where(HasRegisterMethod).ToArray();
        var skippedPluginTypes = pluginTypes.Except(registeredPluginTypes).Select(type => type.FullName ?? type.Name).ToArray();

        return new PluginRegistrationDiscoveryResult(
            registeredPluginTypes.Select(Discover).ToArray(),
            skippedPluginTypes);
    }

    private static MethodInfo GetRegisterMethod(Type pluginType)
    {
        var method = pluginType.GetMethod(RegisterMethodName, BindingFlags.Public | BindingFlags.Static);
        if (method == null)
        {
            throw new InvalidOperationException($"Plugin type '{pluginType.FullName}' must define public static void {RegisterMethodName}({nameof(IPluginRegistration)} registration).");
        }

        var parameters = method.GetParameters();
        if (method.ReturnType != typeof(void) || parameters.Length != 1 || parameters[0].ParameterType != typeof(IPluginRegistration))
        {
            throw new InvalidOperationException($"Plugin type '{pluginType.FullName}' has invalid registration method signature. Expected public static void {RegisterMethodName}({nameof(IPluginRegistration)} registration).");
        }

        return method;
    }

    private static bool HasRegisterMethod(Type pluginType)
    {
        var method = pluginType.GetMethod(RegisterMethodName, BindingFlags.Public | BindingFlags.Static);
        if (method == null)
        {
            return false;
        }

        var parameters = method.GetParameters();
        return method.ReturnType == typeof(void)
            && parameters.Length == 1
            && parameters[0].ParameterType == typeof(IPluginRegistration);
    }
}

public sealed record PluginRegistrationDiscoveryResult(
    IReadOnlyCollection<PluginRegistrationDescriptor> Registrations,
    IReadOnlyCollection<string> PluginTypesWithoutRegistration);
