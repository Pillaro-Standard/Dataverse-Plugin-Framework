using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.PluginRegistrations;
using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Tasks;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;

namespace Pillaro.Dataverse.PluginFramework.Tests.Tests.Tasks;

public class PluginBaseQueuedUpdateTests
{
    private const string QuoteDetail = "quotedetail";

    [Fact]
    public void PostOperation_WritesEntityQueuedByTwoTasksOnce()
    {
        var context = CreateContext(PluginStage.Postoperation);
        var serviceProvider = new FakeServiceProvider(context);

        new QueueingPlugin(PluginStage.Postoperation).Execute(serviceProvider);

        var update = Assert.Single(serviceProvider.OrganizationService.UpdatedEntities);
        Assert.Equal(QuoteDetail, update.LogicalName);
        Assert.Equal(context.PrimaryEntityId, update.Id);
        Assert.Equal(new Money(210M), update["tax"]);
        Assert.Equal("recalculated", update["note"]);
    }

    [Fact]
    public void PostOperation_QueuedValuesAreVisibleToLaterTasks()
    {
        var context = CreateContext(PluginStage.Postoperation);
        var serviceProvider = new FakeServiceProvider(context);

        new QueueingPlugin(PluginStage.Postoperation).Execute(serviceProvider);

        // The second task reads what the first one queued and derives its own value from it.
        var update = Assert.Single(serviceProvider.OrganizationService.UpdatedEntities);
        Assert.Equal(new Money(220M), update["taxwithrounding"]);
    }

    [Fact]
    public void PreOperation_MergesQueuedValuesIntoTargetInsteadOfWriting()
    {
        var context = CreateContext(PluginStage.Preoperation);
        var target = GetTarget(context);

        var serviceProvider = new FakeServiceProvider(context);

        new QueueingPlugin(PluginStage.Preoperation).Execute(serviceProvider);

        Assert.Empty(serviceProvider.OrganizationService.UpdatedEntities);
        Assert.Equal(new Money(210M), target["tax"]);
        Assert.Equal("recalculated", target["note"]);
    }

    [Fact]
    public void PreOperation_WritesQueuedValuesForAnotherRecord()
    {
        var context = CreateContext(PluginStage.Preoperation);

        var serviceProvider = new FakeServiceProvider(context);
        var otherRecord = Guid.NewGuid();

        new OtherRecordPlugin(otherRecord).Execute(serviceProvider);

        var update = Assert.Single(serviceProvider.OrganizationService.UpdatedEntities);
        Assert.Equal("quote", update.LogicalName);
        Assert.Equal(otherRecord, update.Id);
    }

    [Fact]
    public void FailingTask_LeavesQueuedEntitiesUnwritten()
    {
        var context = CreateContext(PluginStage.Postoperation);
        var serviceProvider = new FakeServiceProvider(context);

        Assert.Throws<InvalidPluginExecutionException>(() => new FailingPlugin().Execute(serviceProvider));

        Assert.Empty(serviceProvider.OrganizationService.UpdatedEntities);
    }

    [Fact]
    public void TaskWithoutQueuedEntities_DoesNotWriteAnything()
    {
        var context = CreateContext(PluginStage.Postoperation);
        var serviceProvider = new FakeServiceProvider(context);

        new NoQueuePlugin().Execute(serviceProvider);

        Assert.Empty(serviceProvider.OrganizationService.UpdatedEntities);
    }

    private static FakePluginExecutionContext CreateContext(PluginStage stage)
    {
        var context = new FakePluginExecutionContext
        {
            MessageName = DataverseMessages.Update,
            PrimaryEntityName = QuoteDetail,
            PrimaryEntityId = Guid.NewGuid(),
            Stage = (int)stage,
            Mode = (int)PluginMode.Synchronous,
            UserId = Guid.NewGuid(),
            InitiatingUserId = Guid.NewGuid(),
        };

        context.InputParameters["Target"] = new Entity(QuoteDetail, context.PrimaryEntityId);

        return context;
    }

    private static Entity GetTarget(FakePluginExecutionContext context)
    {
        return (Entity)context.InputParameters["Target"];
    }

