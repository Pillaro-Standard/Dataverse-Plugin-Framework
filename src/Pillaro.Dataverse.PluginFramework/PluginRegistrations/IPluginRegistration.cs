using Microsoft.Xrm.Sdk;
using System.Linq.Expressions;

namespace Pillaro.Dataverse.PluginFramework.PluginRegistrations;

public interface IPluginRegistration
{
    IPluginStepStageBuilder<TEntity> OnCreate<TEntity>(string stepId)
        where TEntity : Entity;

    IPluginStepStageBuilder OnCreate(string entityLogicalName, string stepId);

    IPluginUpdateStepStageBuilder<TEntity> OnUpdate<TEntity>(string stepId)
        where TEntity : Entity;

    IPluginUpdateStepStageBuilder OnUpdate(string entityLogicalName, string stepId);

    IPluginStepStageBuilder<TEntity> OnDelete<TEntity>(string stepId)
        where TEntity : Entity;

    IPluginStepStageBuilder OnDelete(string entityLogicalName, string stepId);

    IPluginStepStageBuilder OnMessage(string stepId, string messageName);

    IPluginStepStageBuilder<TEntity> OnMessage<TEntity>(string stepId, string messageName)
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

public interface IPluginStepModeBuilder
{
    IPluginStepBuilder Synchronous();

    IPluginStepBuilder Asynchronous();
}

public interface IPluginStepBuilder
{
    IPluginStepBuilder Rank(int rank);

    IPluginStepBuilder WithName(string name);

    IPluginStepBuilder WithFilteringAttributes(params string[] attributes);

    /// <summary>
    /// Alias of <see cref="WithFilteringAttributes(string[])"/>. Dataverse stores filtering attributes on
    /// <c>sdkmessageprocessingstep.filteringattributes</c> regardless of the message, so this is available
    /// for every message, not only <c>Update</c>.
    /// </summary>
    IPluginStepBuilder WhenChanged(params string[] attributes);

    IPluginStepBuilder WithUnsecureConfiguration(string unsecureConfiguration);

    IPluginStepBuilder WithPreImage(string imageId, string name, params string[] attributes);

    IPluginStepBuilder WithPostImage(string imageId, string name, params string[] attributes);

    /// <summary>
    /// Registers a single image with Dataverse image type <c>Both</c> (value 2), which is exposed to the
    /// plugin through both <c>PreEntityImages</c> and <c>PostEntityImages</c>.
    /// </summary>
    IPluginStepBuilder WithBothImage(string imageId, string name, params string[] attributes);

    IPluginStepBuilder WithImage(PluginImageOptions image);
}

/// <summary>
/// Entity-typed stage builder. Returned by every entity-typed registration entry point so that typed
/// filtering attributes and typed images are available for all messages, not only <c>Update</c>.
/// </summary>
public interface IPluginStepStageBuilder<TEntity> : IPluginStepStageBuilder
    where TEntity : Entity
{
    new IPluginStepModeBuilder<TEntity> PreValidation();

    new IPluginStepModeBuilder<TEntity> PreOperation();

    new IPluginStepModeBuilder<TEntity> MainOperation();

    new IPluginStepModeBuilder<TEntity> PostOperation();
}

public interface IPluginStepModeBuilder<TEntity> : IPluginStepModeBuilder
    where TEntity : Entity
{
    new IPluginStepBuilder<TEntity> Synchronous();

    new IPluginStepBuilder<TEntity> Asynchronous();
}

public interface IPluginStepBuilder<TEntity> : IPluginStepBuilder
    where TEntity : Entity
{
    new IPluginStepBuilder<TEntity> Rank(int rank);

    new IPluginStepBuilder<TEntity> WithName(string name);

    new IPluginStepBuilder<TEntity> WithFilteringAttributes(params string[] attributes);

    IPluginStepBuilder<TEntity> WithFilteringAttributes(params Expression<Func<TEntity, object>>[] attributes);

    new IPluginStepBuilder<TEntity> WhenChanged(params string[] attributes);

    IPluginStepBuilder<TEntity> WhenChanged(params Expression<Func<TEntity, object>>[] attributes);

    new IPluginStepBuilder<TEntity> WithUnsecureConfiguration(string unsecureConfiguration);

    new IPluginStepBuilder<TEntity> WithPreImage(string imageId, string name, params string[] attributes);

    IPluginStepBuilder<TEntity> WithPreImage(string imageId, string name, params Expression<Func<TEntity, object>>[] attributes);

    new IPluginStepBuilder<TEntity> WithPostImage(string imageId, string name, params string[] attributes);

    IPluginStepBuilder<TEntity> WithPostImage(string imageId, string name, params Expression<Func<TEntity, object>>[] attributes);

    new IPluginStepBuilder<TEntity> WithBothImage(string imageId, string name, params string[] attributes);

    IPluginStepBuilder<TEntity> WithBothImage(string imageId, string name, params Expression<Func<TEntity, object>>[] attributes);

    new IPluginStepBuilder<TEntity> WithImage(PluginImageOptions image);

    IPluginStepBuilder<TEntity> WithImage(PluginImageOptions image, params Expression<Func<TEntity, object>>[] attributes);
}

// The IPluginUpdateStep* interfaces predate the entity-typed builders above, which now cover every message.
// They are retained so that existing registration code that names them explicitly keeps compiling.

public interface IPluginUpdateStepStageBuilder<TEntity> : IPluginStepStageBuilder<TEntity>
    where TEntity : Entity
{
}

public interface IPluginUpdateStepModeBuilder<TEntity> : IPluginStepModeBuilder<TEntity>
    where TEntity : Entity
{
}

public interface IPluginUpdateStepBuilder<TEntity> : IPluginStepBuilder<TEntity>
    where TEntity : Entity
{
}

public interface IPluginUpdateStepStageBuilder : IPluginStepStageBuilder
{
}

public interface IPluginUpdateStepModeBuilder : IPluginStepModeBuilder
{
}

public interface IPluginUpdateStepBuilder : IPluginStepBuilder
{
}
