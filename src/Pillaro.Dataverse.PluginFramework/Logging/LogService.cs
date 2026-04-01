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

namespace Pillaro.Dataverse.PluginFramework.Logging;

public class LogService
{
    private readonly ITracingService _tracingService;
    private readonly IOrganizationService _systemOrganizationService;
    private readonly LogExecutionContext _executionContext;
    private readonly SettingsService _settingService;
    private readonly LogFilterService _filterLogService;
    private readonly DataService _dataService;

    private static readonly string CacheKey = "LogServiceExistLogEntity";
    public const string MinimalSeverityLevelKey = "MinimalSeverityLevel";
    public const string LogFilterKey = "Pillaro.LogFilter";

    private static readonly CacheProvider CacheProvider = new();

    public delegate void SaveLogEventHandler(object sender, BeforeSaveLogEventArgs e);
    public event SaveLogEventHandler BeforeSaveLog = delegate { };

    public delegate void SaveLogsEventHandler(object sender, BeforeSaveLogsEventArgs e);
    public event SaveLogsEventHandler BeforeSaveLogs = delegate { };

    public readonly int CacheNoEntityTimeInSeconds;


    public LogService(IPluginExecutionContext pluginExecutionContext, IOrganizationService systemOrganizationService, ITracingService tracingService, int cacheNoEntityTimeInSeconds = 180)
    {
        if (pluginExecutionContext == null)
            throw new ArgumentException(nameof(pluginExecutionContext));

        if (systemOrganizationService == null)
            throw new ArgumentException(nameof(systemOrganizationService));

        _executionContext = new LogExecutionContext(pluginExecutionContext);

        _systemOrganizationService = systemOrganizationService;
        _tracingService = tracingService;
        _settingService = new SettingsService(systemOrganizationService);
        _filterLogService = new LogFilterService();
        _dataService = new DataService(systemOrganizationService);

        CacheNoEntityTimeInSeconds = cacheNoEntityTimeInSeconds;
    }

    public void Fatal(string name, Exception ex)
    {
        var message = string.Empty;

        var innerException = ex;
        do
        {
            message += Environment.NewLine + innerException;
            innerException = innerException.InnerException;
        }
        while (innerException != null);


        SaveLog(new Log(LogSeverity.Fatal, _executionContext, name, message));
    }
    public void Fatal(Exception ex)
    {
        Fatal(ex.Message, ex);
    }

    public void Debug(string name, string detail, IList<LogDetail> logDetails = null)
    {
        SaveLog(new Log(LogSeverity.Debug, _executionContext, name, detail)
        {
            LogDetails = logDetails
        });
    }
    public void Debug(string detail, IList<LogDetail> logDetails = null)
    {
        var name = "N/A";
        try
        {
            var frame = new System.Diagnostics.StackTrace(1, false).GetFrame(0);
            if (frame?.GetMethod() != null)
                name = frame.GetMethod().Name;
        }
        catch (Exception ex)
        {
            Fatal(ex);
        }

        Debug(name, detail, logDetails);
    }

    public void Info(string name, string detail, IList<LogDetail> logDetails = null)
    {
        SaveLog(new Log(LogSeverity.Info, _executionContext, name, detail)
        {
            LogDetails = logDetails
        });
    }
    public void Info(string detail, IList<LogDetail> logDetails = null)
    {
        var name = "N/A";
        try
        {
            var frame = new System.Diagnostics.StackTrace(1, false).GetFrame(0);
            if (frame?.GetMethod() != null)
                name = frame.GetMethod().Name;
        }
        catch (Exception ex)
        {
            Fatal(ex);
        }
        Info(name, detail, logDetails);
    }

    public void Warning(string name, string detail, IList<LogDetail> logDetails = null)
    {
        var log = new Log(LogSeverity.Warning, _executionContext, name, detail)
        {
            LogDetails = logDetails
        };
        SaveLog(log);
    }
    public void Warning(string detail, IList<LogDetail> logDetails = null)
    {
        var name = "N/A";
        try
        {
            var frame = new System.Diagnostics.StackTrace(1, false).GetFrame(0);
            if (frame?.GetMethod() != null)
                name = frame.GetMethod().Name;
        }
        catch (Exception ex)
        {
            Fatal(ex);
        }
        Warning(name, detail, logDetails);
    }

