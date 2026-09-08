namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal static class PluginManifestValidator
{
    private const int PostOperationStage = 40;
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

        if (step.IsMainOperation && (IsCreate(step) || IsUpdate(step) || IsDelete(step)))
        {
            errors.Add($"Step '{step.StepId}' registers the MainOperation stage for platform message '{step.MessageName}'. MainOperation is supported only for Custom API messages; use PreValidation, PreOperation, or PostOperation for platform messages.");
        }
    }

    private static void ValidateImages(
        PluginManifestStep step,
        Dictionary<Guid, string> imageIds,
        List<string> errors)
    {
        if (step.Images.Count > 0 && step.IsMainOperation)
        {
            errors.Add($"Step '{step.StepId}' defines images in MainOperation stage. A Custom API MainOperation registration deploys the plugin type only and cannot register images.");
        }

        // Dataverse keys the pre-image and post-image collections by entity alias, so a key may repeat
        // across the two collections but not within one. A 'Both' image occupies a key in both.
        var preImageKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var postImageKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
                var key = image.ResolvedEntityAlias;
                if (image.IsPreImage && !preImageKeys.Add(key))
                {
                    errors.Add($"Duplicate pre-image key '{key}' in step '{step.StepId}'. Entity aliases must be unique within the pre-image collection.");
                }

                if (image.IsPostImage && !postImageKeys.Add(key))
                {
                    errors.Add($"Duplicate post-image key '{key}' in step '{step.StepId}'. Entity aliases must be unique within the post-image collection.");
                }
            }

            if (image.Attributes.Count == 0)
            {
                errors.Add($"Image '{image.Name}' in step '{step.StepId}' must define at least one attribute.");
            }

            ValidateImageStageCompatibility(step, image, errors);
            ValidateImageMessageCompatibility(step, image, errors);
        }
    }

    private static void ValidateImageStageCompatibility(PluginManifestStep step, PluginManifestImage image, List<string> errors)
    {
        // A post-image can only be produced once the main operation has completed. Pre-images are valid in
        // every stage, PreValidation included.
        if (image.IsPostImage && !step.IsMainOperation && step.Stage != PostOperationStage)
        {
            errors.Add($"Step '{step.StepId}' defines {image.Type} image '{image.Name}' in the {step.StageName} stage. Post-images are available only in the PostOperation stage.");
        }
    }

    private static void ValidateImageMessageCompatibility(PluginManifestStep step, PluginManifestImage image, List<string> errors)
    {
        if (IsCreate(step) && image.IsPreImage)
        {
            errors.Add($"Create step '{step.StepId}' cannot define {image.Type} image '{image.Name}'. The record does not exist before the operation, so use a post-image for Create steps.");
        }

        if (IsDelete(step) && image.IsPostImage)
        {
            errors.Add($"Delete step '{step.StepId}' cannot define {image.Type} image '{image.Name}'. The record no longer exists after the operation, so use a pre-image for Delete steps.");
        }
    }

    private static bool IsCreate(PluginManifestStep step) => string.Equals(step.MessageName, "Create", StringComparison.OrdinalIgnoreCase);

    private static bool IsUpdate(PluginManifestStep step) => string.Equals(step.MessageName, "Update", StringComparison.OrdinalIgnoreCase);

    private static bool IsDelete(PluginManifestStep step) => string.Equals(step.MessageName, "Delete", StringComparison.OrdinalIgnoreCase);

    private static bool IsPlaceholderGuid(Guid id)
    {
        return id.ToString("D").StartsWith("00000000-0000-0000-0000-", StringComparison.OrdinalIgnoreCase);
    }
}
