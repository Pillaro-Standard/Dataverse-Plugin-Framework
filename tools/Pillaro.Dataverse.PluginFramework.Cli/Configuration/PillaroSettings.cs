namespace Pillaro.Dataverse.PluginFramework.Cli.Configuration;

internal sealed class PillaroSettings
{
    public int SchemaVersion { get; set; } = 1;

    public string DefaultProfile { get; set; } = string.Empty;

    public string Solution { get; set; } = string.Empty;

    public PillaroDataverseSettings Dataverse { get; set; } = new();

    public Dictionary<string, PillaroSettingsProfile> Profiles { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class PillaroDataverseSettings
{
    public string ConnectionStringEnvironmentVariable { get; set; } = "DV_CONN";
}

internal sealed class PillaroSettingsProfile
{
    public string PluginAssemblyPath { get; set; } = string.Empty;
}
