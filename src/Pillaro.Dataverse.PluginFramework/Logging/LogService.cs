using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Pillaro.Dataverse.PluginFramework.Caching;
using Pillaro.Dataverse.PluginFramework.Data;
using Pillaro.Dataverse.PluginFramework.Logging.Enums;
using Pillaro.Dataverse.PluginFramework.Logging.Events;
using Pillaro.Dataverse.PluginFramework.Logging.Models;
using Pillaro.Dataverse.PluginFramework.Settings;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Pillaro.Dataverse.PluginFramework.Logging;

public class LogService(IPluginExecutionContext pluginExecutionContext, IOrganizationService systemOrganizationService, ITracingService tracingService, int cacheNoEntityTimeInSeconds = 180)
{
    private readonly ITracingService _tracingService = tracingService;
    private readonly IOrganizationService _systemOrganizationService = systemOrganizationService ?? throw new ArgumentNullException(nameof(systemOrganizationService));
    private readonly LogExecutionContext _executionContext = new(pluginExecutionContext ?? throw new ArgumentNullException(nameof(pluginExecutionContext)));
    private readonly SettingsService _settingService = new(systemOrganizationService);
    private readonly LogFilterService _filterLogService = new();
    private readonly DataService _dataService = new(systemOrganizationService);

    private static readonly string LogEntityExistsCacheKey = "LogService:EntityExists:pl_log";
    public const string MinimalSeverityLevelKey = "MinimalSeverityLevel";
    public const string LogFilterKey = "Pillaro.LogFilter";

    private static readonly CacheProvider CacheProvider = new();

    public delegate void SaveLogEventHandler(object sender, BeforeSaveLogEventArgs e);
    public event SaveLogEventHandler BeforeSaveLog = delegate { };

    public delegate void SaveLogsEventHandler(object sender, BeforeSaveLogsEventArgs e);
    public event SaveLogsEventHandler BeforeSaveLogs = delegate { };

    protected int CacheNoEntityTimeInSeconds { get; } = cacheNoEntityTimeInSeconds;

    public void Fatal(string name, Exception ex)
    {
        var sb = new StringBuilder();
        var current = ex;
        while (current != null)
        {
            sb.AppendLine().Append(current);
            current = current.InnerException;
        }

        SaveLog(new Log(LogSeverity.Fatal, _executionContext, name, sb.ToString()));
    }

    public void Fatal(Exception ex)
    {
        Fatal(ex.Message, ex);
    }

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

        var validLogs = logList.Where(o => IsValidForSave(o.LogSeverity));

        var filters = _settingService.GetModel<List<LogFilterModel>>(LogFilterKey, false);
        validLogs = validLogs.Union(
            _filterLogService.GetFilteredLogs(filters, logList),
            new LogEqualityComparer());

        var validLogEntities = validLogs
            .ToList()
            .Select(GetLogEntity);

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
            var fault = response.Responses
                .Where(o => o.Fault != null)
                .Select(o => o.Fault)
                .First();

            throw new FaultException<OrganizationServiceFault>(fault);
        }
    }

    public virtual void SaveLog(Log log)
    {
        try
        {
            BeforeSaveLog(this, new BeforeSaveLogEventArgs { Log = log });

            if (log == null)
                throw new ArgumentNullException(nameof(log));

            if (!IsValidForSave(log.LogSeverity))
                return;

            _tracingService?.Trace(log.ToString());

            var logEntity = GetLogEntity(log);
            var outsideTransaction = _dataService.CreateOutsideTransaction(logEntity) ?? throw new Exception("OutsideTransaction response is null");
            logEntity.Id = outsideTransaction;
        }
        catch (Exception ex)
        {
            _tracingService?.Trace(string.Format(CultureInfo.InvariantCulture, "Create pl_Log Exception: {0}", ex));
            throw;
        }
    }

    protected virtual Entity GetLogEntity(Log log)
    {
        var logEntity = new Entity("pl_log")
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

            logDetailsCol.Entities.Add(new Entity("pl_logdetail")
            {
                ["pl_detail"] = logDetail.Detail,
                ["pl_name"] = logDetail.Name,
            });
        }

        logEntity.RelatedEntities.Add(new KeyValuePair<Relationship, EntityCollection>(new Relationship("pl_pl_log_pl_logdetail_LogId"), logDetailsCol));

        return logEntity;
    }

    public bool IsValidForSave(LogSeverity severity)
    {
        if (!ExistEntity("pl_log"))
            return false;

        var minimalSeverityLevel = _settingService.GetIntegerValue(MinimalSeverityLevelKey);
        return (int)severity >= minimalSeverityLevel;
    }

    private string GetCallerName()
    {
        try
        {
            // skipFrames=2: skip GetCallerName + the calling wrapper (e.g. Debug/Info/Warning/Error)
            var frame = new System.Diagnostics.StackTrace(2, false).GetFrame(0);
            if (frame?.GetMethod() != null)
                return frame.GetMethod().Name;
        }
        catch (Exception ex)
        {
            Fatal(ex);
        }

        return "N/A";
    }

    private bool ExistEntity(string logicalName)
    {
        var val = CacheProvider.GetItem<object>(LogEntityExistsCacheKey);
        if (val != null)
            return (bool)val;

        var expiration = DateTimeOffset.Now.Add(TimeSpan.FromSeconds(CacheNoEntityTimeInSeconds));

        try
        {
            _systemOrganizationService.Execute(new RetrieveEntityRequest { LogicalName = logicalName, EntityFilters = EntityFilters.Entity });
            CacheProvider.AddItem(LogEntityExistsCacheKey, true, expiration);
            return true;
        }
        catch
        {
            CacheProvider.AddItem(LogEntityExistsCacheKey, false, expiration);
            _tracingService?.Trace($"Entity '{logicalName}' does not exist. Logging to Dataverse is disabled for {CacheNoEntityTimeInSeconds}s.");
            return false;
        }
    }
}
