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
        where TEntity : Entity => (IPluginUpdateStepStageBuilder)CreateStep(stepId, DataverseMessages.Update, GetEntityLogicalName<TEntity>(), isUpdate: true);

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

    private sealed class StepBuilder : IPluginUpdateStepStageBuilder, IPluginStepModeBuilder, IPluginUpdateStepBuilder
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

        public IPluginStepModeBuilder PreValidation()
        {
            _stage = PluginStage.Prevalidation;
            return this;
        }

        public IPluginStepModeBuilder PreOperation()
        {
            _stage = PluginStage.Preoperation;
            return this;
        }

        public IPluginStepModeBuilder PostOperation()
        {
            _stage = PluginStage.Postoperation;
            return this;
        }

        public IPluginStepBuilder Synchronous()
        {
            _mode = PluginMode.Synchronous;
            return this;
        }

        public IPluginStepBuilder Asynchronous()
        {
            _mode = PluginMode.Asynchronous;
            return this;
        }

        IPluginUpdateStepBuilder IPluginUpdateStepBuilder.WhenChanged(params string[] attributes)
        {
            return WhenChanged(attributes);
        }

        public IPluginUpdateStepBuilder WhenChanged(params string[] attributes)
        {
            if (!_isUpdate)
            {
                throw new InvalidOperationException("Filtering attributes can be configured only for Update message steps.");
            }

            _filteringAttributes.AddRange(NormalizeAttributes(attributes, nameof(attributes)));
            return this;
        }

        public IPluginStepBuilder Rank(int rank)
        {
            if (rank <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rank), rank, "Rank must be greater than zero.");
            }

            _rank = rank;
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

        public IPluginStepBuilder RequiresConfirmation(PluginRisk risk, string reason, PluginDeploymentScope scope = PluginDeploymentScope.All)
        {
            _deploymentPolicy = new PluginDeploymentPolicyDescriptor(
                RequiresConfirmation: true,
                Risk: risk,
                Reason: RequireValue(reason, nameof(reason)),
                Scope: scope);

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

        private void AddImage(string imageId, PluginImageType type, string name, string[] attributes)
        {
            _images.Add(new PluginImageRegistrationDescriptor(
                ParseGuid(imageId, nameof(imageId)),
                type,
                RequireValue(name, nameof(name)),
                NormalizeAttributes(attributes, nameof(attributes)).ToArray()));
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
