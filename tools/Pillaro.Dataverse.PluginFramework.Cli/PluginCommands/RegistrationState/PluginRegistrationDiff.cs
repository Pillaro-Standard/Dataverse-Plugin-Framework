namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.RegistrationState;

internal sealed class PluginRegistrationDiff
{
    public List<PluginStepDiff> StepChanges { get; } = [];

    public List<PluginImageDiff> ImageChanges { get; } = [];

    public bool HasChanges => StepChanges.Any(change => change.Action != PluginDiffAction.Unchanged)
        || ImageChanges.Any(change => change.Action != PluginDiffAction.Unchanged);
}

internal sealed class PluginFieldDiff
{
    public PluginDiffAction Action { get; init; }
    public string DisplayValue { get; init; } = string.Empty;
}

internal sealed class PluginStepDiff
{
    public PluginDiffAction Action { get; set; }

    public Guid StepId { get; init; }

    public string? Name { get; init; }

    public string PluginTypeName { get; init; } = string.Empty;

    public string MessageName { get; init; } = string.Empty;

    public string? EntityName { get; init; }

    public string StageName { get; init; } = string.Empty;

    public string ModeName { get; init; } = string.Empty;

    public PluginFieldDiff? UnsecureConfigurationDiff { get; init; }

    public List<string> Reasons { get; } = [];
}

internal sealed class PluginImageDiff
{
    public PluginDiffAction Action { get; set; }

    public Guid ImageId { get; init; }

    public Guid StepId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;
}

internal enum PluginDiffAction
{
    Create,
    Update,
    Delete,
    Unchanged,
}
