using Pillaro.Dataverse.PluginFramework.Logging.Models;
using System.Collections.Generic;

namespace Pillaro.Dataverse.PluginFramework.Logging;

public class LogEqualityComparer : IEqualityComparer<Log>
{
    public bool Equals(Log x, Log y)
    {
        return x.LogId == y.LogId;
    }

    public int GetHashCode(Log obj)
    {
        return obj.LogId.GetHashCode();
    }
}