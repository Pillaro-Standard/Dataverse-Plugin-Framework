using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Exceptions;
using Pillaro.Dataverse.PluginFramework.Logging.Enums;
using Pillaro.Dataverse.PluginFramework.Logging;
using Pillaro.Dataverse.PluginFramework.Logging.Models;
using Pillaro.Dataverse.PluginFramework.PluginRegistrations;
using Pillaro.Dataverse.PluginFramework.Tasks;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Pillaro.Dataverse.PluginFramework.Plugins;

public abstract class PluginBase(string unsecureConfig, string secureConfig) : IPlugin
{
    private readonly List<PluginRegistration> _registeredEvents = [];
    private readonly string _secureConfig = secureConfig;
    private readonly string _unsecureConfig = unsecureConfig;

    public virtual void Register(IPluginRegistration registration)
    {
    }

    public void Execute(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));

        var tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
        try
        {
            var stop = Stopwatch.StartNew();

            var execContext = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext)) 
                ?? throw new InvalidOperationException($"Required service '{nameof(IPluginExecutionContext)}' was not provided by IServiceProvider.");

            var taskContext = new TaskContext(_unsecureConfig, _secureConfig, execContext);
            if (taskContext.PluginExecutionContext == null)
                throw new InvalidOperationException(
                    $"{nameof(TaskContext)}.{nameof(TaskContext.PluginExecutionContext)} was not initialized.");

            var orgServiceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory))
                ?? throw new InvalidOperationException($"Required service '{nameof(IOrganizationServiceFactory)}' was not provided by IServiceProvider.");

            var userOrgSvc = orgServiceFactory.CreateOrganizationService(execContext.UserId);
            var adminOrgSvc = orgServiceFactory.CreateOrganizationService(null);
            var logService = new LogService(execContext, adminOrgSvc, tracingService);

            var logData = true;
            var queuedUpdatesMessage = "Queued updates: none";

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
                taskContext.Version = GetVersion();

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
                                $"Registered task '{o.TaskType.FullName}' must have a constructor with IServiceProvider and TaskContext.",
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

                    queuedUpdatesMessage = ApplyEntitiesToUpdate(taskContext, userOrgSvc);
                }
                catch
                {
                    logService.SaveLogs(taskContext.GetLogs());
                    logData = false;
                    throw;
                }

                var checkSumMessage = $"Execution: {stop.ElapsedMilliseconds} ms{Environment.NewLine}";
                checkSumMessage += queuedUpdatesMessage + Environment.NewLine;
                stop.Restart();

                if (entityAction.Count == 0)
                {
                    var executorLog = new Log(LogSeverity.Info, new LogExecutionContext(taskContext.PluginExecutionContext), "Plugin")
                    {
                        StartUtc = DateTime.UtcNow,
                        TaskName = "",
                        Detail = $"Framework: {FrameworkConstants.FrameworkVersion} | Plugin: {GetVersion()} | No tasks registered."
                    };

                    EnrichLogWithParametersAndImages(executorLog, taskContext.PluginExecutionContext);
                    logService.SaveLog(executorLog);
                    logData = false;
                }

                checkSumMessage += $"Save log entity: {stop.ElapsedMilliseconds} ms{Environment.NewLine}";
                stop.Restart();
                var logs = taskContext.GetLogs();
                logService.SaveLogs(logs);

                checkSumMessage += $"Save logs: {logs.Count()} item(s) in {stop.ElapsedMilliseconds} ms{Environment.NewLine}";
                stop.Stop();

                tracingService.Trace($"Plugin execution summary:{Environment.NewLine}{checkSumMessage}");
            }
            catch (DataverseValidationException notlogex)
            {
                throw new InvalidPluginExecutionException(notlogex.Message, notlogex);
            }
            catch (Exception ex)
            {
                if (logData)
                    logService.Error($"Plugin: {GetVersion()} | Execution failed: {ex}");

                throw;
            }
        }
        catch (Exception ex)
        {
            tracingService?.Trace($"Plugin: {GetVersion()} | Critical error: {ex}");
            throw new InvalidPluginExecutionException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Writes the entities queued by the tasks through <see cref="TaskContext.AddEntityToUpdate"/>.
    /// Each record is written once, no matter how many tasks contributed attributes to it, so the
    /// registered steps are not triggered repeatedly. In a pre-stage, values for the record the plugin
    /// is running on are merged into the message target instead of being written separately.
    /// </summary>
    private static string ApplyEntitiesToUpdate(TaskContext taskContext, IOrganizationService organizationService)
    {
        var entitiesToUpdate = taskContext.EntitiesToUpdate;

        if (entitiesToUpdate == null || entitiesToUpdate.Count == 0)
            return "Queued updates: none";

        var mergeTarget = GetTargetForQueuedUpdates(taskContext);
        var mergedCount = 0;
        var updatedCount = 0;

        foreach (var entityToUpdate in entitiesToUpdate)
        {
            if (mergeTarget != null && IsSameRecord(mergeTarget, entityToUpdate))
            {
                foreach (var attribute in entityToUpdate.Attributes)
                {
                    mergeTarget[attribute.Key] = attribute.Value;
                }

                mergedCount++;
                continue;
            }

            organizationService.Update(entityToUpdate);
            updatedCount++;
        }

        taskContext.ClearEntitiesToUpdate();

        return $"Queued updates: {mergedCount} merged into target, {updatedCount} updated";
    }

    /// <summary>
    /// Returns the message target the queued values can be merged into, or null when the queued
    /// entities have to be written separately.
    /// </summary>
    private static Entity GetTargetForQueuedUpdates(TaskContext taskContext)
    {
        if (taskContext.Stage != PluginStage.Prevalidation && taskContext.Stage != PluginStage.Preoperation)
            return null;

        var inputParameters = taskContext.PluginExecutionContext?.InputParameters;

        if (inputParameters == null || !inputParameters.ContainsKey(ExecutionContextParameters.TargetParam))
            return null;

        return inputParameters[ExecutionContextParameters.TargetParam] as Entity;
    }

    private static bool IsSameRecord(Entity target, Entity entity)
    {
        return string.Equals(target.LogicalName, entity.LogicalName, StringComparison.OrdinalIgnoreCase)
               && target.Id == entity.Id;
    }

    private static void EnrichLogWithParametersAndImages(Log log, IPluginExecutionContext ctx)
    {
        if (log == null || ctx == null)
            return;

        log.LogDetails ??= [];

        if (ctx.InputParameters != null && ctx.InputParameters.Any())
        {
            foreach (var item in ctx.InputParameters)
            {
                log.LogDetails.Add(new LogDetail($"Input parameter: {item.Key}", item.Value));
            }
        }

        if (ctx.OutputParameters != null && ctx.OutputParameters.Any())
        {
            foreach (var item in ctx.OutputParameters)
            {
                log.LogDetails.Add(new LogDetail($"Output parameter: {item.Key}", item.Value));
            }
        }

        if (ctx.PreEntityImages != null && ctx.PreEntityImages.Any())
        {
            foreach (var item in ctx.PreEntityImages)
            {
                log.LogDetails.Add(new LogDetail($"Pre image: {item.Key}", item.Value));
            }
        }

        if (ctx.PostEntityImages != null && ctx.PostEntityImages.Any())
        {
            foreach (var item in ctx.PostEntityImages)
            {
                log.LogDetails.Add(new LogDetail($"Post image: {item.Key}", item.Value));
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

    public virtual string GetVersion() => "Unknown";
}
