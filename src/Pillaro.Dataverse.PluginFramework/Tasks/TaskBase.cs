using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Exceptions;
using Pillaro.Dataverse.PluginFramework.Logging.Enums;
using Pillaro.Dataverse.PluginFramework.Settings;
using Pillaro.Dataverse.PluginFramework.Data;
using Pillaro.Dataverse.PluginFramework.Logging.Models;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using System.Diagnostics;
using System.Text;
using TaskStatus = Pillaro.Dataverse.PluginFramework.Logging.Enums.TaskStatus;

namespace Pillaro.Dataverse.PluginFramework.Tasks;

public abstract class TaskBase<TEntity> : ITask
    where TEntity : Entity, new()
{

    private const string DefaultImageName = "image";

    protected readonly TaskContext TaskContext;
    protected readonly Log Log;
    protected readonly ITracingService TracingService;
    protected readonly SettingsService SettingService;
    protected readonly DataServiceProvider DataServiceProvider;
    protected readonly OrganizationServiceProvider OrganizationServiceProvider;

    protected TEntity ContextEntity { get; private set; }
    protected TEntity PreImage { get; private set; }
    protected TEntity PostImage { get; private set; }
    protected EntityReference ContextEntityReference { get; private set; }

    protected string ExecutionMessage { get; private set; }

    private readonly IBasicModeValidation _validator;

    protected TaskBase(IServiceProvider serviceProvider, TaskContext taskContext)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));

        TaskContext = taskContext ?? throw new ArgumentNullException(nameof(taskContext));

        var executionContext = GetRequiredService<IPluginExecutionContext>(serviceProvider);
        var serviceFactory = GetRequiredService<IOrganizationServiceFactory>(serviceProvider);
        TracingService = GetRequiredService<ITracingService>(serviceProvider);

        ContextEntityReference = new EntityReference(TaskContext.PrimaryEntityName, TaskContext.PrimaryEntityId);

        OrganizationServiceProvider = new OrganizationServiceProvider(
            serviceFactory,
            executionContext.UserId,
            executionContext.InitiatingUserId);

        SettingService = new SettingsService(OrganizationServiceProvider.Admin);
        DataServiceProvider = new DataServiceProvider(OrganizationServiceProvider);
        _validator = TaskValidator.Create(TaskContext);

        Log = CreateDefaultLog(executionContext);

        InitializeContextData();
    }

    public void Execute()
    {
        var wholeTimer = Stopwatch.StartNew();
        var detailBuilder = new StringBuilder();

        try
        {
            AppendExecutionHeader(detailBuilder);

            Validate(detailBuilder);

            if (Log.Status == TaskStatus.NotValid)
                return;

            ExecuteInternal(detailBuilder);

            Log.Status = TaskStatus.Success;
        }
        catch (DataverseValidationException ex)
        {
            Log.Status = TaskStatus.Success;
            Log.LogSeverity = LogSeverity.Info;
            detailBuilder.AppendLine(ex.ToString());
            throw;
        }
        catch (InvalidPluginExecutionException ex)
        {
            Log.Status = TaskStatus.Error;
            Log.LogSeverity = LogSeverity.Error;
            detailBuilder.AppendLine(ex.ToString());

            if (ex.InnerException != null)
                detailBuilder.AppendLine(ex.InnerException.ToString());

            detailBuilder.AppendLine(ex.StackTrace);
            throw;
        }
        catch (Exception ex)
        {
            Log.Status = TaskStatus.Error;
            Log.LogSeverity = LogSeverity.Error;
            detailBuilder.AppendLine(ex.ToString());
            throw;
        }
        finally
        {
            wholeTimer.Stop();
            Log.ElapsedTimeInMs = wholeTimer.Elapsed.TotalMilliseconds;
            Log.Detail = BuildLogDetail(detailBuilder.ToString(), ExecutionMessage);
        }
    }

    public Log GetTaskLog()
    {
        return Log;
    }

    protected abstract ICompleteValidation AddValidations(IBasicModeValidation validator);

    protected abstract void DoExecute();

    protected virtual IReadOnlyCollection<string> GetMessagesForContextEntity()
    {
        return ["Create", "Update"];
    }

    protected virtual TEntity GetContextEntity(TaskContext taskContext, Log log)
    {
        if (taskContext == null)
            throw new ArgumentNullException(nameof(taskContext));

        if (log == null)
            throw new ArgumentNullException(nameof(log));

        log.LogDetails ??= [];

        return ExecutionContextParameters.GetEntityTarget<TEntity>(taskContext.PluginExecutionContext);
    }

    protected bool HasPreImage(string name = DefaultImageName)
    {
        return HasImage(TaskContext.PluginExecutionContext.PreEntityImages, name);
    }

    protected bool HasPostImage(string name = DefaultImageName)
    {
        return HasImage(TaskContext.PluginExecutionContext.PostEntityImages, name);
    }

    protected TEntity GetPreImage(string name = DefaultImageName, bool throwException = true)
    {
        return GetImage(
            TaskContext?.PluginExecutionContext?.PreEntityImages,
            name,
            throwException,
            "pre");
    }

    protected TEntity GetPostImage(string name = DefaultImageName, bool throwException = true)
    {
        return GetImage(
            TaskContext?.PluginExecutionContext?.PostEntityImages,
            name,
            throwException,
            "post");
    }


    protected void AddLogMessageLine(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (string.IsNullOrEmpty(ExecutionMessage))
            ExecutionMessage = message;
        else
            ExecutionMessage += Environment.NewLine + message;
    }
    protected void AddLogDetail(string name, string detail)
    {
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(detail))
            return;

        Log.AddDetail(name ?? string.Empty, detail ?? string.Empty);
    }

    protected void AddLogDetail(string name, object detail)
    {
        if (string.IsNullOrWhiteSpace(name) && detail == null)
            return;

        Log.AddDetail(name ?? string.Empty, detail);
    }

    protected string GetTaskName()
    {
        return GetType().Name;
    }

    protected virtual bool ShouldInitializeContextEntity()
    {
        var messages = GetMessagesForContextEntity();
        return messages != null && messages.Contains(TaskContext.Message);
    }

    private void InitializeContextData()
    {
        if (!ShouldInitializeContextEntity())
            return;

        ContextEntity = GetContextEntity(TaskContext, Log);

        if (HasPreImage())
            PreImage = GetPreImage();

        if (HasPostImage())
            PostImage = GetPostImage();
    }

    private void Validate(StringBuilder message)
    {
        var timer = Stopwatch.StartNew();

        var validatorExecute = (IExecuteValidation)AddValidations(_validator);

        bool isValid;
        try
        {
            isValid = validatorExecute.IsValid();
        }
        catch
        {
            message.AppendLine("#Validate Error");

            var validatorNumber = 1;
            foreach (var validationMessage in validatorExecute.GetValidationMessages())
            {
                message.AppendLine(validatorNumber + ". " + (validationMessage ?? string.Empty).TrimEnd('_'));
                validatorNumber++;
            }

            throw;
        }

        if (!isValid)
        {
            foreach (var validationMessage in validatorExecute
                .GetValidationMessages()
                .Where(x => x != null && !x.EndsWith("OK_")))
            {
                message.AppendLine(validationMessage);
            }

            Log.Status = TaskStatus.NotValid;
            return;
        }

        timer.Stop();
        message.AppendLine("#Validation: " + timer.Elapsed.TotalMilliseconds + " ms");
    }

    private void ExecuteInternal(StringBuilder message)
    {
        if (Log.Status == TaskStatus.NotValid)
            return;

        var timer = Stopwatch.StartNew();

        DoExecute();
        timer.Stop();
        message.AppendLine("#Execution: " + timer.Elapsed.TotalMilliseconds + " ms");
    }

    private void AppendExecutionHeader(StringBuilder message)
    {
        message.AppendLine($"Framework: {FrameworkConstants.FrameworkVersion} | Plugin: {TaskContext.Version} | Task: {TaskContext.TaskOrder}/{TaskContext.CountOfTasks}");
    }

    private Log CreateDefaultLog(IPluginExecutionContext executionContext)
    {
        var log = new Log(LogSeverity.Debug, new LogExecutionContext(executionContext), "Task")
        {
            TaskName = GetTaskName(),
            StartUtc = DateTime.UtcNow,
        };

        if (!string.IsNullOrWhiteSpace(TaskContext.UnsecureConfig))
        {
            log.AddDetail("UnsecureConfig", TaskContext.UnsecureConfig);
        }

        if (!string.IsNullOrWhiteSpace(TaskContext.SecureConfig))
        {
            log.AddDetail("SecureConfig", "Secure configuration is registered, but its value will not be displayed for security reasons.");
        }

        return log;
    }

    private static TService GetRequiredService<TService>(IServiceProvider serviceProvider)
        where TService : class
    {
        var service = serviceProvider.GetService(typeof(TService)) as TService ?? throw new ArgumentNullException(typeof(TService).Name);
        return service;
    }

    private static bool HasImage(EntityImageCollection images, string name)
    {
        return images != null && images.ContainsKey(name);
    }

    private static TEntity GetImage(EntityImageCollection images, string name, bool throwException, string imageType)
    {
        if (!HasImage(images, name))
        {
            if (throwException)
                throw new InvalidPluginExecutionException(
                    string.Format("No {0} image named '{1}' is registered on the plugin step.", imageType, name));

            return null;
        }

        var image = images[name];
        return image?.ToEntity<TEntity>();
    }

    private static string BuildLogDetail(string message, string executionMessage)
    {
        if (string.IsNullOrWhiteSpace(executionMessage))
            return message;

        return message
               + Environment.NewLine
               + "###Execution Message####"
               + Environment.NewLine
               + executionMessage;
    }
}