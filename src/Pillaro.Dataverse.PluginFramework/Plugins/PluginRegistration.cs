using System;
using System.Linq;

namespace Pillaro.Dataverse.PluginFramework.Plugins;

public class PluginRegistration
{
    public PluginStage Stage { get; }
    public string MessageName { get; }
    public string EntityName { get; }
    public PluginMode[] Modes { get; }
    public Type TaskType { get; }

    public PluginRegistration(
        PluginStage stage,
        string messageName,
        string entityName,
        PluginMode[] modes,
        Type taskType)
    {
        Stage = stage;
        MessageName = messageName;
        EntityName = entityName;
        Modes = modes ?? [];
        TaskType = taskType;
    }

    public bool Matches(PluginStage stage, string messageName, string entityName, PluginMode mode)
    {
        return Stage == stage
               && string.Equals(MessageName, messageName, StringComparison.OrdinalIgnoreCase)
               && (string.IsNullOrWhiteSpace(EntityName)
                   || string.Equals(EntityName, entityName, StringComparison.OrdinalIgnoreCase))
               && Modes.Contains(mode);
    }
}