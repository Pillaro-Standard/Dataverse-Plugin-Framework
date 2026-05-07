namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.RegistrationState;

internal sealed class PluginRegistrationDiff
{
    public List<PluginStepDiff> StepChanges { get; } = [];

    public List<PluginImageDiff> ImageChanges { get; } = [];

    public bool HasChanges => StepChanges.Count > 0 || ImageChanges.Count > 0;
}

internal sealed class PluginStepDiff
{
    public PluginDiffAction Action { get; init; }

    public Guid StepId { get; init; }

    public string PluginTypeName { get; init; } = string.Empty;

    public string MessageName { get; init; } = string.Empty;

    public string? EntityName { get; init; }

    public string StageName { get; init; } = string.Empty;

    public string ModeName { get; init; } = string.Empty;

    public List<string> Reasons { get; } = [];
}

internal sealed class PluginImageDiff
{
    public PluginDiffAction Action { get; init; }

    public Guid ImageId { get; init; }

    public Guid StepId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public List<string> Reasons { get; } = [];
}

internal enum PluginDiffAction
{
    Create,
    Update,
    Unchanged,
}
