using System;
using System.Runtime.Caching;

namespace Pillaro.Dataverse.PluginFramework.Caching;

public abstract class CachingProviderBase
{
    protected MemoryCache Cache = new("CachingProvider");

    protected virtual void AddItem(string key, object value, DateTimeOffset absoluteETimexpiration)
    {
        Cache.Add(key, value, absoluteETimexpiration);
    }

    protected virtual void AddItem(string key, object value)
    {
        AddItem(key, value, DateTimeOffset.MaxValue);
    }

    protected virtual bool ExistItem(string key)
    {
        var res = Cache[key];
        return res != null;
    }

    protected virtual void RemoveItem(string key)
    {
        Cache.Remove(key);
    }

    protected virtual object GetItem(string key, bool remove)
    {
        var res = Cache[key];

        if (res != null)
        {
            if (remove == true)
                Cache.Remove(key);
        }
        else
        {
            return null;
        }

        return res;
    }
}