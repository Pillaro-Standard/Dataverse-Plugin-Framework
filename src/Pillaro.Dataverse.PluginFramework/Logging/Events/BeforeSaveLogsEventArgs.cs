using Pillaro.Dataverse.PluginFramework.Logging.Models;

namespace Pillaro.Dataverse.PluginFramework.Logging.Events;

public class BeforeSaveLogsEventArgs : EventArgs
{
    public IEnumerable<Log> Logs { get; set; }
}