namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.RegistrationState;

internal sealed class DataverseRegistrationState
{
    public Dictionary<string, Guid> PluginTypeIdsByName { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<Guid, DataverseStepState> StepsById { get; } = [];

    public Dictionary<Guid, DataverseImageState> ImagesById { get; } = [];
}

internal sealed class DataverseStepState
{
    public Guid StepId { get; init; }

    public Guid PluginTypeId { get; init; }

    public string PluginTypeName { get; init; } = string.Empty;

    public string? Name { get; init; }

    public string MessageName { get; init; } = string.Empty;

    public string? EntityName { get; init; }

    public int Stage { get; init; }

    public int Mode { get; init; }

    public int Rank { get; init; }

    public IReadOnlyCollection<string> FilteringAttributes { get; init; } = [];

    public string? UnsecureConfiguration { get; init; }
}

internal sealed class DataverseImageState
{
    public Guid ImageId { get; init; }

    public Guid StepId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Type { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Attributes { get; init; } = [];
}
