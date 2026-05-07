using System.Text.Json.Serialization;

namespace Pillaro.Dataverse.PluginFramework.Cli.Configuration;

internal sealed class PillaroSettings
{
    public string Solution { get; set; } = string.Empty;

    public PillaroPluginSettings Plugins { get; set; } = new();

    public PillaroEarlyBoundSettings EarlyBound { get; set; } = new();
}

internal sealed class PillaroPluginSettings
{
    public string Assembly { get; set; } = string.Empty;

    public string Manifest { get; set; } = "artifacts/plugin-manifest.json";

    public string PluginId { get; set; } = string.Empty;

    public string PluginType { get; set; } = "Assembly";
}

internal sealed class PillaroEarlyBoundSettings
{
    public string Out { get; set; } = "src/Dataverse/Model";

    public string Namespace { get; set; } = string.Empty;

    public string ServiceContextName { get; set; } = "DataverseContext";

    public string Language { get; set; } = "en-US";

    public bool IncludeSolutionEntities { get; set; } = true;

    public List<string> Entities { get; set; } = [];

    public bool GenerateSdkMessages { get; set; } = true;

    public List<string> Messages { get; set; } = [];

    public bool EmitFieldsClasses { get; set; } = true;

    public bool EmitVirtualAttributes { get; set; } = true;

    public bool SuppressINotifyPattern { get; set; } = true;

    public bool SuppressGeneratedCodeAttribute { get; set; }

    public string EntityTypesFolder { get; set; } = "Entities";

    public string MessagesTypesFolder { get; set; } = "Messages";

    public string OptionSetsTypesFolder { get; set; } = "OptionSets";
}

[JsonSerializable(typeof(PillaroSettings))]
[JsonSerializable(typeof(PacModelBuilderSettings))]
[JsonSourceGenerationOptions(WriteIndented = true)]
internal sealed partial class PillaroSettingsJsonContext : JsonSerializerContext;

internal sealed class PacModelBuilderSettings
{
    public bool SuppressINotifyPattern { get; set; }

    public bool SuppressGeneratedCodeAttribute { get; set; }

    public string Language { get; set; } = "CS";

    public string Namespace { get; set; } = string.Empty;

    public string ServiceContextName { get; set; } = "DataverseContext";

    public bool GenerateSdkMessages { get; set; }

    public bool GenerateGlobalOptionSets { get; set; }

    public bool EmitFieldsClasses { get; set; } = true;

    public string EntityTypesFolder { get; set; } = "Entities";

    public string MessagesTypesFolder { get; set; } = "Messages";

    public string OptionSetsTypesFolder { get; set; } = "OptionSets";

    public List<string> EntityNamesFilter { get; set; } = [];

    public List<string> MessageNamesFilter { get; set; } = [];

    public bool EmitEntityETC { get; set; }

    public bool EmitVirtualAttributes { get; set; } = true;
}
