namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.RegistrationState;

internal static class PluginRegistrationDiffCalculator
{
    public static PluginRegistrationDiff Calculate(PluginManifestDocument manifest, DataverseRegistrationState currentState)
    {
        var diff = new PluginRegistrationDiff();
        var desiredStepIds = manifest.Plugins.SelectMany(plugin => plugin.Steps).Select(step => step.StepId).ToHashSet();
        var desiredImageIds = manifest.Plugins.SelectMany(plugin => plugin.Steps).SelectMany(step => step.Images).Select(image => image.ImageId).ToHashSet();
        var managedPluginTypes = manifest.Plugins.Select(plugin => plugin.TypeName).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var plugin in manifest.Plugins)
        {
            foreach (var step in plugin.Steps)
            {
                var stepDiff = CalculateStepDiff(plugin, step, currentState);
                diff.StepChanges.Add(stepDiff);

                foreach (var image in step.Images)
                {
                    diff.ImageChanges.Add(CalculateImageDiff(step, image, currentState));
                }
            }
        }

        foreach (var currentStep in currentState.StepsById.Values.Where(step => managedPluginTypes.Contains(step.PluginTypeName) && !desiredStepIds.Contains(step.StepId)))
        {
            var deleteStep = new PluginStepDiff
            {
                Action = PluginDiffAction.Delete,
                StepId = currentStep.StepId,
                Name = currentStep.Name,
                PluginTypeName = currentStep.PluginTypeName,
                MessageName = currentStep.MessageName,
                EntityName = currentStep.EntityName,
                StageName = currentStep.Stage.ToString(),
                ModeName = currentStep.Mode.ToString(),
            };
            diff.StepChanges.Add(deleteStep);
        }

        foreach (var currentImage in currentState.ImagesById.Values.Where(image => IsManagedImage(image, currentState, managedPluginTypes) && !desiredImageIds.Contains(image.ImageId)))
        {
            var deleteImage = new PluginImageDiff
            {
                Action = PluginDiffAction.Delete,
                ImageId = currentImage.ImageId,
                StepId = currentImage.StepId,
                Name = currentImage.Name,
                Type = currentImage.Type,
            };
            diff.ImageChanges.Add(deleteImage);
        }

