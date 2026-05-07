using Pillaro.Dataverse.PluginFramework.Plugins;

namespace Pillaro.Dataverse.PluginFramework.PluginRegistrations;

public enum PluginImageType
{
    PreImage,
    PostImage,
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
    string? EntityName,
    PluginStage Stage,
    PluginMode Mode,
    int Rank,
    string SolutionName,
    IReadOnlyCollection<string> FilteringAttributes,
    IReadOnlyCollection<PluginImageRegistrationDescriptor> Images,
    PluginDeploymentPolicyDescriptor? DeploymentPolicy);

public sealed record PluginImageRegistrationDescriptor(
    Guid ImageId,
    PluginImageType Type,
    string Name,
    IReadOnlyCollection<string> Attributes);

public sealed record PluginDeploymentPolicyDescriptor(
    bool RequiresConfirmation,
    PluginRisk Risk,
    string Reason,
    PluginDeploymentScope Scope);
