namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.RegistrationState;

internal static class PluginRegistrationDiffCalculator
{
    public static PluginRegistrationDiff Calculate(PluginManifestDocument manifest, DataverseRegistrationState currentState)
    {
        var diff = new PluginRegistrationDiff();

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

        return diff;
    }

    private static PluginStepDiff CalculateStepDiff(
        PluginManifestPlugin plugin,
        PluginManifestStep desired,
        DataverseRegistrationState currentState)
    {
        var result = new PluginStepDiff
        {
            Action = PluginDiffAction.Unchanged,
            StepId = desired.StepId,
            PluginTypeName = plugin.TypeName,
            MessageName = desired.MessageName,
            EntityName = desired.EntityName,
            StageName = desired.StageName,
            ModeName = desired.ModeName,
        };

        if (!currentState.StepsById.TryGetValue(desired.StepId, out var current))
        {
            result.Action = PluginDiffAction.Create;
            result.Reasons.Add("Step does not exist in Dataverse.");
            return result;
        }

        AddDifference(result.Reasons, "Message", current.MessageName, desired.MessageName);
        AddDifference(result.Reasons, "Entity", current.EntityName, desired.EntityName);
        AddDifference(result.Reasons, "Stage", current.Stage.ToString(), desired.Stage.ToString());
        AddDifference(result.Reasons, "Mode", current.Mode.ToString(), desired.Mode.ToString());
        AddDifference(result.Reasons, "Rank", current.Rank.ToString(), desired.Rank.ToString());
        AddCollectionDifference(result.Reasons, "FilteringAttributes", current.FilteringAttributes, desired.FilteringAttributes);

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
            result.Reasons.Add("Image does not exist in Dataverse.");
            return result;
        }

        AddDifference(result.Reasons, "StepId", current.StepId.ToString(), step.StepId.ToString());
        AddDifference(result.Reasons, "Name", current.Name, desired.Name);
        AddDifference(result.Reasons, "Type", current.Type, desired.Type);
        AddCollectionDifference(result.Reasons, "Attributes", current.Attributes, desired.Attributes);

        if (result.Reasons.Count > 0)
        {
            result.Action = PluginDiffAction.Update;
        }

        return result;
    }

    private static void AddDifference(List<string> reasons, string fieldName, string? currentValue, string? desiredValue)
    {
        if (!string.Equals(Normalize(currentValue), Normalize(desiredValue), StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add($"{fieldName}: '{currentValue ?? "<null>"}' -> '{desiredValue ?? "<null>"}'.");
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
