using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Plugins;
using System.Reflection;

namespace Pillaro.Dataverse.PluginFramework.PluginRegistrations;

public static class PluginRegistrationDiscovery
{
    public static PluginRegistrationDescriptor? Discover<TPlugin>()
        where TPlugin : IPlugin
    {
        return Discover(typeof(TPlugin));
    }

    public static PluginRegistrationDescriptor? Discover(Type pluginType)
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

        var plugin = CreatePluginInstance(pluginType);

        try
        {
            plugin.Register(builder);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Plugin '{pluginType.FullName}': Register(IPluginRegistration) threw an exception: {UnwrapMessage(ex)}",
                Unwrap(ex));
        }

        PluginRegistrationDescriptor descriptor;
        try
        {
            descriptor = (PluginRegistrationDescriptor)builderType
                .GetMethod(nameof(PluginRegistrationBuilder<IPlugin>.Build), BindingFlags.Instance | BindingFlags.Public)!
                .Invoke(builder, null)!;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Plugin '{pluginType.FullName}': registration build failed: {UnwrapMessage(ex)}",
                Unwrap(ex));
        }

        if (descriptor.Steps == null || descriptor.Steps.Count == 0)
        {
            return null;
        }

        return descriptor;
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

        var descriptors = new List<PluginRegistrationDescriptor>();
        var skipped = new List<string>();

        foreach (var type in pluginTypes)
        {
            // If plugin does not have required ctor, skip it
            if (!HasRegistrationMethod(type))
            {
                skipped.Add(type.FullName ?? type.Name);
                continue;
            }

            try
            {
                var descriptor = Discover(type);
                if (descriptor != null)
                    descriptors.Add(descriptor);
                else
                    skipped.Add(type.FullName ?? type.Name);
            }
            catch (Exception ex)
            {
                // Don't let a single failing plugin discovery stop the whole assembly discovery.
                Console.WriteLine($"[WARN] Failed to discover plugin '{type.FullName}': {UnwrapMessage(ex)}");
                skipped.Add(type.FullName ?? type.Name);
            }
        }

        return new PluginRegistrationDiscoveryResult(descriptors.ToArray(), skipped.ToArray());
    }

    private static PluginBase CreatePluginInstance(Type pluginType)
    {
        if (!typeof(PluginBase).IsAssignableFrom(pluginType))
        {
            throw new InvalidOperationException($"Plugin type '{pluginType.FullName}' must inherit '{typeof(PluginBase).FullName}' and implement Register(IPluginRegistration registration).");
        }

        var constructor = pluginType.GetConstructor([typeof(string), typeof(string)]);
        if (constructor == null)
        {
            throw new InvalidOperationException($"Plugin type '{pluginType.FullName}' must have a public constructor with string unsecureConfig and string secureConfig.");
        }

        return (PluginBase)constructor.Invoke([string.Empty, string.Empty]);
    }

    private static bool HasRegistrationMethod(Type pluginType)
    {
        return typeof(PluginBase).IsAssignableFrom(pluginType)
            && pluginType.GetConstructor([typeof(string), typeof(string)]) != null;
    }

    private static Exception Unwrap(Exception ex)
    {
        while (ex is TargetInvocationException { InnerException: not null } tie)
            ex = tie.InnerException;
        return ex;
    }

    private static string UnwrapMessage(Exception ex)
    {
        ex = Unwrap(ex);
        var messages = new List<string>();
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (!string.IsNullOrWhiteSpace(e.Message))
                messages.Add(e.Message);
        }
        return string.Join(" → ", messages.Distinct(StringComparer.Ordinal));
    }
}

public sealed record PluginRegistrationDiscoveryResult(
    IReadOnlyCollection<PluginRegistrationDescriptor> Registrations,
    IReadOnlyCollection<string> PluginTypesWithoutRegistration);
