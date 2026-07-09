namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal static class PluginManifestValidator
{
    private const int PreValidationStage = 10;
    private const int SynchronousMode = 0;

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

        foreach (var pluginType in manifest.PluginTypesWithoutRegistration)
        {
            _ = pluginType; // reported later in deployment output
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

                ValidateStep(step, stepLabel, stepIds, errors);
                ValidateImages(step, imageIds, errors);
            }
        }

        return errors;
    }

    private static void ValidateStep(
        PluginManifestStep step,
        string stepLabel,
        Dictionary<Guid, string> stepIds,
        List<string> errors)
    {
        if (step.StepId == Guid.Empty)
        {
            errors.Add($"Step ID is required for {stepLabel}.");
        }
        else
        {
            if (IsPlaceholderGuid(step.StepId))
            {
                errors.Add($"Step ID '{step.StepId}' in '{stepLabel}' looks like a placeholder GUID and must be replaced with a real Dataverse step ID.");
            }

            if (!stepIds.TryAdd(step.StepId, stepLabel))
            {
                errors.Add($"Duplicate step ID '{step.StepId}' used by '{stepIds[step.StepId]}' and '{stepLabel}'.");
            }
        }

        if (string.IsNullOrWhiteSpace(step.MessageName))
        {
            errors.Add($"Message name is required for step '{step.StepId}'.");
        }

        if (step.Rank <= 0)
        {
            errors.Add($"Rank must be greater than zero for step '{step.StepId}'.");
        }

        if (IsUpdate(step) && step.Mode == SynchronousMode && !string.IsNullOrWhiteSpace(step.EntityName) && step.FilteringAttributes.Count == 0)
        {
            errors.Add($"Synchronous Update step '{step.StepId}' on entity '{step.EntityName}' should define filtering attributes using WhenChanged(...) to avoid unnecessarily broad execution.");
        }
    }

    private static void ValidateImages(
        PluginManifestStep step,
        Dictionary<Guid, string> imageIds,
        List<string> errors)
    {
        if (step.Images.Count > 0 && step.Stage == PreValidationStage)
        {
            errors.Add($"Step '{step.StepId}' defines images in PreValidation stage. Images should be used only in PreOperation or PostOperation stages.");
        }

        var imageNameTypeInStep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var image in step.Images)
        {
            var imageLabel = $"{step.StepId} / {image.Name}";

            if (image.ImageId == Guid.Empty)
            {
                errors.Add($"Image ID is required for step '{step.StepId}' image '{image.Name}'.");
            }
            else
            {
                if (IsPlaceholderGuid(image.ImageId))
                {
                    errors.Add($"Image ID '{image.ImageId}' in step '{step.StepId}' image '{image.Name}' looks like a placeholder GUID and must be replaced with a real Dataverse image ID.");
                }

                if (!imageIds.TryAdd(image.ImageId, imageLabel))
                {
                    errors.Add($"Duplicate image ID '{image.ImageId}' used by '{imageIds[image.ImageId]}' and '{imageLabel}'.");
                }
            }

            if (string.IsNullOrWhiteSpace(image.Name))
            {
                errors.Add($"Image name is required for step '{step.StepId}'.");
            }
            else
            {
                var imageNameTypeKey = $"{image.Name}:{image.Type}";
                if (!imageNameTypeInStep.Add(imageNameTypeKey))
                {
                    errors.Add($"Duplicate {image.Type} image named '{image.Name}' in step '{step.StepId}'.");
                }
            }

            if (image.Attributes.Count == 0)
            {
                errors.Add($"Image '{image.Name}' in step '{step.StepId}' must define at least one attribute.");
            }

            ValidateImageMessageCompatibility(step, image, errors);
        }
    }

    private static void ValidateImageMessageCompatibility(PluginManifestStep step, PluginManifestImage image, List<string> errors)
    {
        if (IsCreate(step) && IsPreImage(image))
        {
            errors.Add($"Create step '{step.StepId}' cannot define pre-image '{image.Name}'. Use a post-image for Create steps.");
        }

        if (IsDelete(step) && IsPostImage(image))
        {
            errors.Add($"Delete step '{step.StepId}' cannot define post-image '{image.Name}'. Use a pre-image for Delete steps.");
        }
    }

    private static bool IsCreate(PluginManifestStep step) => string.Equals(step.MessageName, "Create", StringComparison.OrdinalIgnoreCase);

    private static bool IsUpdate(PluginManifestStep step) => string.Equals(step.MessageName, "Update", StringComparison.OrdinalIgnoreCase);

    private static bool IsDelete(PluginManifestStep step) => string.Equals(step.MessageName, "Delete", StringComparison.OrdinalIgnoreCase);

    private static bool IsPreImage(PluginManifestImage image) => string.Equals(image.Type, "PreImage", StringComparison.OrdinalIgnoreCase);

    private static bool IsPostImage(PluginManifestImage image) => string.Equals(image.Type, "PostImage", StringComparison.OrdinalIgnoreCase);

    private static bool IsPlaceholderGuid(Guid id)
    {
        return id.ToString("D").StartsWith("00000000-0000-0000-0000-", StringComparison.OrdinalIgnoreCase);
    }
}
