namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal static class PluginManifestValidator
{
    public static IReadOnlyCollection<string> Validate(PluginManifestDocument manifest)
    {
        var errors = new List<string>();

        if (manifest == null)
        {
            return ["Manifest is required."];
        }

        if (manifest.Plugins.Count == 0)
        {
            errors.Add("Manifest does not contain any plugins.");
        }

        var stepIds = new Dictionary<Guid, string>();
        var imageIds = new Dictionary<Guid, string>();

        foreach (var plugin in manifest.Plugins)
        {
            if (string.IsNullOrWhiteSpace(plugin.TypeName))
            {
                errors.Add("Plugin type name is required.");
            }

            if (plugin.Steps.Count == 0)
            {
                errors.Add($"Plugin '{plugin.TypeName}' does not contain any steps.");
            }

            foreach (var step in plugin.Steps)
            {
                var stepLabel = $"{plugin.TypeName} / {step.MessageName} / {step.EntityName ?? "<none>"} / {step.StageName} / {step.ModeName}";

                if (step.StepId == Guid.Empty)
                {
                    errors.Add($"Step ID is required for {stepLabel}.");
                }
                else if (!stepIds.TryAdd(step.StepId, stepLabel))
                {
                    errors.Add($"Duplicate step ID '{step.StepId}' used by '{stepIds[step.StepId]}' and '{stepLabel}'.");
                }

                if (string.IsNullOrWhiteSpace(step.MessageName))
                {
                    errors.Add($"Message name is required for step '{step.StepId}'.");
                }

                if (step.Rank <= 0)
                {
                    errors.Add($"Rank must be greater than zero for step '{step.StepId}'.");
                }

                if (!string.Equals(step.MessageName, "Update", StringComparison.OrdinalIgnoreCase) && step.FilteringAttributes.Count > 0)
                {
                    errors.Add($"Filtering attributes can be used only for Update steps. Step '{step.StepId}' uses message '{step.MessageName}'.");
                }

                var imageNamesInStep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var image in step.Images)
                {
                    var imageLabel = $"{step.StepId} / {image.Name}";

                    if (image.ImageId == Guid.Empty)
                    {
                        errors.Add($"Image ID is required for step '{step.StepId}' image '{image.Name}'.");
                    }
                    else if (!imageIds.TryAdd(image.ImageId, imageLabel))
                    {
                        errors.Add($"Duplicate image ID '{image.ImageId}' used by '{imageIds[image.ImageId]}' and '{imageLabel}'.");
                    }

                    if (string.IsNullOrWhiteSpace(image.Name))
                    {
                        errors.Add($"Image name is required for step '{step.StepId}'.");
                    }
                    else if (!imageNamesInStep.Add(image.Name))
                    {
                        errors.Add($"Duplicate image name '{image.Name}' in step '{step.StepId}'.");
                    }

                    if (image.Attributes.Count == 0)
                    {
                        errors.Add($"Image '{image.Name}' in step '{step.StepId}' must define at least one attribute.");
                    }
                }

                if (step.DeploymentPolicy != null && step.DeploymentPolicy.RequiresConfirmation && string.IsNullOrWhiteSpace(step.DeploymentPolicy.Reason))
                {
                    errors.Add($"Step '{step.StepId}' requires confirmation but does not define a reason.");
                }
            }
        }

        return errors;
    }
}
