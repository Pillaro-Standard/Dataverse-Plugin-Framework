using System.Reflection;

namespace Pillaro.Dataverse.PluginFramework.Cli.AiInstructions;

/// <summary>
/// Reads the AI instruction sources embedded in the CLI assembly. Each resource is named after the
/// file's path in the framework repository, prefixed with <see cref="ResourcePrefix"/>.
/// </summary>
internal static class AiInstructionSource
{
    public const string ResourcePrefix = "ai-source/";

    public static IReadOnlyDictionary<string, string> Load()
    {
        var assembly = typeof(AiInstructionSource).Assembly;
        var files = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var name in assembly.GetManifestResourceNames())
        {
            if (!name.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException($"Embedded resource '{name}' could not be opened.");
            using var reader = new StreamReader(stream);

            var sourcePath = name[ResourcePrefix.Length..].Replace('\\', '/');
            files[sourcePath] = AiText.Normalize(reader.ReadToEnd());
        }

        return files;
    }

    public static string Version()
    {
        var version = typeof(AiInstructionSource).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

        var metadata = version.IndexOf('+', StringComparison.Ordinal);
        return metadata >= 0 ? version[..metadata] : version;
    }
}
