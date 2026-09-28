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
}
