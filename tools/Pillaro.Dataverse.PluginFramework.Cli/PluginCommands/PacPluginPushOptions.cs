using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal sealed class PacPluginPushOptions
{
    public string? PluginId { get; private init; }

    public string PluginType { get; private init; } = "Assembly";

    public bool SkipPacPush { get; private init; } = true;

    public static PacPluginPushOptions From(CommandLineOptions options)
    {
        return new PacPluginPushOptions
        {
            PluginId = options.Get("plugin-id"),
            PluginType = options.Get("plugin-type") ?? "Assembly",
            SkipPacPush = !options.HasFlag("pac-push"),
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
            errors.Add("Missing --plugin-id. PAC plugin push requires an existing plugin assembly/package id. Default deployment skips PAC push and deploys step/image metadata only.");
        }
        else if (!Guid.TryParse(PluginId, out var pluginId) || pluginId == Guid.Empty)
        {
            errors.Add("--plugin-id must be a non-empty GUID.");
        }

        if (!string.Equals(PluginType, "Assembly", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(PluginType, "Package", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("--plugin-type must be either Assembly or Package.");
        }

        return errors;
    }
}
