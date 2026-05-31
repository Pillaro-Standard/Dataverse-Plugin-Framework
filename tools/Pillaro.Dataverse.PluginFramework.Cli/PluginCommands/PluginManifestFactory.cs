using System.Reflection;
using Pillaro.Dataverse.PluginFramework.PluginRegistrations;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal static class PluginManifestFactory
{
    private const string EmbeddedDiscoveryTypeName = "Pillaro.Dataverse.PluginFramework.PluginRegistrations.PluginRegistrationDiscovery";

    public static PluginManifestDocument CreateFromAssembly(string assemblyPath)
    {
        var fullPath = Path.GetFullPath(assemblyPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Plugin assembly was not found.", fullPath);
        }

        var assembly = Assembly.LoadFrom(fullPath);
        var discovery = PluginRegistrationDiscovery.DiscoverFromAssembly(assembly);
        if (discovery.Registrations.Count == 0)
        {
            return CreateFromEmbeddedDiscovery(fullPath, assembly);
        }

        return CreateDocument(
            fullPath,
            assembly.GetName().Name,
            discovery.PluginTypesWithoutRegistration,
            discovery.Registrations.Select(registration => new ReflectedPluginRegistration(
                registration.PluginType.FullName ?? registration.PluginType.Name,
                registration.Steps.Select(step => new ReflectedPluginStep(
                    step.StepId,
                    step.MessageName,
                    step.EntityName,
                    (int)step.Stage,
                    step.Stage.ToString(),
                    (int)step.Mode,
                    step.Mode.ToString(),
                    step.Rank,
                    step.SolutionName,
                    step.Name,
                    step.FilteringAttributes,
                    step.UnsecureConfiguration,
                    step.Images.Select(image => new ReflectedPluginImage(
                        image.ImageId,
                        image.Type.ToString(),
                        image.Name,
                        image.Attributes)).ToArray(),
                    step.DeploymentPolicy == null
                        ? null
                        : new ReflectedDeploymentPolicy(
                            step.DeploymentPolicy.Risk.ToString(),
                            step.DeploymentPolicy.Reason,
                            step.DeploymentPolicy.Scope.ToString()))).ToArray())));
    }

    private static PluginManifestDocument CreateFromEmbeddedDiscovery(string fullPath, Assembly assembly)
    {
        var discoveryType = assembly.GetType(EmbeddedDiscoveryTypeName);
        if (discoveryType == null)
        {
            return CreateDocument(fullPath, assembly.GetName().Name, [], []);
        }

        var discoverMethod = discoveryType.GetMethod("DiscoverFromAssembly", BindingFlags.Public | BindingFlags.Static, [typeof(Assembly)]);
        if (discoverMethod == null)
        {
            throw new InvalidOperationException($"Embedded plugin registration discovery type '{EmbeddedDiscoveryTypeName}' does not expose DiscoverFromAssembly(Assembly).");
        }

        var discovery = discoverMethod.Invoke(null, [assembly])
            ?? throw new InvalidOperationException("Embedded plugin registration discovery returned null.");

        return CreateDocument(
            fullPath,
            assembly.GetName().Name,
            GetStringCollection(discovery, "PluginTypesWithoutRegistration"),
            GetObjectCollection(discovery, "Registrations").Select(ReadRegistration));
    }

    private static PluginManifestDocument CreateDocument(
        string fullPath,
        string? assemblyName,
        IEnumerable<string> pluginTypesWithoutRegistration,
        IEnumerable<ReflectedPluginRegistration> registrations)
    {
        return new PluginManifestDocument
        {
            AssemblyPath = fullPath,
            AssemblyName = assemblyName,
            GeneratedUtc = DateTime.UtcNow,
            PluginTypesWithoutRegistration = pluginTypesWithoutRegistration.ToList(),
            Plugins = registrations
                .OrderBy(registration => registration.TypeName, StringComparer.OrdinalIgnoreCase)
                .Select(registration => new PluginManifestPlugin
                {
                    TypeName = registration.TypeName,
                    Steps = registration.Steps
                        .OrderBy(step => step.EntityName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(step => step.MessageName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(step => step.Stage)
                        .ThenBy(step => step.Mode)
                        .Select(step => new PluginManifestStep
                        {
                            StepId = step.StepId,
                            MessageName = step.MessageName,
                            EntityName = step.EntityName,
                            Stage = step.Stage,
                            StageName = step.StageName,
                            Mode = step.Mode,
                            ModeName = step.ModeName,
                            Rank = step.Rank,
                            SolutionName = step.SolutionName,
                            Name = step.Name,
                            FilteringAttributes = step.FilteringAttributes.OrderBy(attribute => attribute, StringComparer.OrdinalIgnoreCase).ToList(),
                            UnsecureConfiguration = step.UnsecureConfiguration,
                            Images = step.Images
                                .OrderBy(image => image.Name, StringComparer.OrdinalIgnoreCase)
                                .Select(image => new PluginManifestImage
                                {
                                    ImageId = image.ImageId,
                                    Type = image.Type,
                                    Name = image.Name,
                                    Attributes = image.Attributes.OrderBy(attribute => attribute, StringComparer.OrdinalIgnoreCase).ToList(),
                                })
                                .ToList(),
                            DeploymentPolicy = step.DeploymentPolicy == null
                                ? null
                                : new PluginManifestDeploymentPolicy
                                {
                                    Risk = step.DeploymentPolicy.Risk,
                                    Reason = step.DeploymentPolicy.Reason,
                                    Scope = step.DeploymentPolicy.Scope,
                                }
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private static ReflectedPluginRegistration ReadRegistration(object registration)
    {
        var pluginType = GetRequiredProperty<Type>(registration, "PluginType");
        return new ReflectedPluginRegistration(
            pluginType.FullName ?? pluginType.Name,
            GetObjectCollection(registration, "Steps").Select(ReadStep).ToArray());
    }

    private static ReflectedPluginStep ReadStep(object step)
    {
        return new ReflectedPluginStep(
            GetRequiredProperty<Guid>(step, "StepId"),
            GetRequiredProperty<string>(step, "MessageName"),
            GetOptionalProperty<string>(step, "EntityName"),
            GetEnumValue(step, "Stage"),
            GetEnumName(step, "Stage"),
            GetEnumValue(step, "Mode"),
            GetEnumName(step, "Mode"),
            GetRequiredProperty<int>(step, "Rank"),
            GetRequiredProperty<string>(step, "SolutionName"),
            GetOptionalProperty<string>(step, "Name"),
            GetStringCollection(step, "FilteringAttributes"),
            GetOptionalProperty<string>(step, "UnsecureConfiguration"),
            GetObjectCollection(step, "Images").Select(ReadImage).ToArray(),
            ReadDeploymentPolicy(GetOptionalProperty<object>(step, "DeploymentPolicy")));
    }

    private static ReflectedPluginImage ReadImage(object image)
    {
        return new ReflectedPluginImage(
            GetRequiredProperty<Guid>(image, "ImageId"),
            GetEnumName(image, "Type"),
            GetRequiredProperty<string>(image, "Name"),
            GetStringCollection(image, "Attributes"));
    }

    private static ReflectedDeploymentPolicy? ReadDeploymentPolicy(object? policy)
    {
        return policy == null
            ? null
            : new ReflectedDeploymentPolicy(
                GetEnumName(policy, "Risk"),
                GetRequiredProperty<string>(policy, "Reason"),
                GetEnumName(policy, "Scope"));
    }

    private static IReadOnlyCollection<object> GetObjectCollection(object source, string propertyName)
    {
        return GetRequiredProperty<System.Collections.IEnumerable>(source, propertyName).Cast<object>().ToArray();
    }

    private static IReadOnlyCollection<string> GetStringCollection(object source, string propertyName)
    {
        return GetRequiredProperty<System.Collections.IEnumerable>(source, propertyName)
            .Cast<object>()
            .Select(item => item.ToString())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item!)
            .ToArray();
    }

    private static int GetEnumValue(object source, string propertyName)
    {
        return Convert.ToInt32(GetRequiredProperty<object>(source, propertyName));
    }

    private static string GetEnumName(object source, string propertyName)
    {
        return GetRequiredProperty<object>(source, propertyName).ToString() ?? string.Empty;
    }

    private static T GetRequiredProperty<T>(object source, string propertyName)
    {
        var value = GetOptionalProperty<T>(source, propertyName);
        return value ?? throw new InvalidOperationException($"Plugin registration metadata is missing required property '{propertyName}'.");
    }

    private static T? GetOptionalProperty<T>(object source, string propertyName)
    {
        var property = source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
        if (property == null)
        {
            return default;
        }

        var value = property.GetValue(source);
        return value is T typed ? typed : default;
    }

    private sealed record ReflectedPluginRegistration(
        string TypeName,
        IReadOnlyCollection<ReflectedPluginStep> Steps);

    private sealed record ReflectedPluginStep(
        Guid StepId,
        string MessageName,
        string? EntityName,
        int Stage,
        string StageName,
        int Mode,
        string ModeName,
        int Rank,
        string SolutionName,
        string? Name,
        IReadOnlyCollection<string> FilteringAttributes,
        string? UnsecureConfiguration,
        IReadOnlyCollection<ReflectedPluginImage> Images,
        ReflectedDeploymentPolicy? DeploymentPolicy);

    private sealed record ReflectedPluginImage(
        Guid ImageId,
        string Type,
        string Name,
        IReadOnlyCollection<string> Attributes);

    private sealed record ReflectedDeploymentPolicy(
        string Risk,
        string Reason,
        string Scope);
}
