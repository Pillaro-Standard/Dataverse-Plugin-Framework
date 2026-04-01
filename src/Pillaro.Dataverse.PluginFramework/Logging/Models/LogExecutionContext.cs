using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Plugins;
using System;

namespace Pillaro.Dataverse.PluginFramework.Logging.Models;

public class LogExecutionContext
{
    public PluginStage? Stage { get; set; }
    public string EntityName { get; set; }
    public string Message { get; set; }
    public int Depth { get; set; }
    public Guid UserId { get; set; }
    public Guid InitiatingUserId { get; set; }
    public PluginMode Mode { get; set; }
    public string CorrelationId { get; set; }
    public string EntityId { get; set; }

    public LogExecutionContext(IPluginExecutionContext context)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        Stage = (PluginStage)context.Stage;
        EntityName = context.PrimaryEntityName;
        Message = context.MessageName;
        CorrelationId = context.CorrelationId.ToString();
        Depth = context.Depth;
        InitiatingUserId = context.InitiatingUserId;
        Mode = (PluginMode)context.Mode;
        UserId = context.UserId;
        EntityId = context.PrimaryEntityId.ToString();
    }

    public LogExecutionContext()
    {

    }
}