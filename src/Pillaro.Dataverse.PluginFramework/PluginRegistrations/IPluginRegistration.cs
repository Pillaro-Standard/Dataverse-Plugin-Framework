using Microsoft.Xrm.Sdk;

namespace Pillaro.Dataverse.PluginFramework.PluginRegistrations;

public interface IPluginRegistration
{
    IPluginStepStageBuilder OnCreate<TEntity>(string stepId)
        where TEntity : Entity;

    IPluginUpdateStepStageBuilder OnUpdate<TEntity>(string stepId)
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

public interface IPluginUpdateStepStageBuilder : IPluginStepStageBuilder
{
}

public interface IPluginStepModeBuilder
{
    IPluginStepBuilder Synchronous();

    IPluginStepBuilder Asynchronous();
}

public interface IPluginStepBuilder
{
    IPluginStepBuilder Rank(int rank);

    IPluginStepBuilder WithPreImage(string imageId, string name, params string[] attributes);

    IPluginStepBuilder WithPostImage(string imageId, string name, params string[] attributes);

    IPluginStepBuilder RequiresConfirmation(PluginRisk risk, string reason, PluginDeploymentScope scope = PluginDeploymentScope.All);
}

public interface IPluginUpdateStepBuilder : IPluginStepBuilder
{
    IPluginUpdateStepBuilder WhenChanged(params string[] attributes);
}
