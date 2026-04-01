using Pillaro.Dataverse.PluginFramework.Logging.Models;
using System;

namespace Pillaro.Dataverse.PluginFramework.Logging.Events;

public class BeforeSaveLogEventArgs : EventArgs
{
    public Log Log { get; set; }
}