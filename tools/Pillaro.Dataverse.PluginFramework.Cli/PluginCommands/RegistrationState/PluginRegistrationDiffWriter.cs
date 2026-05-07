namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.RegistrationState;

internal static class PluginRegistrationDiffWriter
{
    public static void Write(PluginRegistrationDiff diff, bool includeUnchanged = false)
    {
        Console.WriteLine("Registration diff:");
        Console.WriteLine();

        WriteStepChanges(diff, includeUnchanged);
        Console.WriteLine();
        WriteImageChanges(diff, includeUnchanged);
    }

    private static void WriteStepChanges(PluginRegistrationDiff diff, bool includeUnchanged)
    {
        Console.WriteLine("Steps:");

        var changes = diff.StepChanges
            .Where(change => includeUnchanged || change.Action != PluginDiffAction.Unchanged)
            .OrderBy(change => change.PluginTypeName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(change => change.Action)
            .ThenBy(change => change.MessageName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(change => change.EntityName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (changes.Count == 0)
        {
            Console.WriteLine("  No step changes.");
            return;
        }

        foreach (var change in changes)
        {
            Console.WriteLine($"  {GetActionLabel(change.Action),-6} {change.StepId} {change.PluginTypeName} {change.MessageName} {change.EntityName ?? "<none>"} {change.StageName} {change.ModeName}");
            foreach (var reason in change.Reasons)
            {
                Console.WriteLine($"         - {reason}");
            }
        }
    }

    private static void WriteImageChanges(PluginRegistrationDiff diff, bool includeUnchanged)
    {
        Console.WriteLine("Images:");

        var changes = diff.ImageChanges
            .Where(change => includeUnchanged || change.Action != PluginDiffAction.Unchanged)
            .OrderBy(change => change.StepId)
            .ThenBy(change => change.Action)
            .ThenBy(change => change.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (changes.Count == 0)
        {
            Console.WriteLine("  No image changes.");
            return;
        }

        foreach (var change in changes)
        {
            Console.WriteLine($"  {GetActionLabel(change.Action),-6} {change.ImageId} {change.Type} '{change.Name}' on step {change.StepId}");
            foreach (var reason in change.Reasons)
            {
                Console.WriteLine($"         - {reason}");
            }
        }
    }

    private static string GetActionLabel(PluginDiffAction action)
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