        return diff;
    }

    private static PluginStepDiff CalculateStepDiff(
        PluginManifestPlugin plugin,
        PluginManifestStep desired,
        DataverseRegistrationState currentState)
    {
        if (!currentState.StepsById.TryGetValue(desired.StepId, out var current))
        {
            return new PluginStepDiff
            {
                Action = PluginDiffAction.Create,
                StepId = desired.StepId,
                Name = desired.Name,
                PluginTypeName = plugin.TypeName,
                MessageName = desired.MessageName,
                EntityName = desired.EntityName,
                StageName = desired.StageName,
                ModeName = desired.ModeName,
                UnsecureConfigurationDiff = BuildFieldDiff(null, desired.UnsecureConfiguration),
            };
        }

        var unsecureDiff = BuildFieldDiff(current.UnsecureConfiguration, desired.UnsecureConfiguration);

        var result = new PluginStepDiff
        {
            Action = PluginDiffAction.Unchanged,
            StepId = desired.StepId,
            Name = desired.Name,
            PluginTypeName = plugin.TypeName,
            MessageName = desired.MessageName,
            EntityName = desired.EntityName,
            StageName = desired.StageName,
            ModeName = desired.ModeName,
            UnsecureConfigurationDiff = unsecureDiff,
        };

        AddDifference(result.Reasons, "PluginType", current.PluginTypeName, plugin.TypeName);
        AddDifference(result.Reasons, "Message", current.MessageName, desired.MessageName);
        AddDifference(result.Reasons, "Entity", current.EntityName, desired.EntityName);
        AddDifference(result.Reasons, "Stage", current.Stage.ToString(), desired.Stage.ToString());
        AddDifference(result.Reasons, "Mode", current.Mode.ToString(), desired.Mode.ToString());
        AddDifference(result.Reasons, "Rank", current.Rank.ToString(), desired.Rank.ToString());
        AddNameDifference(result.Reasons, "Name", current.Name, desired.Name);
        AddCollectionDifference(result.Reasons, "FilteringAttributes", current.FilteringAttributes, desired.FilteringAttributes);

        if (unsecureDiff.Action != PluginDiffAction.Unchanged)
        {
            result.Reasons.Add($"UnsecureConfiguration changed.");
        }

        if (result.Reasons.Count > 0)
        {
            result.Action = PluginDiffAction.Update;
        }

        return result;
    }

    private static PluginImageDiff CalculateImageDiff(
        PluginManifestStep step,
        PluginManifestImage desired,
        DataverseRegistrationState currentState)
    {
        var result = new PluginImageDiff
        {
            Action = PluginDiffAction.Unchanged,
            ImageId = desired.ImageId,
            StepId = step.StepId,
            Name = desired.Name,
            Type = desired.Type,
        };

        if (!currentState.ImagesById.TryGetValue(desired.ImageId, out var current))
        {
            result.Action = PluginDiffAction.Create;
            return result;
        }

        var hasChanges = !string.Equals(current.StepId.ToString(), step.StepId.ToString(), StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Normalize(current.Name), Normalize(desired.Name), StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Normalize(current.Type), Normalize(desired.Type), StringComparison.OrdinalIgnoreCase)
            || !NormalizeCollection(current.Attributes).SequenceEqual(NormalizeCollection(desired.Attributes), StringComparer.OrdinalIgnoreCase);

        if (hasChanges)
        {
            result.Action = PluginDiffAction.Update;
        }

        return result;
    }

    private static bool IsManagedImage(DataverseImageState image, DataverseRegistrationState currentState, HashSet<string> managedPluginTypes)
    {
        return currentState.StepsById.TryGetValue(image.StepId, out var step)
            && managedPluginTypes.Contains(step.PluginTypeName);
    }

    private static PluginFieldDiff BuildFieldDiff(string? currentValue, string? desiredValue)
    {
        var currentNorm = Normalize(currentValue);
        var desiredNorm = Normalize(desiredValue);

        if (string.IsNullOrEmpty(currentNorm) && string.IsNullOrEmpty(desiredNorm))
        {
            return new PluginFieldDiff { Action = PluginDiffAction.Unchanged, DisplayValue = "(not set)" };
        }

        if (string.IsNullOrEmpty(currentNorm))
        {
            return new PluginFieldDiff { Action = PluginDiffAction.Create, DisplayValue = desiredValue! };
        }

        if (string.IsNullOrEmpty(desiredNorm))
        {
            return new PluginFieldDiff { Action = PluginDiffAction.Delete, DisplayValue = currentValue! };
        }

        if (!string.Equals(currentNorm, desiredNorm, StringComparison.OrdinalIgnoreCase))
        {
            return new PluginFieldDiff { Action = PluginDiffAction.Update, DisplayValue = $"'{currentValue}' -> '{desiredValue}'" };
        }

        return new PluginFieldDiff { Action = PluginDiffAction.Unchanged, DisplayValue = currentValue! };
    }

    private static void AddDifference(List<string> reasons, string fieldName, string? currentValue, string? desiredValue)
    {
        if (!string.Equals(Normalize(currentValue), Normalize(desiredValue), StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add($"{fieldName}: '{currentValue ?? "<null>"}' -> '{desiredValue ?? "<null>"}'.");
        }
    }

    private static void AddNameDifference(
        List<string> reasons,
        string fieldName,
        string? currentName,
        string? desiredName)
    {
        // If WithName() was not explicitly set, do not manage the name - leave it as-is in Dataverse.
        if (string.IsNullOrWhiteSpace(desiredName))
            return;

        if (!string.Equals(Normalize(currentName), Normalize(desiredName), StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add($"{fieldName}: '{currentName ?? "<null>"}' -> '{desiredName}'.");
        }
    }

    private static void AddCollectionDifference(
        List<string> reasons,
        string fieldName,
        IReadOnlyCollection<string> currentValues,
        IReadOnlyCollection<string> desiredValues)
    {
        var current = NormalizeCollection(currentValues);
        var desired = NormalizeCollection(desiredValues);

        if (!current.SequenceEqual(desired, StringComparer.OrdinalIgnoreCase))
        {
            reasons.Add($"{fieldName}: '{string.Join(",", current)}' -> '{string.Join(",", desired)}'.");
        }
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;

    private static IReadOnlyCollection<string> NormalizeCollection(IReadOnlyCollection<string> values)
    {
        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
