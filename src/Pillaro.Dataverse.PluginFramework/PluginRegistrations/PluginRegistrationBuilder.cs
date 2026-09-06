using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Pillaro.Dataverse.PluginFramework.Plugins;
using System.Linq.Expressions;
using System.Reflection;

namespace Pillaro.Dataverse.PluginFramework.PluginRegistrations;

public sealed class PluginRegistrationBuilder<TPlugin> : IPluginRegistration
    where TPlugin : IPlugin
{
    private readonly List<IStepBuilder> _steps = [];

    public IPluginStepStageBuilder<TEntity> OnCreate<TEntity>(string stepId)
        where TEntity : Entity => CreateTypedStep<TEntity>(stepId, DataverseMessages.Create);

    public IPluginStepStageBuilder OnCreate(string entityLogicalName, string stepId)
    {
        ValidateEntityLogicalName(entityLogicalName);
        return CreateStep(stepId, DataverseMessages.Create, entityLogicalName);
    }

    public IPluginUpdateStepStageBuilder<TEntity> OnUpdate<TEntity>(string stepId)
        where TEntity : Entity => CreateTypedStep<TEntity>(stepId, DataverseMessages.Update);

    public IPluginUpdateStepStageBuilder OnUpdate(string entityLogicalName, string stepId)
    {
        ValidateEntityLogicalName(entityLogicalName);
        return CreateStep(stepId, DataverseMessages.Update, entityLogicalName);
    }

    public IPluginStepStageBuilder<TEntity> OnDelete<TEntity>(string stepId)
        where TEntity : Entity => CreateTypedStep<TEntity>(stepId, DataverseMessages.Delete);

    public IPluginStepStageBuilder OnDelete(string entityLogicalName, string stepId)
    {
        ValidateEntityLogicalName(entityLogicalName);
        return CreateStep(stepId, DataverseMessages.Delete, entityLogicalName);
    }

    public IPluginStepStageBuilder OnMessage(string stepId, string messageName) => CreateStep(stepId, messageName, entityName: null);

    public IPluginStepStageBuilder<TEntity> OnMessage<TEntity>(string stepId, string messageName)
        where TEntity : Entity => CreateTypedStep<TEntity>(stepId, messageName);

    public IPluginStepStageBuilder OnMessage(string entityLogicalName, string stepId, string messageName)
    {
        ValidateEntityLogicalName(entityLogicalName);
        return CreateStep(stepId, messageName, entityLogicalName);
    }

    public PluginRegistrationDescriptor Build()
    {
        return new PluginRegistrationDescriptor(
            typeof(TPlugin),
            _steps.Select(step => step.Build(typeof(TPlugin))).ToArray());
    }

    private StepBuilder CreateStep(string stepId, string messageName, string entityName)
    {
        var builder = new StepBuilder(stepId, messageName, entityName);
        _steps.Add(builder);
        return builder;
    }

    private TypedStepBuilder<TEntity> CreateTypedStep<TEntity>(string stepId, string messageName)
        where TEntity : Entity
    {
        var builder = new TypedStepBuilder<TEntity>(stepId, messageName, GetEntityLogicalName<TEntity>());
        _steps.Add(builder);
        return builder;
    }

    private static void ValidateEntityLogicalName(string entityLogicalName)
    {
        if (string.IsNullOrWhiteSpace(entityLogicalName))
        {
            throw new ArgumentException("Entity logical name is required.", nameof(entityLogicalName));
        }
    }

    private static string GetEntityLogicalName<TEntity>()
        where TEntity : Entity
    {
        var logicalNameAttribute = typeof(TEntity).GetCustomAttribute<EntityLogicalNameAttribute>();
        if (logicalNameAttribute == null || string.IsNullOrWhiteSpace(logicalNameAttribute.LogicalName))
        {
            throw new InvalidOperationException($"Entity type '{typeof(TEntity).FullName}' must be decorated with EntityLogicalNameAttribute.");
        }

        return logicalNameAttribute.LogicalName;
    }

    private interface IStepBuilder
    {
        PluginStepRegistrationDescriptor Build(Type pluginType);
    }

    /// <summary>
    /// Holds the step state shared by the untyped and entity-typed builders. Every capability lives here,
    /// so the two builders differ only in the return types they expose.
    /// </summary>
    private abstract class StepBuilderBase : IStepBuilder
    {
        private readonly List<string> _filteringAttributes = [];
        private readonly List<PluginImageRegistrationDescriptor> _images = [];
        private PluginStage? _stage;
        private PluginMode? _mode;
        private int _rank = 1;
        private string _name;
        private PluginDeploymentPolicyDescriptor _deploymentPolicy;
        private string _unsecureConfiguration;

        protected StepBuilderBase(string stepId, string messageName, string entityName)
        {
            StepId = ParseGuid(stepId, nameof(stepId));
            MessageName = RequireValue(messageName, nameof(messageName));
            EntityName = entityName;
        }

        protected Guid StepId { get; }

        private string MessageName { get; }

        private string EntityName { get; }

        public PluginStepRegistrationDescriptor Build(Type pluginType)
        {
            if (_stage == null)
            {
                throw new InvalidOperationException($"Plugin step '{StepId}' must define a pipeline stage.");
            }

            if (_mode == null)
            {
                throw new InvalidOperationException($"Plugin step '{StepId}' must define an execution mode.");
            }

            return new PluginStepRegistrationDescriptor(
                StepId,
                pluginType,
                MessageName,
                EntityName,
                _stage.Value,
                _mode.Value,
                _rank,
                _name,
                _filteringAttributes.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                _images.ToArray(),
                _deploymentPolicy,
                _unsecureConfiguration);
        }

        protected void SetStage(PluginStage stage) => _stage = stage;

        protected void SetMode(PluginMode mode) => _mode = mode;

        protected void AddFilteringAttributes(IReadOnlyCollection<string> attributes)
        {
            _filteringAttributes.AddRange(attributes);
        }

        protected void SetRank(int rank)
        {
            if (rank <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rank), rank, "Rank must be greater than zero.");
            }

            _rank = rank;
        }

        protected void SetName(string name)
        {
            _name = RequireValue(name, nameof(name));
        }

        protected void SetUnsecureConfiguration(string unsecureConfiguration)
        {
            _unsecureConfiguration = string.IsNullOrWhiteSpace(unsecureConfiguration) ? null : unsecureConfiguration.Trim();
        }

        protected void AddImage(string imageId, PluginImageType type, string name, string[] attributes)
        {
            AddImage(imageId, type, name, NormalizeAttributes(attributes, nameof(attributes)));
        }

        protected void AddImage(string imageId, PluginImageType type, string name, IReadOnlyCollection<string> attributes)
        {
            AddImage(
                new PluginImageOptions(imageId, type, name) { Attributes = attributes });
        }

        protected void AddImage(PluginImageOptions image)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            var parsedImageId = ParseGuid(image.ImageId, nameof(image.ImageId));
            var normalizedName = RequireValue(image.Name, nameof(image.Name));
            var normalizedAlias = string.IsNullOrWhiteSpace(image.EntityAlias) ? null : image.EntityAlias.Trim();
            var normalizedMessagePropertyName = string.IsNullOrWhiteSpace(image.MessagePropertyName) ? null : image.MessagePropertyName.Trim();

            if (image.Attributes == null || image.Attributes.Count == 0)
            {
                throw new ArgumentException("At least one attribute must be provided.", nameof(image));
            }

            if (_images.Any(existing => existing.ImageId == parsedImageId))
            {
                throw new InvalidOperationException($"Plugin step '{StepId}' already contains image with ID '{parsedImageId}'.");
            }

            var alias = normalizedAlias ?? normalizedName;
            var conflicting = _images.FirstOrDefault(existing =>
                string.Equals(ResolveAlias(existing), alias, StringComparison.OrdinalIgnoreCase)
                && SharesImageCollection(existing.Type, image.Type));
            if (conflicting != null)
            {
                throw new InvalidOperationException(
                    $"Plugin step '{StepId}' already contains a {conflicting.Type} image using key '{alias}'. " +
                    "Image keys must be unique per step within the pre-image and post-image collections.");
            }

            _images.Add(new PluginImageRegistrationDescriptor(
                parsedImageId,
                image.Type,
                normalizedName,
                image.Attributes.ToArray())
            {
                EntityAlias = normalizedAlias,
                MessagePropertyName = normalizedMessagePropertyName,
            });
        }

        protected void SetDeploymentPolicy(PluginRisk risk, string reason, PluginDeploymentScope scope)
        {
            _deploymentPolicy = new PluginDeploymentPolicyDescriptor(
                Risk: risk,
                Reason: RequireValue(reason, nameof(reason)),
                Scope: scope);
        }

        private static string ResolveAlias(PluginImageRegistrationDescriptor image)
            => string.IsNullOrWhiteSpace(image.EntityAlias) ? image.Name : image.EntityAlias;

        // Both occupies the pre-image and the post-image collection at once, so it collides with either type.
        private static bool SharesImageCollection(PluginImageType left, PluginImageType right)
            => left == right || left == PluginImageType.Both || right == PluginImageType.Both;

        protected static Guid ParseGuid(string value, string parameterName)
        {
            if (!Guid.TryParse(value, out var guid) || guid == Guid.Empty)
            {
                throw new ArgumentException("Value must be a non-empty GUID.", parameterName);
            }

            return guid;
        }

        protected static string RequireValue(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value is required.", parameterName);
            }

            return value.Trim();
        }

        protected static IReadOnlyCollection<string> NormalizeAttributes(string[] attributes, string parameterName)
        {
            if (attributes == null || attributes.Length == 0)
            {
                throw new ArgumentException("At least one attribute must be provided.", parameterName);
            }

            return attributes
                .Where(attribute => !string.IsNullOrWhiteSpace(attribute))
                .Select(attribute => attribute.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    /// <summary>
    /// Builder used by the registration entry points that take an entity logical name as a string.
    /// </summary>
    private sealed class StepBuilder : StepBuilderBase,
        IPluginUpdateStepStageBuilder,
        IPluginUpdateStepModeBuilder,
        IPluginUpdateStepBuilder
    {
        public StepBuilder(string stepId, string messageName, string entityName)
            : base(stepId, messageName, entityName)
        {
        }

        public IPluginStepModeBuilder PreValidation()
        {
            SetStage(PluginStage.Prevalidation);
            return this;
        }

        public IPluginStepModeBuilder PreOperation()
        {
            SetStage(PluginStage.Preoperation);
            return this;
        }

        public IPluginStepModeBuilder MainOperation()
        {
            SetStage(PluginStage.Mainoperation);
            return this;
        }

        public IPluginStepModeBuilder PostOperation()
        {
            SetStage(PluginStage.Postoperation);
            return this;
        }

        public IPluginStepBuilder Synchronous()
        {
            SetMode(PluginMode.Synchronous);
            return this;
        }

        public IPluginStepBuilder Asynchronous()
        {
            SetMode(PluginMode.Asynchronous);
            return this;
        }

        public IPluginStepBuilder Rank(int rank)
        {
            SetRank(rank);
            return this;
        }

        public IPluginStepBuilder WithName(string name)
        {
            SetName(name);
            return this;
        }

        public IPluginStepBuilder WithFilteringAttributes(params string[] attributes)
        {
            AddFilteringAttributes(NormalizeAttributes(attributes, nameof(attributes)));
            return this;
        }

        public IPluginStepBuilder WhenChanged(params string[] attributes) => WithFilteringAttributes(attributes);

        public IPluginStepBuilder WithUnsecureConfiguration(string unsecureConfiguration)
        {
            SetUnsecureConfiguration(unsecureConfiguration);
            return this;
        }

        public IPluginStepBuilder WithPreImage(string imageId, string name, params string[] attributes)
        {
            AddImage(imageId, PluginImageType.PreImage, name, attributes);
            return this;
        }

        public IPluginStepBuilder WithPostImage(string imageId, string name, params string[] attributes)
        {
            AddImage(imageId, PluginImageType.PostImage, name, attributes);
            return this;
        }

        public IPluginStepBuilder WithBothImage(string imageId, string name, params string[] attributes)
        {
            AddImage(imageId, PluginImageType.Both, name, attributes);
            return this;
        }

        public IPluginStepBuilder WithImage(PluginImageOptions image)
        {
            AddImage(image);
            return this;
        }
    }

    /// <summary>
    /// Builder used by the entity-typed registration entry points. It carries the same capabilities as
    /// <see cref="StepBuilder"/> plus the expression-based overloads, for every message.
    /// </summary>
    private sealed class TypedStepBuilder<TEntity> : StepBuilderBase,
        IPluginUpdateStepStageBuilder<TEntity>,
        IPluginUpdateStepModeBuilder<TEntity>,
        IPluginUpdateStepBuilder<TEntity>
        where TEntity : Entity
    {
        public TypedStepBuilder(string stepId, string messageName, string entityName)
            : base(stepId, messageName, entityName)
        {
        }

        public IPluginStepModeBuilder<TEntity> PreValidation()
        {
            SetStage(PluginStage.Prevalidation);
            return this;
        }

        public IPluginStepModeBuilder<TEntity> PreOperation()
        {
            SetStage(PluginStage.Preoperation);
            return this;
        }

        public IPluginStepModeBuilder<TEntity> MainOperation()
        {
            SetStage(PluginStage.Mainoperation);
            return this;
        }

        public IPluginStepModeBuilder<TEntity> PostOperation()
        {
            SetStage(PluginStage.Postoperation);
            return this;
        }

        public IPluginStepBuilder<TEntity> Synchronous()
        {
            SetMode(PluginMode.Synchronous);
            return this;
        }

        public IPluginStepBuilder<TEntity> Asynchronous()
        {
            SetMode(PluginMode.Asynchronous);
            return this;
        }

        public IPluginStepBuilder<TEntity> Rank(int rank)
        {
            SetRank(rank);
            return this;
        }

        public IPluginStepBuilder<TEntity> WithName(string name)
        {
            SetName(name);
            return this;
        }

        public IPluginStepBuilder<TEntity> WithFilteringAttributes(params string[] attributes)
        {
            AddFilteringAttributes(NormalizeAttributes(attributes, nameof(attributes)));
            return this;
        }

        public IPluginStepBuilder<TEntity> WithFilteringAttributes(params Expression<Func<TEntity, object>>[] attributes)
        {
            AddFilteringAttributes(TypedAttributeSelector.GetLogicalNames(attributes));
            return this;
        }

        public IPluginStepBuilder<TEntity> WhenChanged(params string[] attributes) => WithFilteringAttributes(attributes);

        public IPluginStepBuilder<TEntity> WhenChanged(params Expression<Func<TEntity, object>>[] attributes) => WithFilteringAttributes(attributes);

        public IPluginStepBuilder<TEntity> WithUnsecureConfiguration(string unsecureConfiguration)
        {
            SetUnsecureConfiguration(unsecureConfiguration);
            return this;
        }

        public IPluginStepBuilder<TEntity> WithPreImage(string imageId, string name, params string[] attributes)
        {
            AddImage(imageId, PluginImageType.PreImage, name, attributes);
            return this;
        }

        public IPluginStepBuilder<TEntity> WithPreImage(string imageId, string name, params Expression<Func<TEntity, object>>[] attributes)
        {
            AddImage(imageId, PluginImageType.PreImage, name, TypedAttributeSelector.GetLogicalNames(attributes));
            return this;
        }

        public IPluginStepBuilder<TEntity> WithPostImage(string imageId, string name, params string[] attributes)
        {
            AddImage(imageId, PluginImageType.PostImage, name, attributes);
            return this;
        }

        public IPluginStepBuilder<TEntity> WithPostImage(string imageId, string name, params Expression<Func<TEntity, object>>[] attributes)
        {
            AddImage(imageId, PluginImageType.PostImage, name, TypedAttributeSelector.GetLogicalNames(attributes));
            return this;
        }

        public IPluginStepBuilder<TEntity> WithBothImage(string imageId, string name, params string[] attributes)
        {
            AddImage(imageId, PluginImageType.Both, name, attributes);
            return this;
        }

        public IPluginStepBuilder<TEntity> WithBothImage(string imageId, string name, params Expression<Func<TEntity, object>>[] attributes)
        {
            AddImage(imageId, PluginImageType.Both, name, TypedAttributeSelector.GetLogicalNames(attributes));
            return this;
        }

        public IPluginStepBuilder<TEntity> WithImage(PluginImageOptions image)
        {
            AddImage(image);
            return this;
        }

        public IPluginStepBuilder<TEntity> WithImage(PluginImageOptions image, params Expression<Func<TEntity, object>>[] attributes)
        {
            if (image == null)
            {
                throw new ArgumentNullException(nameof(image));
            }

            AddImage(image with { Attributes = TypedAttributeSelector.GetLogicalNames(attributes) });
            return this;
        }

        IPluginStepModeBuilder IPluginStepStageBuilder.PreValidation() => PreValidation();

        IPluginStepModeBuilder IPluginStepStageBuilder.PreOperation() => PreOperation();

        IPluginStepModeBuilder IPluginStepStageBuilder.MainOperation() => MainOperation();

        IPluginStepModeBuilder IPluginStepStageBuilder.PostOperation() => PostOperation();

        IPluginStepBuilder IPluginStepModeBuilder.Synchronous() => Synchronous();

        IPluginStepBuilder IPluginStepModeBuilder.Asynchronous() => Asynchronous();

        IPluginStepBuilder IPluginStepBuilder.Rank(int rank) => Rank(rank);

        IPluginStepBuilder IPluginStepBuilder.WithName(string name) => WithName(name);

        IPluginStepBuilder IPluginStepBuilder.WithFilteringAttributes(params string[] attributes) => WithFilteringAttributes(attributes);

        IPluginStepBuilder IPluginStepBuilder.WhenChanged(params string[] attributes) => WhenChanged(attributes);

        IPluginStepBuilder IPluginStepBuilder.WithUnsecureConfiguration(string unsecureConfiguration) => WithUnsecureConfiguration(unsecureConfiguration);

        IPluginStepBuilder IPluginStepBuilder.WithPreImage(string imageId, string name, params string[] attributes) => WithPreImage(imageId, name, attributes);

        IPluginStepBuilder IPluginStepBuilder.WithPostImage(string imageId, string name, params string[] attributes) => WithPostImage(imageId, name, attributes);

        IPluginStepBuilder IPluginStepBuilder.WithBothImage(string imageId, string name, params string[] attributes) => WithBothImage(imageId, name, attributes);

        IPluginStepBuilder IPluginStepBuilder.WithImage(PluginImageOptions image) => WithImage(image);
    }
}
