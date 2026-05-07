using Microsoft.Xrm.Sdk;
using System.Linq.Expressions;

namespace Pillaro.Dataverse.PluginFramework.PluginRegistrations;

public interface IPluginRegistration
{
    IPluginStepStageBuilder OnCreate<TEntity>(string stepId)
        where TEntity : Entity;

    IPluginUpdateStepStageBuilder<TEntity> OnUpdate<TEntity>(string stepId)
        where TEntity : Entity;

    IPluginStepStageBuilder OnDelete<TEntity>(string stepId)
        where TEntity : Entity;

    IPluginStepStageBuilder OnMessage(string stepId, string messageName);

    IPluginStepStageBuilder OnMessage<TEntity>(string stepId, string messageName)
        where TEntity : Entity;
}

public interface IPluginStepStageBuilder
{
    IPluginStepModeBuilder PreValidation();

    IPluginStepModeBuilder PreOperation();

    IPluginStepModeBuilder PostOperation();
}

public interface IPluginUpdateStepStageBuilder<TEntity>
    where TEntity : Entity
{
    IPluginUpdateStepModeBuilder<TEntity> PreValidation();

    IPluginUpdateStepModeBuilder<TEntity> PreOperation();

    IPluginUpdateStepModeBuilder<TEntity> PostOperation();
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

public interface IPluginStepBuilder
{
    IPluginStepBuilder Rank(int rank);

    IPluginStepBuilder InSolution(string solutionName);

    IPluginStepBuilder WithPreImage(string imageId, string name, params string[] attributes);

    IPluginStepBuilder WithPostImage(string imageId, string name, params string[] attributes);

    IPluginStepBuilder RequiresConfirmation(PluginRisk risk, string reason, PluginDeploymentScope scope = PluginDeploymentScope.All);
}

public interface IPluginUpdateStepBuilder<TEntity> : IPluginStepBuilder
    where TEntity : Entity
{
    new IPluginUpdateStepBuilder<TEntity> Rank(int rank);

    new IPluginUpdateStepBuilder<TEntity> InSolution(string solutionName);

    IPluginUpdateStepBuilder<TEntity> WhenChanged(params string[] attributes);

    IPluginUpdateStepBuilder<TEntity> WhenChanged(params Expression<Func<TEntity, object>>[] attributes);

    new IPluginUpdateStepBuilder<TEntity> WithPreImage(string imageId, string name, params string[] attributes);

    IPluginUpdateStepBuilder<TEntity> WithPreImage(string imageId, string name, params Expression<Func<TEntity, object>>[] attributes);

    new IPluginUpdateStepBuilder<TEntity> WithPostImage(string imageId, string name, params string[] attributes);

    IPluginUpdateStepBuilder<TEntity> WithPostImage(string imageId, string name, params Expression<Func<TEntity, object>>[] attributes);

    new IPluginUpdateStepBuilder<TEntity> RequiresConfirmation(PluginRisk risk, string reason, PluginDeploymentScope scope = PluginDeploymentScope.All);
}
