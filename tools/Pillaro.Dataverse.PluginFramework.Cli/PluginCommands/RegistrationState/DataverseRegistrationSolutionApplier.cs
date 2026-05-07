using Microsoft.Xrm.Sdk;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.RegistrationState;

internal static class DataverseRegistrationSolutionApplier
{
    public static async Task ApplyAsync(
        IOrganizationServiceAsync2 service,
        PluginManifestDocument manifest,
        PluginRegistrationDiff diff)
    {
        var solutionNames = manifest.Plugins
            .SelectMany(plugin => plugin.Steps)
            .Select(step => step.SolutionName)
            .Where(solution => !string.IsNullOrWhiteSpace(solution))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (solutionNames.Length == 0)
        {
            throw new InvalidOperationException("Every plugin step must define SolutionName through InSolution(...).");
        }

        foreach (var solutionName in solutionNames)
        {
            var scopedManifest = CreateScopedManifest(manifest, solutionName);
            var scopedDiff = PluginRegistrationDiffCalculator.Calculate(scopedManifest, CreateScopedStateFromDiff(diff, scopedManifest));
            await DataverseRegistrationUpserter.ApplyAsync(service, scopedManifest, scopedDiff, solutionName);
        }
    }

    private static PluginManifestDocument CreateScopedManifest(PluginManifestDocument manifest, string solutionName)
    {
        return new PluginManifestDocument
        {
            SchemaVersion = manifest.SchemaVersion,
            AssemblyPath = manifest.AssemblyPath,
            AssemblyName = manifest.AssemblyName,
            GeneratedUtc = manifest.GeneratedUtc,
            Plugins = manifest.Plugins
                .Select(plugin => new PluginManifestPlugin
                {
                    TypeName = plugin.TypeName,
                    Steps = plugin.Steps
                        .Where(step => string.Equals(step.SolutionName, solutionName, StringComparison.OrdinalIgnoreCase))
                        .ToList()
                })
                .Where(plugin => plugin.Steps.Count > 0)
                .ToList()
        };
    }

    private static DataverseRegistrationState CreateScopedStateFromDiff(PluginRegistrationDiff diff, PluginManifestDocument scopedManifest)
    {
        // The existing upserter expects a diff calculated for the scoped manifest.
        // Until the upserter accepts solution per step directly, this lightweight state treats
        // all scoped desired records as missing and relies on the current upsert behavior.
        return new DataverseRegistrationState();
    }
}
