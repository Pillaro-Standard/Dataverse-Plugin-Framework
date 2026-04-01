using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Logging.Enums;
using Pillaro.Dataverse.PluginFramework.Plugins;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Pillaro.Dataverse.PluginFramework.Logging.Models;

public class Log
{
    public Guid LogId { get; } = Guid.NewGuid();
    public LogSeverity LogSeverity { get; set; }
    public string TaskName { get; set; }
    public DateTime? StartUtc { get; set; }
    public double ElapsedTimeInMs { get; set; }
    public TaskStatus? Status { get; set; }
    public string Entity { get; }
    public PluginStage? Stage { get; }
    public string Message { get; set; }
    public int Depth { get; }
    public EntityReference User { get; }
    public EntityReference InitiatingUser { get; }
    public PluginMode? Mode { get; }
    public string CorrelationId { get; }
    public string Name { get; }
    public string Detail { get; set; }
    public DateTime CreateDateUtc { get; } = DateTime.UtcNow;
    public string SortDateUtc => CreateDateUtc.ToString("yyyy-MM-ddTHH:mm:ss.ffff", CultureInfo.InvariantCulture);
    public string EntityId { get; set; }
    public IList<LogDetail> LogDetails { get; set; }
    public Log(LogSeverity logSeverity, LogExecutionContext logExecutionContext, string name, string detail = null)
    {
        if (logExecutionContext == null)
            throw new ArgumentNullException(nameof(logExecutionContext));

        Stage = logExecutionContext.Stage;
        Entity = logExecutionContext.EntityName;
        Message = logExecutionContext.Message;
        Depth = logExecutionContext.Depth;
        InitiatingUser = new EntityReference("systemuser", logExecutionContext.InitiatingUserId);
        User = new EntityReference("systemuser", logExecutionContext.UserId);
        Mode = (PluginMode)logExecutionContext.Mode;
        CorrelationId = logExecutionContext.CorrelationId;
        EntityId = logExecutionContext.EntityId;
        Name = name;
        Detail = detail;
        LogSeverity = logSeverity;
        LogDetails = [];
    }

    public Log(LogSeverity logSeverity, string name, string detail = null)
    {
        Name = name;
        Detail = detail;
        LogSeverity = logSeverity;
        LogDetails = [];
    }

    public void AddDetail(string name, string detail)
    {
        LogDetails.Add(new LogDetail(name, detail));
    }
    public override string ToString()
    {
        return
            $@"****************{Name}****************
             Entity Id: {EntityId}
             Detail: {Detail} 
             Severity: {LogSeverity}
             Task: {TaskName}
             Start: {StartUtc}
             Elapsed Time in ms:{ElapsedTimeInMs}
             Status: {Status}
             Entity: {Entity}
             Stage: {Stage}
             Depth: {Depth}
             User: {User?.Id}
             Initiating User: {InitiatingUser.Id}
             Mode: {Mode}
             Correlation Id: {CorrelationId}
             Message: {Message}";
    }

    public object Clone()
    {
        var clone = (Log)MemberwiseClone();

        clone.LogDetails = LogDetails?
            .Select(x => x == null ? null : new LogDetail(x.Name, x.Detail))
            .ToList();

        return clone;
    }
}