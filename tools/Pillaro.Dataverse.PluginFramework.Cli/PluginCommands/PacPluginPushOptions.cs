using Pillaro.Dataverse.PluginFramework.Cli.Configuration;
using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal sealed class PacPluginPushOptions
{
    public string? PluginId { get; private init; }

    public string PluginType { get; private init; } = "Assembly";

    public bool SkipPacPush { get; private init; }

    public static PacPluginPushOptions From(CommandLineOptions options)
    {
        return From(options, settings: null);
    }

    public static PacPluginPushOptions From(CommandLineOptions options, PillaroSettings? settings)
    {
        return new PacPluginPushOptions
        {
            PluginId = options.Get("plugin-id") ?? settings?.Plugins.PluginId,
            PluginType = options.Get("plugin-type") ?? settings?.Plugins.PluginType ?? "Assembly",
            SkipPacPush = options.HasFlag("skip-pac-push"),
        };
    }

    public IReadOnlyCollection<string> ValidateForPacPush()
    {
        var errors = new List<string>();

        if (SkipPacPush)
        {
            return errors;
        }

        if (string.IsNullOrWhiteSpace(PluginId))
        {
            errors.Add("Missing pluginId. Set plugins.pluginId in PillaroSettings.json or pass --plugin-id. Use --skip-pac-push to skip PAC upload.");
        }
        else if (!Guid.TryParse(PluginId, out var pluginId) || pluginId == Guid.Empty)
        {
            errors.Add("Plugin ID must be a non-empty GUID.");
        }

        if (!string.Equals(PluginType, "Assembly", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(PluginType, "Package", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Plugin type must be either Assembly or Package.");
        }

        return errors;
    }
}