    public void Error(string name, string detail, IList<LogDetail> logDetails = null)
    {
        var log = new Log(LogSeverity.Error, _executionContext, name, detail)
        {
            LogDetails = logDetails
        };
        SaveLog(log);
    }
    public void Error(string detail, IList<LogDetail> logDetails = null)
    {
        var name = "N/A";
        try
        {
            var frame = new System.Diagnostics.StackTrace(1, false).GetFrame(0);
            if (frame?.GetMethod() != null)
                name = frame.GetMethod().Name;
        }
        catch (Exception ex)
        {
            Fatal(ex);
        }
        Error(name, detail, logDetails);
    }

    public virtual void SaveLogs(IEnumerable<Log> logs)
    {
        BeforeSaveLogs(this, new BeforeSaveLogsEventArgs { Logs = logs });

        if (!(logs?.Any() ?? false))
            return;

        foreach (var log in logs)
        {
            _tracingService?.Trace(log?.ToString());
        }

        var validLogs = logs.Where(o => IsValidForSave(o.LogSeverity));

        var filters = _settingService.GetModel<List<LogFilterModel>>(LogFilterKey, false);
        validLogs = validLogs.Union(_filterLogService.GetFilteredLogs(filters, logs.ToList()), new LogEqualityComparer());

        var validLogEntities = validLogs.ToList().Select(GetLogEntity);

        var multipleRequest = new ExecuteMultipleRequest()
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
            var falut = response.Responses
                .Where(o => o.Fault != null).Select(o => o.Fault).First();

            throw new FaultException<OrganizationServiceFault>(falut);
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

            /* Write to trace */
            _tracingService?.Trace(log.ToString());

            var logEntity = GetLogEntity(log);

            var outsideTransaction = _dataService.CreateOutsideTransaction(logEntity);
            if (outsideTransaction == null)
                throw new Exception("OutsideTransaction response is null");

            logEntity.Id = outsideTransaction.Value;

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


        var logDetaislCol = new EntityCollection();

        foreach (var logDetail in log.LogDetails ?? [])
        {
            if (logDetail == null)
                continue;

            var detail = new Entity("pl_logdetail")
            {
                ["pl_detail"] = logDetail.Detail,
                ["pl_name"] = logDetail.Name,
            };

            logDetaislCol.Entities.Add(detail);
        }

        logEntity.RelatedEntities.Add(new KeyValuePair<Relationship, EntityCollection>(new Relationship("pl_pl_log_pl_logdetail_LogId"), logDetaislCol));

        return logEntity;
    }



    public bool IsValidForSave(LogSeverity severity)
    {
        if (!ExistEntity("pl_log"))
            return false;

        var minimalSeverityLevel = _settingService.GetIntegerValue(MinimalSeverityLevelKey);
        if ((int)severity < minimalSeverityLevel)
            return false;

        return true;
    }

    private bool ExistEntity(string logicalName)
    {
        var val = CacheProvider.GetItem(CacheKey);
        if (val != null)
        {
            if ((bool)val)
                _tracingService?.Trace($"{FrameworkConstants.ProductName} is licensed. Information is returned from cache. Cache will be reset in {CacheNoEntityTimeInSeconds}s period.");
            else
                _tracingService?.Trace($"{FrameworkConstants.ProductName} is in Light version. Information is returned from cache. Cache will be reset in {CacheNoEntityTimeInSeconds}s period.");

            return (bool)val;
        }

        try
        {
            // If the entity does not exist, the organization service throws — used intentionally for license detection.
            var entMetDataRes = _systemOrganizationService.Execute(new RetrieveEntityRequest
            {
                LogicalName = logicalName,
                EntityFilters = EntityFilters.All
            });

            CacheProvider.AddItem(CacheKey, true, DateTimeOffset.Now.Add(TimeSpan.FromSeconds(CacheNoEntityTimeInSeconds)));
            _tracingService?.Trace($"{FrameworkConstants.ProductName} is licensed. Value is returned from Request.");
            return true;
        }
        catch
        {
            CacheProvider.AddItem(CacheKey, false, DateTimeOffset.Now.Add(TimeSpan.FromSeconds(CacheNoEntityTimeInSeconds)));
            _tracingService?.Trace($"{FrameworkConstants.ProductName} is in Light version. Value is returned from Request.");
            return false;
        }
    }
}
