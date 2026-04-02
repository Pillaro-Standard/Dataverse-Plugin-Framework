using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;


internal class DataverseConnectionServiceFactory : IDataverseConnectionService
{
    private const int CacheExpirationMinutes = 5;
    private readonly string _connectionString;
    private readonly IMemoryCache _memoryCache;

    private string BaseCacheKey => $"{nameof(IOrganizationService)}:{_connectionString}";

    public DataverseConnectionServiceFactory(IConfiguration configuration, IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _connectionString = configuration?.GetConnectionString("Dataverse")
            ?? throw new InvalidOperationException("Missing connection string with name 'Dataverse'.");
    }

    public IOrganizationServiceAsync2 GetOrganizationService(bool ignoreCache = false)
    {
        ServiceClient.MaxConnectionTimeout = TimeSpan.FromMinutes(30);

        if (ignoreCache)
            return new ServiceClient(_connectionString);

        if (_memoryCache.TryGetValue(BaseCacheKey, out ServiceClient cachedClient) && cachedClient.IsReady)
            return cachedClient;

        var client = new ServiceClient(_connectionString);
        _memoryCache.Set(BaseCacheKey, client, DateTimeOffset.UtcNow.AddMinutes(CacheExpirationMinutes));
        return client;
    }

    public IOrganizationServiceAsync2 GetOrganizationService(Guid callerId, bool ignoreCache = false)
    {
        ServiceClient.MaxConnectionTimeout = TimeSpan.FromMinutes(30);

        if (ignoreCache)
            return CreateCallerClient(callerId);

        var cacheKey = GetCallerCacheKey(callerId);
        if (_memoryCache.TryGetValue(cacheKey, out ServiceClient cachedClient) && cachedClient.IsReady)
            return cachedClient;

        var client = CreateCallerClient(callerId);
        _memoryCache.Set(cacheKey, client, DateTimeOffset.UtcNow.AddMinutes(CacheExpirationMinutes));
        return client;
    }

    private ServiceClient CreateCallerClient(Guid callerId)
    {
        var client = new ServiceClient(_connectionString)
        {
            CallerId = callerId
        };
        return client;
    }

    private string GetCallerCacheKey(Guid callerId)
    {
        return $"{BaseCacheKey}:{callerId}";
    }
}