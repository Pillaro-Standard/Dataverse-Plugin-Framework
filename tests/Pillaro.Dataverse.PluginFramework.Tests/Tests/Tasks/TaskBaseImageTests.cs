using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.PluginRegistrations;
using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Tasks;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using TaskStatus = Pillaro.Dataverse.PluginFramework.Logging.Enums.TaskStatus;

namespace Pillaro.Dataverse.PluginFramework.Tests.Tests.Tasks;

public class TaskBaseImageTests
{
    private const string QuoteDetail = "quotedetail";

    [Fact]
    public void DeleteStepWithPreImage_PopulatesPreImage()
    {
        var context = CreateDeleteContext();
        context.PreEntityImages["image"] = CreateImage();

        var task = CreateTask<ImageReadingTask>(context);

        task.Execute();

        Assert.Equal(TaskStatus.Success, task.GetTaskLog().Status);
        Assert.NotNull(task.ReadPreImage);
        Assert.Equal("quote", task.ReadPreImage.GetAttributeValue<EntityReference>("quoteid").LogicalName);
    }

    [Fact]
    public void DeleteStepWithPostImage_PopulatesPostImage()
    {
        var context = CreateDeleteContext();
        context.PostEntityImages["image"] = CreateImage();

        var task = CreateTask<PostImageReadingTask>(context);

        task.Execute();

        Assert.Equal(TaskStatus.Success, task.GetTaskLog().Status);
        Assert.NotNull(task.ReadPostImage);
    }

    [Fact]
    public void DeleteStep_DoesNotInitializeContextEntity()
    {
        var context = CreateDeleteContext();
        context.PreEntityImages["image"] = CreateImage();

        var task = CreateTask<ImageReadingTask>(context);

        task.Execute();

        // Delete carries an EntityReference target, so there is no context entity to initialize.
        Assert.Null(task.ReadContextEntity);
    }

    [Fact]
    public void UpdateStep_StillInitializesContextEntityAndImages()
    {
        var context = CreateContext(DataverseMessages.Update);
        context.InputParameters["Target"] = new Entity(QuoteDetail, context.PrimaryEntityId);
        context.PreEntityImages["image"] = CreateImage();

        var task = CreateTask<ImageReadingTask>(context);

        task.Execute();

        Assert.Equal(TaskStatus.Success, task.GetTaskLog().Status);
        Assert.NotNull(task.ReadContextEntity);
        Assert.NotNull(task.ReadPreImage);
    }

    [Fact]
    public void CustomImageName_IsUsedWhenTaskOverridesImageName()
    {
        var context = CreateDeleteContext();
        context.PreEntityImages["deleted"] = CreateImage();

        var task = CreateTask<CustomImageNameTask>(context);

        task.Execute();

        Assert.Equal(TaskStatus.Success, task.GetTaskLog().Status);
        Assert.NotNull(task.ReadPreImage);
    }

    [Fact]
    public void MissingPreImage_FailsValidation()
    {
        var context = CreateDeleteContext();

        var task = CreateTask<ImageReadingTask>(context);

        task.Execute();

        Assert.Equal(TaskStatus.NotValid, task.GetTaskLog().Status);
        Assert.Contains("does not contains preimage with name image", task.GetTaskLog().Detail);
    }

    [Fact]
    public void RegisteredPreImageWithoutData_FailsValidation()
    {
        var context = CreateDeleteContext();
        context.PreEntityImages["image"] = null;

        var task = CreateTask<ImageReadingTask>(context);

        task.Execute();

        Assert.Equal(TaskStatus.NotValid, task.GetTaskLog().Status);
        Assert.Contains("does not contain any data", task.GetTaskLog().Detail);
    }

    private static Entity CreateImage()
    {
        var image = new Entity(QuoteDetail, Guid.NewGuid());
        image["quoteid"] = new EntityReference("quote", Guid.NewGuid());
        return image;
    }

    private static FakePluginExecutionContext CreateDeleteContext()
    {
        var context = CreateContext(DataverseMessages.Delete);
        context.InputParameters["Target"] = new EntityReference(QuoteDetail, context.PrimaryEntityId);
        return context;
    }

    private static FakePluginExecutionContext CreateContext(string message)
    {
        return new FakePluginExecutionContext
        {
            MessageName = message,
            PrimaryEntityName = QuoteDetail,
            PrimaryEntityId = Guid.NewGuid(),
            Stage = (int)PluginStage.Postoperation,
            Mode = (int)PluginMode.Synchronous,
            UserId = Guid.NewGuid(),
            InitiatingUserId = Guid.NewGuid(),
        };
    }

    private static TTask CreateTask<TTask>(FakePluginExecutionContext context)
        where TTask : ITask
    {
        var serviceProvider = new FakeServiceProvider(context);
        var taskContext = new TaskContext(null, null, context);

        return (TTask)Activator.CreateInstance(typeof(TTask), serviceProvider, taskContext)!;
    }

