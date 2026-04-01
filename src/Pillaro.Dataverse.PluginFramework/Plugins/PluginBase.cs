using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Exceptions;
using Pillaro.Dataverse.PluginFramework.Logging.Enums;
using Pillaro.Dataverse.PluginFramework.Logging;
using Pillaro.Dataverse.PluginFramework.Logging.Models;
using Pillaro.Dataverse.PluginFramework.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Pillaro.Dataverse.PluginFramework.Plugins;

public abstract class PluginBase : IPlugin
{
    private readonly List<PluginRegistration> _registeredEvents = [];
    private readonly string _secureConfig;
    private readonly string _unsecureConfig;

    public PluginBase(string unsecureConfig, string secureConfig)
    {
        _secureConfig = secureConfig;
        _unsecureConfig = unsecureConfig;
    }

    public void Execute(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));

        var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
        try
        {
            var stop = Stopwatch.StartNew();

            var execContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            if (execContext == null)
                throw new ArgumentNullException(nameof(execContext));

            var taskContext = new TaskContext(_unsecureConfig, _secureConfig, execContext);
            if (taskContext.PluginExecutionContext == null)
                throw new ArgumentNullException(nameof(taskContext.PluginExecutionContext));

            var orgServiceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            if (orgServiceFactory == null)
                throw new ArgumentNullException(nameof(orgServiceFactory));

            var userOrgSvc = orgServiceFactory.CreateOrganizationService(execContext.UserId);
            var adminOrgSvc = orgServiceFactory.CreateOrganizationService(null);
            var logService = new LogService(execContext, adminOrgSvc, tracingService);


            var logData = true;

            try
            {
                var entityAction = _registeredEvents
                    .Where(a => a.Matches(
                        (PluginStage)taskContext.PluginExecutionContext.Stage,
                        taskContext.PluginExecutionContext.MessageName,
                        taskContext.PluginExecutionContext.PrimaryEntityName,
                        taskContext.Mode))
                    .ToList();

                taskContext.CountOfTasks = entityAction.Count;
                taskContext.Version = GetSolutionVersion();

                object instance;
                try
                {
                    entityAction.ForEach(o =>
                    {
                        taskContext.TaskOrder++;

                        object[] taskArgs = [serviceProvider, taskContext];
                        
                        try
                        {
                            instance = Activator.CreateInstance(o.TaskType, taskArgs);
                        }
                        catch (MissingMethodException ex)
                        {
                            throw new InvalidOperationException(
                                $"Registered task '{o.TaskType.FullName}' must define a constructor accepting IServiceProvider and TaskContext.",
                                ex);
                        }

                        if (instance is not ITask task)
                        {
                            throw new InvalidOperationException($"Registered task '{o.TaskType.FullName}' does not implement '{nameof(ITask)}'.");
                        }

                        var taskLog = task.GetTaskLog();

                        EnrichLogWithParametersAndImages(taskLog, taskContext.PluginExecutionContext);

                        taskContext.AddLog(taskLog);

                        task.Execute();
                    });
                }
                catch
                {
                    logService.SaveLogs(taskContext.GetLogs());
                    logData = false;
                    throw;
                }

                var checkSumMessage = $"Task Execution elapsed time is {stop.ElapsedMilliseconds} ms{Environment.NewLine}";
                stop.Restart();

                // If no tasks found still create an executor-level log (with parameters/images if present)
                if (entityAction.Count == 0)
                {
                    var executorLog = new Log(LogSeverity.Info, new LogExecutionContext(taskContext.PluginExecutionContext), "Plugin")
                    {
                        StartUtc = DateTime.UtcNow,
                        TaskName = "",
                        Detail = $"Framework: {FrameworkConstants.FrameworkVersion}{Environment.NewLine}" +
                        $"Version: {GetSolutionVersion()}{Environment.NewLine}" +
                        $"No registered tasks for this event."

                    };

                    // Enrich task log with parameters/images
                    EnrichLogWithParametersAndImages(executorLog, taskContext.PluginExecutionContext);
                    logService.SaveLog(executorLog);
                    logData = false;
                }


                checkSumMessage += $"SaveUpdateEntity elapsed time is {stop.ElapsedMilliseconds} ms{Environment.NewLine}";
                stop.Restart();
                var logs = taskContext.GetLogs();
                logService.SaveLogs(logs);

                checkSumMessage += $"SaveLogs count{logs.Count()} elapsed time is {stop.ElapsedMilliseconds} ms{Environment.NewLine}";
                stop.Stop();

                tracingService.Trace($"Pillaro Plugin Executor Elapsed Times: {checkSumMessage}");
            }
            catch (DataverseValidationException notlogex)
            {
                throw new InvalidPluginExecutionException(notlogex.Message, notlogex);
            }
            catch (Exception ex)
            {
                if (logData)
                    logService.Error($"{GetSolutionVersion()} - Execute Exception: " + ex);

                throw;
            }
        }
        catch (Exception ex)
        {
            tracingService.Trace($"{GetSolutionVersion()} - Critical Plugin Executor Exception: {ex}");
            throw new InvalidPluginExecutionException(ex.Message, ex);
        }
    }

    private void EnrichLogWithParametersAndImages(Log log, IPluginExecutionContext ctx)
    {
        if (log == null || ctx == null)
            return;

        log.LogDetails ??= [];

        if (ctx.InputParameters != null && ctx.InputParameters.Any())
        {
            foreach (var item in ctx.InputParameters)
            {
                log.LogDetails.Add(new LogDetail($"InputParameters - {item.Key}", item.Value));
            }
        }

        if (ctx.OutputParameters != null && ctx.OutputParameters.Any())
        {
            foreach (var item in ctx.OutputParameters)
            {
                log.LogDetails.Add(new LogDetail($"OutputParameters - {item.Key}", item.Value));
            }
        }

        if (ctx.PreEntityImages != null && ctx.PreEntityImages.Any())
        {
            foreach (var item in ctx.PreEntityImages)
            {
                log.LogDetails.Add(new LogDetail($"PreEntityImages - {item.Key}", item.Value));
            }
        }

        if (ctx.PostEntityImages != null && ctx.PostEntityImages.Any())
        {
            foreach (var item in ctx.PostEntityImages)
            {
                log.LogDetails.Add(new LogDetail($"PostEntityImages - {item.Key}", item.Value));
            }
        }
    }


    #region Register task

    public void UnRegisterTask<TTask>(PluginStage stage, string messageName, string entityName, PluginMode mode)
         where TTask : ITask
    {
        var ev = _registeredEvents.SingleOrDefault(o =>
            o.Stage == stage &&
            string.Equals(o.MessageName, messageName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(o.EntityName, entityName, StringComparison.OrdinalIgnoreCase) &&
            o.Modes.Contains(mode) &&
            o.TaskType == typeof(TTask));

        if (ev != null)
        {
            _registeredEvents.Remove(ev);
        }
    }

    public void RegisterTask<TTask>(PluginStage stage, string messageName, string entityName, PluginMode mode)
        where TTask : ITask
    {
        _registeredEvents.Add(new PluginRegistration(
            stage,
            messageName,
            entityName,
            [mode],
            typeof(TTask)));
    }

    public void RegisterTask<TTask>(PluginStage stage, string[] messageNames, string entityName, PluginMode mode)
        where TTask : ITask
    {
        foreach (var message in messageNames)
        {
            _registeredEvents.Add(new PluginRegistration(
                stage,
                message,
                entityName,
                [mode],
                typeof(TTask)));
        }
    }

    public void RegisterTask<TTask>(PluginStage stage, string[] messageNames, string[] entityNames, PluginMode mode)
        where TTask : ITask
    {
        foreach (var entityName in entityNames)
        {
            foreach (var message in messageNames)
            {
                _registeredEvents.Add(new PluginRegistration(
                    stage,
                    message,
                    entityName,
                    [mode],
                    typeof(TTask)));
            }
        }
    }

    #endregion

    protected ReadOnlyCollection<PluginRegistration> GetAllRegisteredEvents()
    {
        return _registeredEvents.AsReadOnly();
    }

    public virtual string GetSolutionVersion() => "Unknown";
}