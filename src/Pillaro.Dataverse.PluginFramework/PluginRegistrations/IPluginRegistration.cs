using Microsoft.Xrm.Sdk;
using System.Linq.Expressions;

namespace Pillaro.Dataverse.PluginFramework.PluginRegistrations;

public interface IPluginRegistration
{
    IPluginStepStageBuilder OnCreate<TEntity>(string stepId)
        where TEntity : Entity;

    IPluginStepStageBuilder OnCreate(string entityLogicalName, string stepId);

    IPluginUpdateStepStageBuilder<TEntity> OnUpdate<TEntity>(string stepId)
        where TEntity : Entity;

    IPluginUpdateStepStageBuilder OnUpdate(string entityLogicalName, string stepId);

    IPluginStepStageBuilder OnDelete<TEntity>(string stepId)
        where TEntity : Entity;

    IPluginStepStageBuilder OnDelete(string entityLogicalName, string stepId);

    IPluginStepStageBuilder OnMessage(string stepId, string messageName);

    IPluginStepStageBuilder OnMessage<TEntity>(string stepId, string messageName)
        where TEntity : Entity;

    IPluginStepStageBuilder OnMessage(string entityLogicalName, string stepId, string messageName);
}

public interface IPluginStepStageBuilder
{
    IPluginStepModeBuilder PreValidation();

    IPluginStepModeBuilder PreOperation();

    IPluginStepModeBuilder MainOperation();

    IPluginStepModeBuilder PostOperation();
}

public interface IPluginUpdateStepStageBuilder<TEntity>
    where TEntity : Entity
{
    IPluginUpdateStepModeBuilder<TEntity> PreValidation();

    IPluginUpdateStepModeBuilder<TEntity> PreOperation();

    IPluginUpdateStepModeBuilder<TEntity> MainOperation();

    IPluginUpdateStepModeBuilder<TEntity> PostOperation();
}

public interface IPluginUpdateStepStageBuilder
{
    IPluginUpdateStepModeBuilder PreValidation();

    IPluginUpdateStepModeBuilder PreOperation();

    IPluginUpdateStepModeBuilder MainOperation();

    IPluginUpdateStepModeBuilder PostOperation();
}

public interface IPluginStepModeBuilder
{
    IPluginStepBuilder Synchronous();

    IPluginStepBuilder Asynchronous();
}

public interface IPluginUpdateStepModeBuilder<TEntity>
    where TEntity : Entity
{
    IPluginUpdateStepBuilder<TEntity> Synchronous();

    IPluginUpdateStepBuilder<TEntity> Asynchronous();
}

public interface IPluginUpdateStepModeBuilder
{
    IPluginUpdateStepBuilder Synchronous();

    IPluginUpdateStepBuilder Asynchronous();
}

public interface IPluginStepBuilder
{
    IPluginStepBuilder Rank(int rank);

    IPluginStepBuilder WithName(string name);

    IPluginStepBuilder WithFilteringAttributes(params string[] attributes);

    IPluginStepBuilder WithUnsecureConfiguration(string unsecureConfiguration);

    IPluginStepBuilder WithPreImage(string imageId, string name, params string[] attributes);

    IPluginStepBuilder WithPostImage(string imageId, string name, params string[] attributes);
}

public interface IPluginUpdateStepBuilder<TEntity> : IPluginStepBuilder
    where TEntity : Entity
{
    new IPluginUpdateStepBuilder<TEntity> Rank(int rank);

    new IPluginUpdateStepBuilder<TEntity> WithName(string name);

    new IPluginUpdateStepBuilder<TEntity> WithFilteringAttributes(params string[] attributes);

    new IPluginUpdateStepBuilder<TEntity> WithUnsecureConfiguration(string unsecureConfiguration);

    IPluginUpdateStepBuilder<TEntity> WhenChanged(params string[] attributes);

    IPluginUpdateStepBuilder<TEntity> WhenChanged(params Expression<Func<TEntity, object>>[] attributes);

    new IPluginUpdateStepBuilder<TEntity> WithPreImage(string imageId, string name, params string[] attributes);

    IPluginUpdateStepBuilder<TEntity> WithPreImage(string imageId, string name, params Expression<Func<TEntity, object>>[] attributes);

    new IPluginUpdateStepBuilder<TEntity> WithPostImage(string imageId, string name, params string[] attributes);

    IPluginUpdateStepBuilder<TEntity> WithPostImage(string imageId, string name, params Expression<Func<TEntity, object>>[] attributes);

}

public interface IPluginUpdateStepBuilder : IPluginStepBuilder
{
    new IPluginUpdateStepBuilder Rank(int rank);

    new IPluginUpdateStepBuilder WithName(string name);

    new IPluginUpdateStepBuilder WithFilteringAttributes(params string[] attributes);

    new IPluginUpdateStepBuilder WithUnsecureConfiguration(string unsecureConfiguration);

    IPluginUpdateStepBuilder WhenChanged(params string[] attributes);

    new IPluginUpdateStepBuilder WithPreImage(string imageId, string name, params string[] attributes);

    new IPluginUpdateStepBuilder WithPostImage(string imageId, string name, params string[] attributes);
}
