using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Pillaro.Dataverse.PluginFramework.Caching;
using Pillaro.Dataverse.PluginFramework.Data;
using Pillaro.Dataverse.PluginFramework.Logging.Enums;
using Pillaro.Dataverse.PluginFramework.Logging.Events;
using Pillaro.Dataverse.PluginFramework.Logging.Models;
using Pillaro.Dataverse.PluginFramework.Settings;
using System.ServiceModel;

namespace Pillaro.Dataverse.PluginFramework.Logging;

public class LogService(IPluginExecutionContext pluginExecutionContext, IOrganizationService systemOrganizationService, ITracingService tracingService, int cacheNoEntityTimeInSeconds = 180)
{
    private readonly ITracingService _tracingService = tracingService;
    private readonly IOrganizationService _systemOrganizationService = systemOrganizationService ?? throw new ArgumentNullException(nameof(systemOrganizationService));
    private readonly LogExecutionContext _executionContext = new(pluginExecutionContext ?? throw new ArgumentNullException(nameof(pluginExecutionContext)));
    private readonly SettingsService _settingService = new(systemOrganizationService);
    private readonly LogFilterService _filterLogService = new();
    private readonly DataService _dataService = new(systemOrganizationService);

    public const string MinimalSeverityLevelKey = "MinimalSeverityLevel";
    public const string LogFilterKey = "Pillaro.LogFilter";

    private const string LogEntityLogicalName = "pl_log";
    private const string SettingEntityLogicalName = "pl_setting";
    private const string LogDetailEntityLogicalName = "pl_logdetail";

    private static readonly CacheProvider CacheProvider = new();

    public delegate void SaveLogEventHandler(object sender, BeforeSaveLogEventArgs e);
    public event SaveLogEventHandler BeforeSaveLog = delegate { };

    public delegate void SaveLogsEventHandler(object sender, BeforeSaveLogsEventArgs e);
    public event SaveLogsEventHandler BeforeSaveLogs = delegate { };

    protected int CacheNoEntityTimeInSeconds { get; } = cacheNoEntityTimeInSeconds;

    public void Debug(string name, string detail, IList<LogDetail> logDetails = null)
    {
        SaveLog(new Log(LogSeverity.Debug, _executionContext, name, detail) { LogDetails = logDetails });
    }

    public void Debug(string detail, IList<LogDetail> logDetails = null)
    {
        Debug(GetCallerName(), detail, logDetails);
    }

    public void Info(string name, string detail, IList<LogDetail> logDetails = null)
    {
        SaveLog(new Log(LogSeverity.Info, _executionContext, name, detail) { LogDetails = logDetails });
    }

    public void Info(string detail, IList<LogDetail> logDetails = null)
    {
        Info(GetCallerName(), detail, logDetails);
    }

    public void Warning(string name, string detail, IList<LogDetail> logDetails = null)
    {
        SaveLog(new Log(LogSeverity.Warning, _executionContext, name, detail) { LogDetails = logDetails });
    }

    public void Warning(string detail, IList<LogDetail> logDetails = null)
    {
        Warning(GetCallerName(), detail, logDetails);
    }

    public void Error(string name, string detail, IList<LogDetail> logDetails = null)
    {
        SaveLog(new Log(LogSeverity.Error, _executionContext, name, detail) { LogDetails = logDetails });
    }

    public void Error(string detail, IList<LogDetail> logDetails = null)
    {
        Error(GetCallerName(), detail, logDetails);
    }

    public virtual void SaveLogs(IEnumerable<Log> logs)
    {
        var logList = logs?.ToList();

        BeforeSaveLogs(this, new BeforeSaveLogsEventArgs
        {
            Logs = logList
        });

        if (!(logList?.Any() ?? false))
            return;

        foreach (var log in logList)
            _tracingService?.Trace(log?.ToString());

        try 
        { 
            var validLogs = logList.Where(o => IsValidForSave(o.LogSeverity)).ToList();

            if (!validLogs.Any())
                return;

            List<LogFilterModel> filters = null;
            if (CanReadSettings())
            {
                try
                {
                    filters = _settingService.GetModel<List<LogFilterModel>>(LogFilterKey, false);
                }
                catch (Exception ex)
                {
                    TraceFallback($"Unable to read log filters from settings. Falling back without filters. {ex}");
                }
            }

            var logsToSave = validLogs.AsEnumerable();
            if (filters != null)
            {
                logsToSave = logsToSave.Union(
                    _filterLogService.GetFilteredLogs(filters, logList),
                    new LogEqualityComparer());
            }

            var validLogEntities = logsToSave
                .Select(GetLogEntity)
                .ToList();

            if (!validLogEntities.Any())
                return;

            var multipleRequest = new ExecuteMultipleRequest
            {
                Settings = new ExecuteMultipleSettings
                {
                    ContinueOnError = true,
                    ReturnResponses = true,
                },
                Requests = []
            };

            foreach (var validLog in validLogEntities)
            {
                multipleRequest.Requests.Add(new CreateRequest
                {
                    Target = validLog
                });
            }

            var response = (ExecuteMultipleResponse)_systemOrganizationService.Execute(multipleRequest);

            if (response.IsFaulted)
            {
                var faults = response.Responses
                    .Where(o => o.Fault != null)
                    .Select(o => o.Fault)
                    .ToList();

                foreach (var fault in faults)
                    TraceFallback($"Create {LogEntityLogicalName} fault: {fault.Message}");

                // Logging must never break business logic.
                return;
            }
        }
        catch (Exception ex)
        {
            TraceFallback($"Create {LogEntityLogicalName} batch exception: {ex}");
        }
    }

