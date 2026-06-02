namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.RegistrationState;

internal static class PluginRegistrationDiffWriter
{
    public static void Write(PluginRegistrationDiff diff, PluginManifestDocument manifest)
    {
        var stepsByPlugin = new Dictionary<string, List<(PluginManifestStep? Step, PluginStepDiff Diff)>>();

        foreach (var plugin in manifest.Plugins)
        {
            var pluginSteps = new List<(PluginManifestStep? Step, PluginStepDiff Diff)>();

            foreach (var step in plugin.Steps)
            {
                var stepDiff = diff.StepChanges.First(d => d.StepId == step.StepId);
                pluginSteps.Add((step, stepDiff));
            }

            stepsByPlugin[plugin.TypeName] = pluginSteps;
        }

        var deletedSteps = diff.StepChanges.Where(d => d.Action == PluginDiffAction.Delete).ToList();
        foreach (var deletedStep in deletedSteps)
        {
            if (!stepsByPlugin.ContainsKey(deletedStep.PluginTypeName))
            {
                stepsByPlugin[deletedStep.PluginTypeName] = [];
            }
            stepsByPlugin[deletedStep.PluginTypeName].Add((null, deletedStep));
        }

        foreach (var pluginTypeName in stepsByPlugin.Keys.OrderBy(k => k))
        {
            var pluginDisplayName = GetPluginDisplayName(pluginTypeName);
            Console.WriteLine();
            Console.WriteLine(pluginDisplayName);

            var steps = stepsByPlugin[pluginTypeName]
                .OrderBy(s => s.Diff.Action == PluginDiffAction.Delete ? 1 : 0)
                .ThenBy(s => s.Diff.MessageName)
                .ThenBy(s => s.Diff.StageName);

            foreach (var (step, stepDiff) in steps)
            {
                var stepName = step != null && !string.IsNullOrWhiteSpace(step.Name)
                    ? step.Name
                    : (!string.IsNullOrWhiteSpace(stepDiff.Name)
                        ? stepDiff.Name
                        : $"{GetStagePrefix(stepDiff.StageName)}{stepDiff.MessageName}");

                var status = GetStatusLabel(stepDiff.Action);

                Console.WriteLine($"  [{status}] {stepName}");

                if (stepDiff.UnsecureConfigurationDiff != null
                    && !(stepDiff.UnsecureConfigurationDiff.Action == PluginDiffAction.Unchanged
                         && stepDiff.UnsecureConfigurationDiff.DisplayValue == "(not set)"))
                {
                    WriteFieldDiff("UnsecureConfig", stepDiff.UnsecureConfigurationDiff);
                }

                foreach (var reason in stepDiff.Reasons.Where(r => !r.StartsWith("UnsecureConfiguration changed", StringComparison.Ordinal)))
                {
                    Console.WriteLine($"       [CHANGE] {reason}");
                }

                var stepId = step?.StepId ?? stepDiff.StepId;
                var allImagesForStep = diff.ImageChanges
                    .Where(ic => ic.StepId == stepId)
                    .OrderBy(ic => ic.Action == PluginDiffAction.Delete ? 1 : 0)
                    .ThenBy(ic => ic.Type)
                    .ThenBy(ic => ic.Name);

                foreach (var imageDiff in allImagesForStep)
                {
                    var imageStatus = GetStatusLabel(imageDiff.Action);
                    Console.WriteLine($"       [{imageStatus}] {imageDiff.Type,-9}: {imageDiff.Name}");
                }
            }
        }

        foreach (var skipped in manifest.PluginTypesWithoutRegistration)
        {
            Console.WriteLine();
            Console.WriteLine($"[WARN] {GetPluginDisplayName(skipped)} - no steps registered via Register(IPluginRegistration), skipped from deployment.");
        }
    }

    private static void WriteFieldDiff(string label, PluginFieldDiff? diff)
    {
        if (diff == null)
            return;

        var status = GetStatusLabel(diff.Action);
        Console.WriteLine($"       [{status}] {label}: {diff.DisplayValue}");
    }

    private static string GetPluginDisplayName(string fullTypeName)
    {
        var parts = fullTypeName.Split('.');
        var className = parts[^1];
        return className.EndsWith("Plugin", StringComparison.OrdinalIgnoreCase)
            ? className.Substring(0, className.Length - 6)
            : className;
    }

    private static string GetStagePrefix(string stageName)
    {
        return stageName switch
        {
            "Prevalidation" => "PreValidation ",
            "Preoperation" => "PreOperation  ",
            "Postoperation" => "PostOperation ",
            _ => string.Empty
        };
    }

    private static string GetStatusLabel(PluginDiffAction action)
    {
        return action switch
        {
            PluginDiffAction.Create => "CREATE",
            PluginDiffAction.Update => "UPDATE",
            PluginDiffAction.Delete => "DELETE",
            PluginDiffAction.Unchanged => "OK",
            _ => action.ToString().ToUpperInvariant(),
        };
    }
}
