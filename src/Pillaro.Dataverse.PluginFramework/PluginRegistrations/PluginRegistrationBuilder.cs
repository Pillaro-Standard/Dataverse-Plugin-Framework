using Microsoft.Xrm.Sdk;
using Pillaro.Dataverse.PluginFramework.Plugins;
using System.Reflection;

namespace Pillaro.Dataverse.PluginFramework.PluginRegistrations;

public sealed class PluginRegistrationBuilder<TPlugin> : IPluginRegistration
    where TPlugin : IPlugin
{
    private readonly List<StepBuilder> _steps = [];

    public IPluginStepStageBuilder OnCreate<TEntity>(string stepId)
        where TEntity : Entity => CreateStep(stepId, DataverseMessages.Create, GetEntityLogicalName<TEntity>(), isUpdate: false);

    public IPluginUpdateStepStageBuilder OnUpdate<TEntity>(string stepId)
        where TEntity : Entity => CreateStep(stepId, DataverseMessages.Update, GetEntityLogicalName<TEntity>(), isUpdate: true);

    public IPluginStepStageBuilder OnDelete<TEntity>(string stepId)
        where TEntity : Entity => CreateStep(stepId, DataverseMessages.Delete, GetEntityLogicalName<TEntity>(), isUpdate: false);

    public IPluginStepStageBuilder OnMessage(string stepId, string messageName) => CreateStep(stepId, messageName, entityName: null, isUpdate: false);

    public IPluginStepStageBuilder OnMessage<TEntity>(string stepId, string messageName)
        where TEntity : Entity => CreateStep(stepId, messageName, GetEntityLogicalName<TEntity>(), isUpdate: false);

    public PluginRegistrationDescriptor Build()
    {
        return new PluginRegistrationDescriptor(
            typeof(TPlugin),
            _steps.Select(step => step.Build(typeof(TPlugin))).ToArray());
    }

    private StepBuilder CreateStep(string stepId, string messageName, string? entityName, bool isUpdate)
    {
        var builder = new StepBuilder(stepId, messageName, entityName, isUpdate);
        _steps.Add(builder);
        return builder;
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

    private sealed class StepBuilder : IPluginUpdateStepStageBuilder, IPluginStepModeBuilder, IPluginUpdateStepModeBuilder, IPluginUpdateStepBuilder
    {
        private readonly bool _isUpdate;
        private readonly List<string> _filteringAttributes = [];
        private readonly List<PluginImageRegistrationDescriptor> _images = [];
        private PluginStage? _stage;
        private PluginMode? _mode;
        private int _rank = 1;
        private PluginDeploymentPolicyDescriptor? _deploymentPolicy;

        public StepBuilder(string stepId, string messageName, string? entityName, bool isUpdate)
        {
            StepId = ParseGuid(stepId, nameof(stepId));
            MessageName = RequireValue(messageName, nameof(messageName));
            EntityName = entityName;
            _isUpdate = isUpdate;
        }

        private Guid StepId { get; }

        private string MessageName { get; }

        private string? EntityName { get; }

        IPluginStepModeBuilder IPluginStepStageBuilder.PreValidation() => SetStage(PluginStage.Prevalidation);

        IPluginStepModeBuilder IPluginStepStageBuilder.PreOperation() => SetStage(PluginStage.Preoperation);

        IPluginStepModeBuilder IPluginStepStageBuilder.PostOperation() => SetStage(PluginStage.Postoperation);

        IPluginUpdateStepModeBuilder IPluginUpdateStepStageBuilder.PreValidation() => SetStage(PluginStage.Prevalidation);

        IPluginUpdateStepModeBuilder IPluginUpdateStepStageBuilder.PreOperation() => SetStage(PluginStage.Preoperation);

        IPluginUpdateStepModeBuilder IPluginUpdateStepStageBuilder.PostOperation() => SetStage(PluginStage.Postoperation);

        IPluginStepBuilder IPluginStepModeBuilder.Synchronous() => SetMode(PluginMode.Synchronous);

        IPluginStepBuilder IPluginStepModeBuilder.Asynchronous() => SetMode(PluginMode.Asynchronous);

        IPluginUpdateStepBuilder IPluginUpdateStepModeBuilder.Synchronous() => SetMode(PluginMode.Synchronous);

        IPluginUpdateStepBuilder IPluginUpdateStepModeBuilder.Asynchronous() => SetMode(PluginMode.Asynchronous);

        public IPluginUpdateStepBuilder WhenChanged(params string[] attributes)
        {
            if (!_isUpdate)
            {
                throw new InvalidOperationException("Filtering attributes can be configured only for Update message steps.");
            }

            _filteringAttributes.AddRange(NormalizeAttributes(attributes, nameof(attributes)));
            return this;
        }

        public IPluginUpdateStepBuilder Rank(int rank)
        {
            SetRank(rank);
            return this;
        }

        IPluginStepBuilder IPluginStepBuilder.Rank(int rank)
        {
            SetRank(rank);
            return this;
        }

        public IPluginUpdateStepBuilder WithPreImage(string imageId, string name, params string[] attributes)
        {
            AddImage(imageId, PluginImageType.PreImage, name, attributes);
            return this;
        }

        IPluginStepBuilder IPluginStepBuilder.WithPreImage(string imageId, string name, params string[] attributes)
        {
            AddImage(imageId, PluginImageType.PreImage, name, attributes);
            return this;
        }

        public IPluginUpdateStepBuilder WithPostImage(string imageId, string name, params string[] attributes)
        {
            AddImage(imageId, PluginImageType.PostImage, name, attributes);
            return this;
        }

        IPluginStepBuilder IPluginStepBuilder.WithPostImage(string imageId, string name, params string[] attributes)
        {
            AddImage(imageId, PluginImageType.PostImage, name, attributes);
            return this;
        }

        public IPluginUpdateStepBuilder RequiresConfirmation(PluginRisk risk, string reason, PluginDeploymentScope scope = PluginDeploymentScope.All)
        {
            SetDeploymentPolicy(risk, reason, scope);
            return this;
        }

        IPluginStepBuilder IPluginStepBuilder.RequiresConfirmation(PluginRisk risk, string reason, PluginDeploymentScope scope)
        {
            SetDeploymentPolicy(risk, reason, scope);
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
                _filteringAttributes.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                _images.ToArray(),
                _deploymentPolicy);
        }

        private StepBuilder SetStage(PluginStage stage)
        {
            _stage = stage;
            return this;
        }

        private StepBuilder SetMode(PluginMode mode)
        {
            _mode = mode;
            return this;
        }

        private void SetRank(int rank)
        {
            if (rank <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rank), rank, "Rank must be greater than zero.");
            }

            _rank = rank;
        }

        private void AddImage(string imageId, PluginImageType type, string name, string[] attributes)
        {
            var parsedImageId = ParseGuid(imageId, nameof(imageId));
            var normalizedName = RequireValue(name, nameof(name));

            if (_images.Any(image => image.ImageId == parsedImageId))
            {
                throw new InvalidOperationException($"Plugin step '{StepId}' already contains image with ID '{parsedImageId}'.");
            }

            if (_images.Any(image => string.Equals(image.Name, normalizedName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Plugin step '{StepId}' already contains image named '{normalizedName}'. Image names must be unique within one step.");
            }

            _images.Add(new PluginImageRegistrationDescriptor(
                parsedImageId,
                type,
                normalizedName,
                NormalizeAttributes(attributes, nameof(attributes)).ToArray()));
        }

        private void SetDeploymentPolicy(PluginRisk risk, string reason, PluginDeploymentScope scope)
        {
            _deploymentPolicy = new PluginDeploymentPolicyDescriptor(
                RequiresConfirmation: true,
                Risk: risk,
                Reason: RequireValue(reason, nameof(reason)),
                Scope: scope);
        }

        private static Guid ParseGuid(string value, string parameterName)
        {
            if (!Guid.TryParse(value, out var guid) || guid == Guid.Empty)
            {
                throw new ArgumentException("Value must be a non-empty GUID.", parameterName);
            }

            return guid;
        }

        private static string RequireValue(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value is required.", parameterName);
            }

            return value.Trim();
        }

        private static IReadOnlyCollection<string> NormalizeAttributes(string[] attributes, string parameterName)
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
}
