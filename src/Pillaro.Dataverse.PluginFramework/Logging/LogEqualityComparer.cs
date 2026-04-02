using Pillaro.Dataverse.PluginFramework.Logging.Models;
using System.Collections.Generic;

namespace Pillaro.Dataverse.PluginFramework.Logging;

/// <summary>
/// Compares <see cref="Log"/> instances by their unique <see cref="Log.LogId"/>.
/// </summary>
public class LogEqualityComparer : IEqualityComparer<Log>
{
    public bool Equals(Log x, Log y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        return x.LogId == y.LogId;
    }

    public int GetHashCode(Log obj)
    {
        return obj?.LogId.GetHashCode() ?? 0;
    }
}