using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace Pillaro.Dataverse.PluginFramework.Testing.Infrastructure.Dataverse;


internal class DataverseConnectionServiceFactory : IDataverseConnectionService
{
    private const int CacheExpirationMinutes = 5;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _memoryCache;

    public DataverseConnectionServiceFactory(IConfiguration configuration, IMemoryCache memoryCache)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
    }

    public IOrganizationServiceAsync2 GetOrganizationService(string connectionStringName = "Dataverse", bool ignoreCache = false)
    {
        if (string.IsNullOrWhiteSpace(connectionStringName))
            throw new ArgumentException("Connection string name cannot be null or whitespace.", nameof(connectionStringName));

        var connectionString = GetConnectionString(connectionStringName);
        ServiceClient.MaxConnectionTimeout = TimeSpan.FromMinutes(30);

        if (ignoreCache)
            return new ServiceClient(connectionString);

        var cacheKey = GetBaseCacheKey(connectionString);
        if (_memoryCache.TryGetValue(cacheKey, out ServiceClient cachedClient) && cachedClient.IsReady)
            return cachedClient;

        var client = new ServiceClient(connectionString);
        _memoryCache.Set(cacheKey, client, DateTimeOffset.UtcNow.AddMinutes(CacheExpirationMinutes));
        return client;
    }

    public IOrganizationServiceAsync2 GetOrganizationService(Guid callerId, string connectionStringName = "Dataverse", bool ignoreCache = false)
    {
        if (string.IsNullOrWhiteSpace(connectionStringName))
            throw new ArgumentException("Connection string name cannot be null or whitespace.", nameof(connectionStringName));

        var connectionString = GetConnectionString(connectionStringName);
        ServiceClient.MaxConnectionTimeout = TimeSpan.FromMinutes(30);

        if (ignoreCache)
            return CreateCallerClient(connectionString, callerId);

        var cacheKey = GetCallerCacheKey(connectionString, callerId);
        if (_memoryCache.TryGetValue(cacheKey, out ServiceClient cachedClient) && cachedClient.IsReady)
            return cachedClient;

        var client = CreateCallerClient(connectionString, callerId);
        _memoryCache.Set(cacheKey, client, DateTimeOffset.UtcNow.AddMinutes(CacheExpirationMinutes));
        return client;
    }

    private string GetConnectionString(string connectionStringName)
    {
        return _configuration.GetConnectionString(connectionStringName)
            ?? throw new InvalidOperationException($"Connection string with name '{connectionStringName}' is missing.");
    }

    private ServiceClient CreateCallerClient(string connectionString, Guid callerId)
    {
        var client = new ServiceClient(connectionString)
        {
            CallerId = callerId
        };
        return client;
    }

    private string GetBaseCacheKey(string connectionString)
    {
        return $"{nameof(IOrganizationService)}:{connectionString}";
    }

    private string GetCallerCacheKey(string connectionString, Guid callerId)
    {
        return $"{GetBaseCacheKey(connectionString)}:{callerId}";
    }
}