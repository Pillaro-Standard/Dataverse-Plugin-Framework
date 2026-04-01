using System;
using System.Linq;
using System.Runtime.Caching;

namespace Pillaro.Dataverse.PluginFramework.Caching;

public class CacheProvider : IDisposable
{
    private readonly MemoryCache _cache;
    private bool _disposed;

    public CacheProvider()
         : this("Pillaro.Dataverse.PluginFramework.Cache")
    {
    }

    public CacheProvider(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Cache name cannot be null or empty.", nameof(name));

        _cache = new MemoryCache(name);
    }

    public virtual void AddItem<T>(string key, T value, DateTimeOffset absoluteExpiration)
    {
        ValidateKey(key);

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        _cache.Set(key, value, absoluteExpiration);
    }

    public virtual void AddItem<T>(string key, T value)
    {
        AddItem(key, value, ObjectCache.InfiniteAbsoluteExpiration);
    }

    public virtual bool ContainsItem(string key)
    {
        ValidateKey(key);
        return _cache.Contains(key);
    }

    public virtual T GetItem<T>(string key)
    {
        if (TryGetItem<T>(key, out var value))
            return value;

        return default;
    }

    public virtual bool TryGetItem<T>(string key, out T value)
    {
        ValidateKey(key);
        var item = _cache.Get(key);

        if (item is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    public virtual T GetAndRemoveItem<T>(string key)
    {
        ValidateKey(key);
        var item = _cache.Remove(key);
        return item is T typed ? typed : default;
    }

    public virtual bool TryGetAndRemoveItem<T>(string key, out T value)
    {
        ValidateKey(key);
        var item = _cache.Remove(key);

        if (item is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    public virtual void RemoveItem(string key)
    {
        ValidateKey(key);
        _cache.Remove(key);
    }

    public virtual void ClearCache()
    {
        var keys = _cache.Select(kvp => kvp.Key).ToList();

        foreach (var key in keys)
        {
            _cache.Remove(key);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _cache.Dispose();
        }

        _disposed = true;
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));
    }
}
