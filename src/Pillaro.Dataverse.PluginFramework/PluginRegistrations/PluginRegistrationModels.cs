using Pillaro.Dataverse.PluginFramework.Plugins;

namespace Pillaro.Dataverse.PluginFramework.PluginRegistrations;

public enum PluginImageType
{
    PreImage = 0,
    PostImage = 1,

    /// <summary>
    /// Dataverse image type <c>Both</c>. A single image registration that the platform exposes through
    /// both <c>PreEntityImages</c> and <c>PostEntityImages</c>.
    /// </summary>
    Both = 2,
}

public enum PluginRisk
{
    Low,
    Medium,
    High,
    Critical,
}

public enum PluginDeploymentScope
{
    All,
    Development,
    Test,
    Production,
    TestAndProduction,
}

public sealed record PluginRegistrationDescriptor(
    Type PluginType,
    IReadOnlyCollection<PluginStepRegistrationDescriptor> Steps);

public sealed record PluginStepRegistrationDescriptor(
    Guid StepId,
    Type PluginType,
    string MessageName,
    string EntityName,
    PluginStage Stage,
    PluginMode Mode,
    int Rank,
    string Name,
    IReadOnlyCollection<string> FilteringAttributes,
    IReadOnlyCollection<PluginImageRegistrationDescriptor> Images,
    PluginDeploymentPolicyDescriptor DeploymentPolicy,
    string UnsecureConfiguration);

public sealed record PluginImageRegistrationDescriptor(
    Guid ImageId,
    PluginImageType Type,
    string Name,
    IReadOnlyCollection<string> Attributes)
{
    /// <summary>
    /// Key used to read the image from <c>PreEntityImages</c>/<c>PostEntityImages</c>. When null, the
    /// image name is used, which keeps the behaviour of registrations that never set an alias.
    /// </summary>
    public string EntityAlias { get; init; }

    /// <summary>
    /// Request property the image is taken from (<c>sdkmessageprocessingstepimage.messagepropertyname</c>).
    /// When null, it is derived from the message; set it explicitly for messages that expose the record
    /// under more than one property, such as <c>Merge</c> (<c>Target</c> or <c>SubordinateId</c>).
    /// </summary>
    public string MessagePropertyName { get; init; }
}

/// <summary>
/// Full image registration input, for the combinations that the WithPreImage/WithPostImage/WithBothImage
/// shorthands cannot express (a distinct entity alias, or an explicit message property name).
/// </summary>
public sealed record PluginImageOptions(
    string ImageId,
    PluginImageType Type,
    string Name)
{
    public string EntityAlias { get; init; }

    public string MessagePropertyName { get; init; }

    public IReadOnlyCollection<string> Attributes { get; init; } = [];
}

public sealed record PluginDeploymentPolicyDescriptor(
    PluginRisk Risk,
    string Reason,
    PluginDeploymentScope Scope);
