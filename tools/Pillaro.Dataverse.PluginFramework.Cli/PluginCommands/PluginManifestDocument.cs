using System.Text.Json.Serialization;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal sealed class PluginManifestDocument
{
    public string SchemaVersion { get; set; } = "1.0";

    public string? AssemblyPath { get; set; }

    public string? AssemblyName { get; set; }

    public DateTime GeneratedUtc { get; set; } = DateTime.UtcNow;

    public List<string> PluginTypesWithoutRegistration { get; set; } = [];

    public List<PluginManifestPlugin> Plugins { get; set; } = [];
}

internal sealed class PluginManifestPlugin
{
    public string TypeName { get; set; } = string.Empty;

    public List<PluginManifestStep> Steps { get; set; } = [];
}

internal sealed class PluginManifestStep
{
    internal const int MainOperationStage = 30;

    public Guid StepId { get; set; }

    public string MessageName { get; set; } = string.Empty;

    public string? EntityName { get; set; }

    public int Stage { get; set; }

    public string StageName { get; set; } = string.Empty;

    public int Mode { get; set; }

    public string ModeName { get; set; } = string.Empty;

    public int Rank { get; set; }

    public string? Name { get; set; }

    public List<string> FilteringAttributes { get; set; } = [];

    public string? UnsecureConfiguration { get; set; }

    public List<PluginManifestImage> Images { get; set; } = [];

    public PluginManifestDeploymentPolicy? DeploymentPolicy { get; set; }

    // Custom API MainOperation handlers are associated through CustomAPI.PluginTypeId;
    // only the assembly and plugin type are deployed, never an SdkMessageProcessingStep.
    [JsonIgnore]
    public bool IsMainOperation => Stage == MainOperationStage;
}

internal sealed class PluginManifestImage
{
    public Guid ImageId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    /// <summary>Key into PreEntityImages/PostEntityImages. Null falls back to <see cref="Name"/>.</summary>
    public string? EntityAlias { get; set; }

    /// <summary>Explicit sdkmessageprocessingstepimage.messagepropertyname. Null is derived from the message.</summary>
    public string? MessagePropertyName { get; set; }

    public List<string> Attributes { get; set; } = [];

    [JsonIgnore]
    public string ResolvedEntityAlias => string.IsNullOrWhiteSpace(EntityAlias) ? Name : EntityAlias!;

    [JsonIgnore]
    public bool IsPreImage => string.Equals(Type, "PreImage", StringComparison.OrdinalIgnoreCase) || IsBothImage;

    [JsonIgnore]
    public bool IsPostImage => string.Equals(Type, "PostImage", StringComparison.OrdinalIgnoreCase) || IsBothImage;

    [JsonIgnore]
    public bool IsBothImage => string.Equals(Type, "Both", StringComparison.OrdinalIgnoreCase);
}

internal sealed class PluginManifestDeploymentPolicy
{
    public string Risk { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;
}

[JsonSerializable(typeof(PluginManifestDocument))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class PluginManifestJsonContext : JsonSerializerContext;
