using Pillaro.Dataverse.PluginFramework.Logging.Models;
using System;
using System.Collections.Generic;

namespace Pillaro.Dataverse.PluginFramework.Logging.Events;

public class BeforeSaveLogsEventArgs : EventArgs
{
    public IEnumerable<Log> Logs { get; set; }
}