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

    public IPluginStepStageBuilder OnCreate<TEntity>(string stepId)
        where TEntity : Entity => CreateStep(stepId, DataverseMessages.Create, GetEntityLogicalName<TEntity>(), isUpdate: false);

    public IPluginStepStageBuilder OnCreate(string entityLogicalName, string stepId)
    {
        ValidateEntityLogicalName(entityLogicalName);
        return CreateStep(stepId, DataverseMessages.Create, entityLogicalName, isUpdate: false);
    }

    public IPluginUpdateStepStageBuilder<TEntity> OnUpdate<TEntity>(string stepId)
        where TEntity : Entity
    {
        var builder = new UpdateStepBuilder<TEntity>(stepId, DataverseMessages.Update, GetEntityLogicalName<TEntity>());
        _steps.Add(builder);
        return builder;
    }

    public IPluginUpdateStepStageBuilder OnUpdate(string entityLogicalName, string stepId)
    {
        ValidateEntityLogicalName(entityLogicalName);
        var builder = new UpdateStepBuilderNonGeneric(stepId, DataverseMessages.Update, entityLogicalName);
        _steps.Add(builder);
        return builder;
    }

    public IPluginStepStageBuilder OnDelete<TEntity>(string stepId)
        where TEntity : Entity => CreateStep(stepId, DataverseMessages.Delete, GetEntityLogicalName<TEntity>(), isUpdate: false);

    public IPluginStepStageBuilder OnDelete(string entityLogicalName, string stepId)
    {
        ValidateEntityLogicalName(entityLogicalName);
        return CreateStep(stepId, DataverseMessages.Delete, entityLogicalName, isUpdate: false);
    }

    public IPluginStepStageBuilder OnMessage(string stepId, string messageName) => CreateStep(stepId, messageName, entityName: null, isUpdate: false);

    public IPluginStepStageBuilder OnMessage<TEntity>(string stepId, string messageName)
        where TEntity : Entity => CreateStep(stepId, messageName, GetEntityLogicalName<TEntity>(), isUpdate: false);

    public IPluginStepStageBuilder OnMessage(string entityLogicalName, string stepId, string messageName)
    {
        ValidateEntityLogicalName(entityLogicalName);
        return CreateStep(stepId, messageName, entityLogicalName, isUpdate: false);
    }

    public PluginRegistrationDescriptor Build()
    {
        return new PluginRegistrationDescriptor(
            typeof(TPlugin),
            _steps.Select(step => step.Build(typeof(TPlugin))).ToArray());
    }

    private StepBuilder CreateStep(string stepId, string messageName, string entityName, bool isUpdate)
    {
        var builder = new StepBuilder(stepId, messageName, entityName, isUpdate);
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

    private class StepBuilder : IPluginStepStageBuilder, IPluginStepModeBuilder, IPluginStepBuilder, IStepBuilder
    {
        private readonly bool _isUpdate;
        private readonly List<string> _filteringAttributes = [];
        private readonly List<PluginImageRegistrationDescriptor> _images = [];
        private PluginStage? _stage;
        private PluginMode? _mode;
        private int _rank = 1;
        private string _name;
        private PluginDeploymentPolicyDescriptor _deploymentPolicy;
        private string _unsecureConfiguration;

        public StepBuilder(string stepId, string messageName, string entityName, bool isUpdate)
        {
            StepId = ParseGuid(stepId, nameof(stepId));
            MessageName = RequireValue(messageName, nameof(messageName));
            EntityName = entityName;
            _isUpdate = isUpdate;
        }

        protected Guid StepId { get; }

        private string MessageName { get; }

        private string EntityName { get; }

        public IPluginStepModeBuilder PreValidation() => SetStage(PluginStage.Prevalidation);

        public IPluginStepModeBuilder PreOperation() => SetStage(PluginStage.Preoperation);

        public IPluginStepModeBuilder MainOperation() => SetStage(PluginStage.Mainoperation);

        public IPluginStepModeBuilder PostOperation() => SetStage(PluginStage.Postoperation);

        public IPluginStepBuilder Synchronous() => SetMode(PluginMode.Synchronous);

        public IPluginStepBuilder Asynchronous() => SetMode(PluginMode.Asynchronous);

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

        protected StepBuilder SetStage(PluginStage stage)
        {
            _stage = stage;
            return this;
        }

        protected StepBuilder SetMode(PluginMode mode)
        {
            _mode = mode;
            return this;
        }

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
            var parsedImageId = ParseGuid(imageId, nameof(imageId));
            var normalizedName = RequireValue(name, nameof(name));

            if (_images.Any(image => image.ImageId == parsedImageId))
            {
                throw new InvalidOperationException($"Plugin step '{StepId}' already contains image with ID '{parsedImageId}'.");
            }

            if (_images.Any(image => string.Equals(image.Name, normalizedName, StringComparison.OrdinalIgnoreCase) && image.Type == type))
            {
                throw new InvalidOperationException($"Plugin step '{StepId}' already contains {type} image named '{normalizedName}'. Image names must be unique per type within one step.");
            }

            _images.Add(new PluginImageRegistrationDescriptor(
                parsedImageId,
                type,
                normalizedName,
                attributes.ToArray()));
        }

        protected void SetDeploymentPolicy(PluginRisk risk, string reason, PluginDeploymentScope scope)
        {
            _deploymentPolicy = new PluginDeploymentPolicyDescriptor(
                Risk: risk, 
                Reason: RequireValue(reason, nameof(reason)),
                Scope: scope);
        }

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

    private sealed class UpdateStepBuilder<TEntity> : StepBuilder,
        IPluginUpdateStepStageBuilder<TEntity>,
        IPluginUpdateStepModeBuilder<TEntity>,
        IPluginUpdateStepBuilder<TEntity>
        where TEntity : Entity
    {
        public UpdateStepBuilder(string stepId, string messageName, string entityName)
            : base(stepId, messageName, entityName, isUpdate: true)
        {
        }

        IPluginUpdateStepModeBuilder<TEntity> IPluginUpdateStepStageBuilder<TEntity>.PreValidation()
        {
            SetStage(PluginStage.Prevalidation);
            return this;
        }

        IPluginUpdateStepModeBuilder<TEntity> IPluginUpdateStepStageBuilder<TEntity>.PreOperation()
        {
            SetStage(PluginStage.Preoperation);
            return this;
        }

        IPluginUpdateStepModeBuilder<TEntity> IPluginUpdateStepStageBuilder<TEntity>.MainOperation()
        {
            SetStage(PluginStage.Mainoperation);
            return this;
        }

        IPluginUpdateStepModeBuilder<TEntity> IPluginUpdateStepStageBuilder<TEntity>.PostOperation()
        {
            SetStage(PluginStage.Postoperation);
            return this;
        }

        IPluginUpdateStepBuilder<TEntity> IPluginUpdateStepModeBuilder<TEntity>.Synchronous()
        {
            SetMode(PluginMode.Synchronous);
            return this;
        }

        IPluginUpdateStepBuilder<TEntity> IPluginUpdateStepModeBuilder<TEntity>.Asynchronous()
        {
            SetMode(PluginMode.Asynchronous);
            return this;
        }

        public new IPluginUpdateStepBuilder<TEntity> Rank(int rank)
        {
            SetRank(rank);
            return this;
        }

        public new IPluginUpdateStepBuilder<TEntity> WithName(string name)
        {
            SetName(name);
            return this;
        }

        public new IPluginUpdateStepBuilder<TEntity> WithFilteringAttributes(params string[] attributes)
        {
            AddFilteringAttributes(NormalizeAttributes(attributes, nameof(attributes)));
            return this;
        }

        public new IPluginUpdateStepBuilder<TEntity> WithUnsecureConfiguration(string unsecureConfiguration)
        {
            SetUnsecureConfiguration(unsecureConfiguration);
            return this;
        }

        public IPluginUpdateStepBuilder<TEntity> WhenChanged(params string[] attributes)
        {
            AddFilteringAttributes(NormalizeAttributes(attributes, nameof(attributes)));
            return this;
        }

        public IPluginUpdateStepBuilder<TEntity> WhenChanged(params Expression<Func<TEntity, object>>[] attributes)
        {
            AddFilteringAttributes(TypedAttributeSelector.GetLogicalNames(attributes));
            return this;
        }

        public new IPluginUpdateStepBuilder<TEntity> WithPreImage(string imageId, string name, params string[] attributes)
        {
            AddImage(imageId, PluginImageType.PreImage, name, attributes);
            return this;
        }

        public IPluginUpdateStepBuilder<TEntity> WithPreImage(string imageId, string name, params Expression<Func<TEntity, object>>[] attributes)
        {
            AddImage(imageId, PluginImageType.PreImage, name, TypedAttributeSelector.GetLogicalNames(attributes));
            return this;
        }

        public new IPluginUpdateStepBuilder<TEntity> WithPostImage(string imageId, string name, params string[] attributes)
        {
            AddImage(imageId, PluginImageType.PostImage, name, attributes);
            return this;
        }

        public IPluginUpdateStepBuilder<TEntity> WithPostImage(string imageId, string name, params Expression<Func<TEntity, object>>[] attributes)
        {
            AddImage(imageId, PluginImageType.PostImage, name, TypedAttributeSelector.GetLogicalNames(attributes));
            return this;
        }
    }

    private sealed class UpdateStepBuilderNonGeneric : StepBuilder,
        IPluginUpdateStepStageBuilder,
        IPluginUpdateStepModeBuilder,
        IPluginUpdateStepBuilder
    {
        public UpdateStepBuilderNonGeneric(string stepId, string messageName, string entityName)
            : base(stepId, messageName, entityName, isUpdate: true)
        {
        }

        IPluginUpdateStepModeBuilder IPluginUpdateStepStageBuilder.PreValidation()
        {
            SetStage(PluginStage.Prevalidation);
            return this;
        }

        IPluginUpdateStepModeBuilder IPluginUpdateStepStageBuilder.PreOperation()
        {
            SetStage(PluginStage.Preoperation);
            return this;
        }

        IPluginUpdateStepModeBuilder IPluginUpdateStepStageBuilder.MainOperation()
        {
            SetStage(PluginStage.Mainoperation);
            return this;
        }

        IPluginUpdateStepModeBuilder IPluginUpdateStepStageBuilder.PostOperation()
        {
            SetStage(PluginStage.Postoperation);
            return this;
        }

        IPluginUpdateStepBuilder IPluginUpdateStepModeBuilder.Synchronous()
        {
            SetMode(PluginMode.Synchronous);
            return this;
        }

        IPluginUpdateStepBuilder IPluginUpdateStepModeBuilder.Asynchronous()
        {
            SetMode(PluginMode.Asynchronous);
            return this;
        }

        public new IPluginUpdateStepBuilder Rank(int rank)
        {
            SetRank(rank);
            return this;
        }

        public new IPluginUpdateStepBuilder WithName(string name)
        {
            SetName(name);
            return this;
        }

        public new IPluginUpdateStepBuilder WithFilteringAttributes(params string[] attributes)
        {
            AddFilteringAttributes(NormalizeAttributes(attributes, nameof(attributes)));
            return this;
        }

        public new IPluginUpdateStepBuilder WithUnsecureConfiguration(string unsecureConfiguration)
        {
            SetUnsecureConfiguration(unsecureConfiguration);
            return this;
        }

        public IPluginUpdateStepBuilder WhenChanged(params string[] attributes)
        {
            AddFilteringAttributes(NormalizeAttributes(attributes, nameof(attributes)));
            return this;
        }

        public new IPluginUpdateStepBuilder WithPreImage(string imageId, string name, params string[] attributes)
        {
            AddImage(imageId, PluginImageType.PreImage, name, attributes);
            return this;
        }

        public new IPluginUpdateStepBuilder WithPostImage(string imageId, string name, params string[] attributes)
        {
            AddImage(imageId, PluginImageType.PostImage, name, attributes);
            return this;
        }
    }
}
