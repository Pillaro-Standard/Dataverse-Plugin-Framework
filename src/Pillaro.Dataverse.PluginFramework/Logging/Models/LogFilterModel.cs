using Pillaro.Dataverse.PluginFramework.Logging.Enums;
using System;
using System.Collections.Generic;

namespace Pillaro.Dataverse.PluginFramework.Logging.Models;

public class LogFilterModel
{
    public string Entity { get; set; }
    public List<string> TaskNames { get; set; }
    public string LogType { get; set; }
    public LogSeverity? MinimalSeverity { get; set; }
    public List<Guid> UserIds { get; set; }
    public List<Guid> InitiatingUserIds { get; set; }
}