    private class QueueingPlugin : PluginBase
    {
        public QueueingPlugin(PluginStage stage) : base(null, null)
        {
            RegisterTask<QueueTaxTask>(stage, DataverseMessages.Update, QuoteDetail, PluginMode.Synchronous);
            RegisterTask<QueueNoteTask>(stage, DataverseMessages.Update, QuoteDetail, PluginMode.Synchronous);
        }
    }

    private class OtherRecordPlugin : PluginBase
    {
        internal static Guid RecordId;

        public OtherRecordPlugin(Guid recordId) : base(null, null)
        {
            RecordId = recordId;
            RegisterTask<QueueParentTask>(PluginStage.Preoperation, DataverseMessages.Update, QuoteDetail, PluginMode.Synchronous);
        }
    }

    private class FailingPlugin : PluginBase
    {
        public FailingPlugin() : base(null, null)
        {
            RegisterTask<QueueTaxTask>(PluginStage.Postoperation, DataverseMessages.Update, QuoteDetail, PluginMode.Synchronous);
            RegisterTask<ThrowingTask>(PluginStage.Postoperation, DataverseMessages.Update, QuoteDetail, PluginMode.Synchronous);
        }
    }

    private class NoQueuePlugin : PluginBase
    {
        public NoQueuePlugin() : base(null, null)
        {
            RegisterTask<NoQueueTask>(PluginStage.Postoperation, DataverseMessages.Update, QuoteDetail, PluginMode.Synchronous);
        }
    }

    private abstract class QueueTaskBase : TaskBase<Entity>
    {
        protected QueueTaskBase(IServiceProvider serviceProvider, TaskContext taskContext)
            : base(serviceProvider, taskContext)
        {
        }

        protected override ICompleteValidation AddValidations(IBasicModeValidation validator)
        {
            return validator
                .WithMode(PluginMode.Synchronous)
                .WithStage(TaskContext.Stage)
                .WithMessage(DataverseMessages.Update)
                .ForEntity(QuoteDetail);
        }
    }

    private class QueueTaxTask : QueueTaskBase
    {
        public QueueTaxTask(IServiceProvider serviceProvider, TaskContext taskContext)
            : base(serviceProvider, taskContext)
        {
        }

        protected override void DoExecute()
        {
            var update = new Entity(ContextEntityReference.LogicalName) { Id = ContextEntityReference.Id };
            update["tax"] = new Money(210M);

            TaskContext.AddEntityToUpdate(update);
        }
    }

    private class QueueNoteTask : QueueTaskBase
    {
        public QueueNoteTask(IServiceProvider serviceProvider, TaskContext taskContext)
            : base(serviceProvider, taskContext)
        {
        }

        protected override void DoExecute()
        {
            var queued = TaskContext.GetActualEntityToUpdate(
                ContextEntityReference.LogicalName,
                ContextEntityReference.Id);

            var tax = queued.GetAttributeValue<Money>("tax");

            var update = new Entity(ContextEntityReference.LogicalName) { Id = ContextEntityReference.Id };
            update["note"] = "recalculated";
            update["taxwithrounding"] = new Money(tax.Value + 10M);

            TaskContext.AddEntityToUpdate(update);
        }
    }

    private class QueueParentTask : QueueTaskBase
    {
        public QueueParentTask(IServiceProvider serviceProvider, TaskContext taskContext)
            : base(serviceProvider, taskContext)
        {
        }

        protected override void DoExecute()
        {
            var update = new Entity("quote") { Id = OtherRecordPlugin.RecordId };
            update["totaltax"] = new Money(210M);

            TaskContext.AddEntityToUpdate(update);
        }
    }

    private class ThrowingTask : QueueTaskBase
    {
        public ThrowingTask(IServiceProvider serviceProvider, TaskContext taskContext)
            : base(serviceProvider, taskContext)
        {
        }

        protected override void DoExecute()
        {
            throw new InvalidOperationException("task failed");
        }
    }

    private class NoQueueTask : QueueTaskBase
    {
        public NoQueueTask(IServiceProvider serviceProvider, TaskContext taskContext)
            : base(serviceProvider, taskContext)
        {
        }

        protected override void DoExecute()
        {
        }
    }
}
