using System.Text.Json;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal static class PluginManifestSerializer
{
    public static async Task SaveAsync(PluginManifestDocument manifest, string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, manifest, PluginManifestJsonContext.Default.PluginManifestDocument);
    }

    public static async Task<PluginManifestDocument> LoadAsync(string path)
    {
        await using var stream = File.OpenRead(path);
        var manifest = await JsonSerializer.DeserializeAsync(stream, PluginManifestJsonContext.Default.PluginManifestDocument);
        return manifest ?? throw new InvalidOperationException($"Manifest '{path}' is empty or invalid.");
    }
}