    private class ImageReadingTask : TaskBase<Entity>
    {
        public ImageReadingTask(IServiceProvider serviceProvider, TaskContext taskContext)
            : base(serviceProvider, taskContext)
        {
        }

        public Entity? ReadPreImage { get; private set; }

        public Entity? ReadContextEntity { get; private set; }

        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Postoperation)
                .WithMessages(DataverseMessages.Delete, DataverseMessages.Update)
                .ForEntity(QuoteDetail)
                .HasPreImage();
        }

        protected override void DoExecute()
        {
            ReadPreImage = PreImage;
            ReadContextEntity = ContextEntity;
        }
    }

    private class PostImageReadingTask : TaskBase<Entity>
    {
        public PostImageReadingTask(IServiceProvider serviceProvider, TaskContext taskContext)
            : base(serviceProvider, taskContext)
        {
        }

        public Entity? ReadPostImage { get; private set; }

        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Postoperation)
                .WithMessage(DataverseMessages.Delete)
                .ForEntity(QuoteDetail)
                .HasPostImage();
        }

        protected override void DoExecute()
        {
            ReadPostImage = PostImage;
        }
    }

    private class CustomImageNameTask : TaskBase<Entity>
    {
        public CustomImageNameTask(IServiceProvider serviceProvider, TaskContext taskContext)
            : base(serviceProvider, taskContext)
        {
        }

        public Entity? ReadPreImage { get; private set; }

        protected override string GetPreImageName()
        {
            return "deleted";
        }

        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(PluginStage.Postoperation)
                .WithMessage(DataverseMessages.Delete)
                .ForEntity(QuoteDetail)
                .HasPreImage("deleted");
        }

        protected override void DoExecute()
        {
            ReadPreImage = PreImage;
        }
    }

    private class FakeServiceProvider : IServiceProvider
    {
        private readonly IPluginExecutionContext _pluginExecutionContext;
        private readonly IOrganizationServiceFactory _organizationServiceFactory = new FakeOrganizationServiceFactory();
        private readonly ITracingService _tracingService = new FakeTracingService();

        public FakeServiceProvider(IPluginExecutionContext pluginExecutionContext)
        {
            _pluginExecutionContext = pluginExecutionContext;
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IPluginExecutionContext))
                return _pluginExecutionContext;

            if (serviceType == typeof(IOrganizationServiceFactory))
                return _organizationServiceFactory;

            if (serviceType == typeof(ITracingService))
                return _tracingService;

            return null;
        }
    }

    private class FakeOrganizationServiceFactory : IOrganizationServiceFactory
    {
        public IOrganizationService CreateOrganizationService(Guid? userId)
        {
            return new FakeOrganizationService();
        }
    }

    private class FakeTracingService : ITracingService
    {
        public void Trace(string format, params object[] args)
        {
        }
    }

    private class FakeOrganizationService : IOrganizationService
    {
        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
            => throw new NotSupportedException();

        public Guid Create(Entity entity) => throw new NotSupportedException();

        public void Delete(string entityName, Guid id) => throw new NotSupportedException();

        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
            => throw new NotSupportedException();

        public OrganizationResponse Execute(OrganizationRequest request) => throw new NotSupportedException();

        public Entity Retrieve(string entityName, Guid id, Microsoft.Xrm.Sdk.Query.ColumnSet columnSet)
            => throw new NotSupportedException();

        public EntityCollection RetrieveMultiple(Microsoft.Xrm.Sdk.Query.QueryBase query)
            => throw new NotSupportedException();

        public void Update(Entity entity) => throw new NotSupportedException();
    }

    private class FakePluginExecutionContext : IPluginExecutionContext
    {
        public int Stage { get; set; }

        public IPluginExecutionContext? ParentContext { get; set; }

        public int Mode { get; set; }

        public int IsolationMode { get; set; }

        public int Depth { get; set; } = 1;

        public string? MessageName { get; set; }

        public string? PrimaryEntityName { get; set; }

        public Guid? RequestId { get; set; }

        public string? SecondaryEntityName { get; set; }

        public ParameterCollection InputParameters { get; set; } = [];

        public ParameterCollection OutputParameters { get; set; } = [];

        public ParameterCollection SharedVariables { get; set; } = [];

        public Guid UserId { get; set; }

        public Guid InitiatingUserId { get; set; }

        public Guid BusinessUnitId { get; set; }

        public Guid OrganizationId { get; set; }

        public string? OrganizationName { get; set; }

        public Guid PrimaryEntityId { get; set; }

        public EntityImageCollection PreEntityImages { get; set; } = [];

        public EntityImageCollection PostEntityImages { get; set; } = [];

        public EntityReference? OwningExtension { get; set; }

        public Guid CorrelationId { get; set; } = Guid.NewGuid();

        public bool IsExecutingOffline { get; set; }

        public bool IsOfflinePlayback { get; set; }

        public bool IsInTransaction { get; set; }

        public Guid OperationId { get; set; }

        public DateTime OperationCreatedOn { get; set; } = DateTime.UtcNow;
    }
}