    public virtual void SaveLog(Log log)
    {
        try
        {
            BeforeSaveLog(this, new BeforeSaveLogEventArgs { Log = log });

            if (log == null)
                throw new ArgumentNullException(nameof(log));

            _tracingService?.Trace(log.ToString());

            if (!IsValidForSave(log.LogSeverity))
                return;

            var logEntity = GetLogEntity(log);
            var outsideTransaction = _dataService.CreateOutsideTransaction(logEntity);

            if (outsideTransaction != null)
                logEntity.Id = outsideTransaction.Value;
        }
        catch (Exception ex)
        {
            TraceFallback($"Create {LogEntityLogicalName} exception: {ex}");
        }
    }

    protected virtual Entity GetLogEntity(Log log)
    {
        var logEntity = new Entity(LogEntityLogicalName)
        {
            ["pl_name"] = log.Name,
            ["pl_correlationid"] = log.CorrelationId,
            ["pl_depth"] = log.Depth,
            ["pl_detail"] = log.Detail,
            ["pl_elapsedms"] = (int)log.ElapsedTimeInMs,
            ["pl_entity"] = log.Entity,
            ["pl_userid"] = log.User,
            ["pl_initiatinguserid"] = log.InitiatingUser,
            ["pl_logseverity"] = new OptionSetValue((int)log.LogSeverity),
            ["pl_message"] = log.Message,
            ["pl_sortdateutc"] = log.SortDateUtc,
            ["pl_startdate"] = log.StartUtc,
            ["pl_task"] = log.TaskName,
            ["pl_entityid"] = log.EntityId,
        };

        if (log.Mode != null)
            logEntity["pl_mode"] = new OptionSetValue((int)log.Mode);

        if (log.Stage != null)
            logEntity["pl_pluginstage"] = new OptionSetValue((int)log.Stage);

        if (log.Status != null)
            logEntity["pl_taskstatus"] = new OptionSetValue((int)log.Status);

        var logDetailsCol = new EntityCollection();

        foreach (var logDetail in log.LogDetails ?? [])
        {
            if (logDetail == null)
                continue;

            logDetailsCol.Entities.Add(new Entity(LogDetailEntityLogicalName)
            {
                ["pl_detail"] = logDetail.Detail,
                ["pl_name"] = logDetail.Name,
            });
        }

        logEntity.RelatedEntities.Add(
            new KeyValuePair<Relationship, EntityCollection>(
                new Relationship("pl_pl_log_pl_logdetail_LogId"),
                logDetailsCol));

        return logEntity;
    }

    public bool IsValidForSave(LogSeverity severity)
    {
        if (!ExistEntity(LogEntityLogicalName))
            return false;

        if (!ExistEntity(SettingEntityLogicalName))
            return false;

        var minimalSeverityLevel = TryGetMinimalSeverityLevel();
        var maxSeverityLevelToSave = Math.Max(1, minimalSeverityLevel);
        return (int)severity <= maxSeverityLevelToSave;
    }

    private int TryGetMinimalSeverityLevel()
    {
        if (!CanReadSettings())
            return 0;

        try
        {
            return _settingService.GetIntegerValue(MinimalSeverityLevelKey);
        }
        catch (Exception ex)
        {
            TraceFallback($"Unable to read setting '{MinimalSeverityLevelKey}'. Defaulting to 0. {ex}");
            return 0;
        }
    }

    private bool CanReadSettings()
    {
        // podpora pro obě varianty názvu entity
        return ExistEntity(SettingEntityLogicalName);
    }

    private string GetCallerName()
    {
        try
        {
            var frame = new System.Diagnostics.StackTrace(2, false).GetFrame(0);
            if (frame?.GetMethod() != null)
                return frame.GetMethod().Name;
        }
        catch (Exception ex)
        {
            TraceFallback($"GetCallerName exception: {ex}");
        }

        return "N/A";
    }

    private bool ExistEntity(string logicalName)
    {
        var cacheKey = GetEntityExistsCacheKey(logicalName);
        var cachedValue = CacheProvider.GetItem<object>(cacheKey);
        if (cachedValue != null)
            return (bool)cachedValue;

        var expiration = DateTimeOffset.Now.Add(TimeSpan.FromSeconds(CacheNoEntityTimeInSeconds));

        try
        {
            _systemOrganizationService.Execute(new RetrieveEntityRequest
            {
                LogicalName = logicalName,
                EntityFilters = EntityFilters.Entity
            });

            CacheProvider.AddItem(cacheKey, true, expiration);
            return true;
        }
        catch (FaultException<OrganizationServiceFault> ex) when (IsEntityNotFoundFault(ex))
        {
            CacheProvider.AddItem(cacheKey, false, expiration);
            TraceFallback($"Entity '{logicalName}' does not exist. Logging to Dataverse is disabled for {CacheNoEntityTimeInSeconds}s.");
            return false;
        }
        catch (Exception ex)
        {
            CacheProvider.AddItem(cacheKey, false, expiration);
            TraceFallback($"Unable to verify existence of entity '{logicalName}'. Logging to Dataverse is disabled for {CacheNoEntityTimeInSeconds}s. {ex.Message}");
            return false;
        }
    }

    private static string GetEntityExistsCacheKey(string logicalName)
    {
        return $"LogService:EntityExists:{logicalName}";
    }

    private static bool IsEntityNotFoundFault(FaultException<OrganizationServiceFault> ex)
    {
        return ex?.Detail?.ErrorCode == unchecked((int)0x80041102)
               || ex?.Detail?.Message?.IndexOf("was not found in the MetadataCache", StringComparison.OrdinalIgnoreCase) >= 0
               || ex?.Detail?.Message?.IndexOf("does not exist", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void TraceFallback(string message)
    {
        _tracingService?.Trace(message);
    }
}
