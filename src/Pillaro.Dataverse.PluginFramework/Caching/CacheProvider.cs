using System;
using System.Collections.Generic;
using System.Linq;

namespace Pillaro.Dataverse.PluginFramework.Caching;

public class CacheProvider : CachingProviderBase
{
    public CacheProvider()
    {
    }

    public new virtual bool ExistItem(string key)
    {
        return base.ExistItem(key);
    }

    public new virtual void AddItem(string key, object value)
    {
        base.AddItem(key, value);
    }

    public new virtual void AddItem(string key, object value, DateTimeOffset absoluteExpiration)
    {
        base.AddItem(key, value, absoluteExpiration);
    }

    public virtual object GetItem(string key)
    {
        return base.GetItem(key, false);
    }

    public virtual object GetAndRemoveItem(string key)
    {
        return base.GetItem(key, true);
    }

    public new virtual void RemoveItem(string key)
    {
        base.RemoveItem(key);
    }

    public virtual void ClearCache()
    {
        List<string> cacheKeys = Cache.Select(kvp => kvp.Key).ToList();
        foreach (string cacheKey in cacheKeys)
        {
            Cache.Remove(cacheKey);
        }
    }

    public virtual void ClearCache(string key)
    {
        Cache.Remove(key);
    }
}
