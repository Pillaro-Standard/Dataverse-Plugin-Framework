using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Plugins;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.FluentInterfaces;
using Pillaro.Dataverse.PluginFramework.Tasks.Validation.Validators;
using System;

namespace Pillaro.Dataverse.PluginFramework.Tasks.Validation;

internal class TaskValidatorBase : IBasicModeValidation, IBasicStageValidation,
    IBasicMessageValidation, IBasicPrimaryEntityValidation
{

    protected readonly IValidatorProcessor ValidatorProcessor;

    protected TaskValidatorBase(TaskContext taskContext)
    {
        ValidatorProcessor = new ValidatorProcessor(taskContext);
    }

    public IBasicStageValidation WithMode(PluginMode mode)
    {
        ValidatorProcessor.AddValidatorHandler(new ModeValidator(mode));
        return this;
    }

    public IBasicMessageValidation WithStage(PluginStage stage)
    {
        ValidatorProcessor.AddValidatorHandler(new StageValidator(stage));
        return this;
    }

    public IBasicImageValidation ForEntities(params string[] entityNames)
    {

        ValidatorProcessor.AddValidatorHandler(new EntityValidator(entityNames));
        return this;
    }

    public IBasicImageValidation ForEntity(string entityName)
    {
        ValidatorProcessor.AddValidatorHandler(new EntityValidator([entityName]));
        return this;
    }

    public IBasicPrimaryEntityValidation WithMessage(string message)
    {
        ValidatorProcessor.AddValidatorHandler(new MessageValidator([message]));
        return this;
    }

    public IBasicPrimaryEntityValidation WithMessages(params string[] messages)
    {
        ValidatorProcessor.AddValidatorHandler(new MessageValidator(messages));
        return this;
    }

    public IBasicImageValidation HasPreImage(string imageName = "image")
    {
        ValidatorProcessor.AddValidatorHandler(new ImageValidator(imageName, true));
        return this;
    }

    public IBasicImageValidation HasPreImageWhen(Func<TaskContext, bool> predicate, string imageName = "image")
    {
        ValidatorProcessor.AddValidatorHandler(new ImageWithConditionValidator(predicate, true, imageName));
        return this;
    }

    public IBasicImageValidation HasPostImage(string imageName = "image")
    {
        ValidatorProcessor.AddValidatorHandler(new ImageValidator(imageName, false));
        return this;
    }

    public IBasicImageValidation HasPostImageWhen(Func<TaskContext, bool> predicate, string imageName = "image")
    {
        ValidatorProcessor.AddValidatorHandler(new ImageWithConditionValidator(predicate, false, imageName));
        return this;
    }

    public IBasicAttributeValidation EntityWithAtLeastOneAttribute(Entity entity, params string[] attributeNames)
    {
        ValidatorProcessor.AddValidatorHandler(new EntityAttributesValidator(entity, attributeNames, false));
        return this;
    }

    public IBasicAttributeValidation EntityWithAtLeastOneAttributeWhen(Func<TaskContext, bool> predicate, Entity entity, params string[] attributeNames)
    {
        ValidatorProcessor.AddValidatorHandler(new EntityAttributesWithConditionValidator(attributeNames, entity, false, predicate));
        return this;
    }

    public IBasicAttributeValidation EntityWithAllAttributes(Entity entity, params string[] attributeNames)
    {
        ValidatorProcessor.AddValidatorHandler(new EntityAttributesValidator(entity, attributeNames, true));
        return this;
    }

    public IBasicAttributeValidation EntityWithAllAttributesWhen(Func<TaskContext, bool> predicate, Entity entity, params string[] attributeNames)
    {
        ValidatorProcessor.AddValidatorHandler(new EntityAttributesWithConditionValidator(attributeNames, entity, true, predicate));
        return this;
    }

    public ICustomValidation WithValidation(string message, Func<TaskContext, bool> predicate)
    {
        ValidatorProcessor.AddValidatorHandler(new CustomValidator(message, predicate));
        return this;
    }

    public ICustomValidation WithValidation(Lazy<string> message, Func<TaskContext, bool> predicate)
    {
        ValidatorProcessor.AddValidatorHandler(new CustomValidator(message, predicate));
        return this;
    }

    public IBreakValidation WithBreakValidation(string message, Func<TaskContext, bool> predicate)
    {
        ValidatorProcessor.AddValidatorHandler(new CustomBreakValidator(message, predicate));
        return this;
    }

    public IBreakValidation WithBreakValidation(Lazy<string> message, Func<TaskContext, bool> predicate)
    {
        ValidatorProcessor.AddValidatorHandler(new CustomBreakValidator(message, predicate));
        return this;
    }

    public IBreakValidation ThrowWithError(string message, Func<TaskContext, bool> predicate)
    {
        ValidatorProcessor.AddValidatorHandler(new ThrowExceptionValidator(message, predicate));
        return this;
    }

    public IBreakValidation ThrowWithError(Lazy<string> message, Func<TaskContext, bool> predicate)
    {
        ValidatorProcessor.AddValidatorHandler(new ThrowExceptionValidator(message, predicate));
        return this;
    }

    public IBreakValidation ThrowWithWarning(string message, Func<TaskContext, bool> predicate)
    {
        ValidatorProcessor.AddValidatorHandler(new ThrowExceptionValidator(message, predicate, true));
        return this;
    }

    public IBreakValidation ThrowWithWarning(Lazy<string> message, Func<TaskContext, bool> predicate)
    {
        ValidatorProcessor.AddValidatorHandler(new ThrowExceptionValidator(message, predicate, true));
        return this;
    }
}
