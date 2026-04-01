using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using Pillaro.Dataverse.PluginFramework.Caching;
using System;
using System.Linq;

namespace Pillaro.Dataverse.PluginFramework.Settings;

public class SettingsService
{
    private readonly IOrganizationService _organizationService;

    private static readonly CacheProvider CacheProvider = new();

    protected double CacheTimeInSeconds { get; }

    public SettingsService(IOrganizationService organizationService, int cacheTimeInSeconds = 60)
    {
        CacheTimeInSeconds = cacheTimeInSeconds;
        _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
    }

    public string GetTextValue(string key)
    {
        var val = CacheProvider.GetItem(key);
        if (val == null)
        {
            val = GetTextValue(_organizationService, key);
            if (string.IsNullOrWhiteSpace(val?.ToString()))
                throw new Exception($"SettingsService => Key '{key}' value is null or empty.");

            CacheProvider.AddItem(key, val, DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(CacheTimeInSeconds)));
        }

        return val.ToString();
    }

    public string GetJsonValue(string key, bool throwException = true)
    {
        var val = CacheProvider.GetItem(key);
        if (val == null)
        {
            val = GetJsonValue(_organizationService, key);

            if (string.IsNullOrEmpty(val?.ToString()) && throwException)
                throw new Exception($"SettingsService => Key '{key}' value is null or empty.");

            val ??= string.Empty;

            CacheProvider.AddItem(key, val, DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(CacheTimeInSeconds)));
        }

        return val.ToString();
    }

    public TModel GetModel<TModel>(string key, bool throwException = true) where TModel : class
    {
        var json = GetJsonValue(key, throwException);
        if (!string.IsNullOrEmpty(json))
            return JsonConvert.DeserializeObject<TModel>(json);

        return default;
    }

    public int GetIntegerValue(string key)
    {
        var val = (int?)CacheProvider.GetItem(key);
        if (val == null)
        {
            val = GetIntegerValue(_organizationService, key);
            CacheProvider.AddItem(key, val, DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(CacheTimeInSeconds)));
        }

        return val.Value;
    }

    public bool GetBoolValue(string key)
    {
        var val = (bool?)CacheProvider.GetItem(key);
        if (val == null)
        {

            val = GetBoolValue(_organizationService, key);
            CacheProvider.AddItem(key, val, DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(CacheTimeInSeconds)));
        }

        return val.Value;
    }

    public decimal GetDecimalValue(string key)
    {
        var val = (decimal?)CacheProvider.GetItem(key);
        if (val == null)
        {
            val = GetDecimalValue(_organizationService, key);
            CacheProvider.AddItem(key, val, DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(CacheTimeInSeconds)));
        }

        return val.Value;
    }

    public DateTime GetDateTimeValue(string key)
    {
        DateTime? val = (DateTime?)CacheProvider.GetItem(key);
        if (val == null)
        {
            val = GetDateTimeValue(_organizationService, key);
            CacheProvider.AddItem(key, val, DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(CacheTimeInSeconds)));
        }

        return val.Value;
    }

    protected static string GetTextValue(IOrganizationService service, string key)
    {
        var entity = GetEntityByKey(service, key);
        if (entity == null)
            return null;

        if (!entity.Attributes.Contains("pl_text"))
            return null;

        return (string)entity.Attributes["pl_text"];
    }

    protected static string GetJsonValue(IOrganizationService service, string key)
    {
        var entity = GetEntityByKey(service, key);
        if (entity == null)
            return null;

        if (!entity.Attributes.Contains("pl_json"))
            return null;

        return (string)entity.Attributes["pl_json"];
    }

    protected static int GetIntegerValue(IOrganizationService service, string key)
    {
        var i = GetValue<int>(service, key);
        return i;
    }

    protected static bool GetBoolValue(IOrganizationService service, string key)
    {
        var i = GetValue<bool>(service, key);
        return i;
    }

    protected static decimal GetDecimalValue(IOrganizationService service, string key)
    {
        var i = GetValue<decimal>(service, key);
        return i;
    }

    protected static DateTime GetDateTimeValue(IOrganizationService service, string key)
    {
        var i = GetValue<DateTime>(service, key);
        return i;
    }

    protected static Entity GetEntityByKey(IOrganizationService service, string key)
    {
        QueryByAttribute queryByAttribute = new("pl_setting")
        {
            ColumnSet = new ColumnSet("pl_text", "pl_bool", "pl_date", "pl_decimal", "pl_int", "pl_description", "pl_json")
        };
        queryByAttribute.Attributes.Add("pl_key");
        queryByAttribute.Values.Add(key);

        var entityCollection = service.RetrieveMultiple(queryByAttribute);

        if (entityCollection?.Entities == null || !entityCollection.Entities.Any())
            return null;

        // pl_key is enforced as unique at the database level, so at most one record is expected here.
        return entityCollection.Entities.First();
    }

    private static T GetValue<T>(IOrganizationService service, string key)
        where T : struct
    {
        var entity = GetEntityByKey(service, key);
        if (entity == null)
            throw new Exception($"SettingsService => Key '{key}' value is null or empty.");

        var type = typeof(T);
        var typeCode = Type.GetTypeCode(type);

        switch (typeCode)
        {
            case TypeCode.Int32:
                {
                    if (!entity.Attributes.Contains("pl_int"))
                        throw new Exception($"SettingsService => Key '{key}' value is null or empty.");
                    return (T)entity.Attributes["pl_int"];
                }

            case TypeCode.Decimal:
                {
                    if (!entity.Attributes.Contains("pl_decimal"))
                        throw new Exception($"SettingsService => Key '{key}' value is null or empty.");
                    return (T)entity.Attributes["pl_decimal"];
                }

            case TypeCode.Boolean:
                {
                    if (!entity.Attributes.Contains("pl_bool"))
                        throw new Exception($"SettingsService => Key '{key}' value is null or empty.");
                    var bv = Convert.ToBoolean(((OptionSetValue)entity.Attributes["pl_bool"]).Value);
                    return (T)(object)bv;
                }

            case TypeCode.DateTime:
                {
                    if (!entity.Attributes.Contains("pl_date"))
                        throw new Exception($"SettingsService => Key '{key}' value is null or empty.");
                    return (T)entity.Attributes["pl_date"];
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(typeCode), $"{nameof(typeCode)} is outside of valid range, Key: {key} , T: {type}");
        }
    }
}