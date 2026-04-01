using System;
using System.Linq;

namespace Pillaro.Dataverse.PluginFramework.Plugins;

public class PluginRegistration
{
    public PluginStage Stage { get; private set; }
    public string MessageName { get; private set; }
    public string EntityName { get; private set; }
    public PluginMode[] Modes { get; private set; }
    public Type TaskType { get; private set; }

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
        Modes = modes ?? new PluginMode[0];
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