using Pillaro.Dataverse.PluginFramework.PluginRegistrations;
using System.Reflection;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal static class PluginManifestFactory
{
    public static PluginManifestDocument CreateFromAssembly(string assemblyPath)
    {
        var fullPath = Path.GetFullPath(assemblyPath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("Plugin assembly was not found.", fullPath);
        }

        var assembly = Assembly.LoadFrom(fullPath);
        var descriptors = PluginRegistrationDiscovery.DiscoverFromAssembly(assembly);

        return new PluginManifestDocument
        {
            AssemblyPath = fullPath,
            AssemblyName = assembly.GetName().Name,
            GeneratedUtc = DateTime.UtcNow,
            Plugins = descriptors
                .OrderBy(descriptor => descriptor.PluginType.FullName, StringComparer.OrdinalIgnoreCase)
                .Select(descriptor => new PluginManifestPlugin
                {
                    TypeName = descriptor.PluginType.FullName ?? descriptor.PluginType.Name,
                    Steps = descriptor.Steps
                        .OrderBy(step => step.EntityName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(step => step.MessageName, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(step => step.Stage)
                        .ThenBy(step => step.Mode)
                        .Select(step => new PluginManifestStep
                        {
                            StepId = step.StepId,
                            MessageName = step.MessageName,
                            EntityName = step.EntityName,
                            Stage = (int)step.Stage,
                            StageName = step.Stage.ToString(),
                            Mode = (int)step.Mode,
                            ModeName = step.Mode.ToString(),
                            Rank = step.Rank,
                            SolutionName = step.SolutionName,
                            FilteringAttributes = step.FilteringAttributes.OrderBy(attribute => attribute, StringComparer.OrdinalIgnoreCase).ToList(),
                            Images = step.Images
                                .OrderBy(image => image.Name, StringComparer.OrdinalIgnoreCase)
                                .Select(image => new PluginManifestImage
                                {
                                    ImageId = image.ImageId,
                                    Type = image.Type.ToString(),
                                    Name = image.Name,
                                    Attributes = image.Attributes.OrderBy(attribute => attribute, StringComparer.OrdinalIgnoreCase).ToList(),
                                })
                                .ToList(),
                            DeploymentPolicy = step.DeploymentPolicy == null
                                ? null
                                : new PluginManifestDeploymentPolicy
                                {
                                    RequiresConfirmation = step.DeploymentPolicy.RequiresConfirmation,
                                    Risk = step.DeploymentPolicy.Risk.ToString(),
                                    Reason = step.DeploymentPolicy.Reason,
                                    Scope = step.DeploymentPolicy.Scope.ToString(),
                                }
                        })
                        .ToList()
                })
                .ToList()
        };
    }
}
