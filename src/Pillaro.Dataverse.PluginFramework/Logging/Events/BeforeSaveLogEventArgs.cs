using Pillaro.Dataverse.PluginFramework.Logging.Models;

namespace Pillaro.Dataverse.PluginFramework.Logging.Events;

public class BeforeSaveLogEventArgs : EventArgs
{
    public Log Log { get; set; }
}