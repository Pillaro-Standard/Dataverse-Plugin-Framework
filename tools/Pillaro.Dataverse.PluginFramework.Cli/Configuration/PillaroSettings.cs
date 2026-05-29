using System.Text.Json.Serialization;

namespace Pillaro.Dataverse.PluginFramework.Cli.Configuration;

internal sealed class PillaroSettings
{
    public int SchemaVersion { get; set; } = 1;

    public string DefaultProfile { get; set; } = string.Empty;

    public string Solution { get; set; } = string.Empty;

    public PillaroDataverseSettings Dataverse { get; set; } = new();

    public Dictionary<string, PillaroSettingsProfile> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // Legacy: top-level plugins.assembly from older config shape. Used only to emit a helpful migration error.
    public PillaroLegacyPluginSettings? Plugins { get; set; }
}

internal sealed class PillaroDataverseSettings
{
    public string ConnectionStringEnvironmentVariable { get; set; } = "DV_CONN";
}

internal sealed class PillaroSettingsProfile
{
    public string PluginAssemblyPath { get; set; } = string.Empty;
}

// Retained for non-deploy commands that resolve connection via ~/.pillaro/dataverse-profiles.json.
internal sealed class DataverseProfilesDocument
{
    public string DefaultProfile { get; set; } = string.Empty;

    public Dictionary<string, DataverseProfile> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class DataverseProfile
{
    public string ConnectionString { get; set; } = string.Empty;

    public string PacProfile { get; set; } = string.Empty;
}

internal sealed class PillaroLegacyPluginSettings
{
    public string? Assembly { get; set; }
}

[JsonSerializable(typeof(PillaroSettings))]
[JsonSerializable(typeof(DataverseProfilesDocument))]
[JsonSerializable(typeof(PillaroSettingsProfile))]
[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true)]
internal sealed partial class PillaroSettingsJsonContext : JsonSerializerContext;
