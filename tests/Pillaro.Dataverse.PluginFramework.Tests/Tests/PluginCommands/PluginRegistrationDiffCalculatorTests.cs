using Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;
using Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.RegistrationState;

namespace Pillaro.Dataverse.PluginFramework.Tests.Tests.PluginCommands;

public class PluginRegistrationDiffCalculatorTests
{
    [Fact]
    public void Calculate_MainOperationStep_ProducesNoStepChanges()
    {
        var manifest = CreateManifest(new PluginManifestStep
        {
            StepId = Guid.NewGuid(),
            MessageName = "pil_CopyInvoice",
            EntityName = "pil_invoice",
            Stage = 30,
            StageName = "Mainoperation",
            Mode = 0,
            ModeName = "Synchronous",
            Rank = 1,
        });
        var currentState = new DataverseRegistrationState();

        var diff = PluginRegistrationDiffCalculator.Calculate(manifest, currentState);

        Assert.Empty(diff.StepChanges);
        Assert.Empty(diff.ImageChanges);
        Assert.False(diff.HasChanges);
    }

    [Fact]
    public void Calculate_ExistingMainOperationStepInDataverse_IsNotDeleted()
    {
        var manifest = CreateManifest(new PluginManifestStep
        {
            StepId = Guid.NewGuid(),
            MessageName = "pil_CopyInvoice",
            EntityName = "pil_invoice",
            Stage = 30,
            StageName = "Mainoperation",
            Mode = 0,
            ModeName = "Synchronous",
            Rank = 1,
        });

        // Dataverse auto-creates a stage-30 step when CustomAPI.PluginTypeId references the plugin type.
        var autoCreatedStepId = Guid.NewGuid();
        var currentState = new DataverseRegistrationState();
        currentState.StepsById[autoCreatedStepId] = new DataverseStepState
        {
            StepId = autoCreatedStepId,
            PluginTypeName = "SamplePlugin",
            MessageName = "pil_CopyInvoice",
            EntityName = "pil_invoice",
            Stage = 30,
            Mode = 0,
            Rank = 1,
        };

        var diff = PluginRegistrationDiffCalculator.Calculate(manifest, currentState);

        Assert.DoesNotContain(diff.StepChanges, change => change.Action == PluginDiffAction.Delete);
    }

    [Fact]
    public void Calculate_ImageOnExistingMainOperationStep_IsNotDeleted()
    {
        var manifest = CreateManifest(new PluginManifestStep
        {
            StepId = Guid.NewGuid(),
            MessageName = "pil_CopyInvoice",
            EntityName = "pil_invoice",
            Stage = 30,
            StageName = "Mainoperation",
            Mode = 0,
            ModeName = "Synchronous",
            Rank = 1,
        });

        var autoCreatedStepId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var currentState = new DataverseRegistrationState();
        currentState.StepsById[autoCreatedStepId] = new DataverseStepState
        {
            StepId = autoCreatedStepId,
            PluginTypeName = "SamplePlugin",
            MessageName = "pil_CopyInvoice",
            EntityName = "pil_invoice",
            Stage = 30,
            Mode = 0,
            Rank = 1,
        };
        currentState.ImagesById[imageId] = new DataverseImageState
        {
            ImageId = imageId,
            StepId = autoCreatedStepId,
            Name = "image",
            Type = "PostImage",
            Attributes = ["name"],
        };

        var diff = PluginRegistrationDiffCalculator.Calculate(manifest, currentState);

        Assert.DoesNotContain(diff.ImageChanges, change => change.Action == PluginDiffAction.Delete);
    }

    [Fact]
    public void Calculate_NonMainOperationStepMissingInDataverse_IsCreated()
    {
        var manifest = CreateManifest(new PluginManifestStep
        {
            StepId = Guid.NewGuid(),
            MessageName = "Update",
            EntityName = "account",
            Stage = 20,
            StageName = "Preoperation",
            Mode = 0,
            ModeName = "Synchronous",
            Rank = 1,
            FilteringAttributes = ["name"],
        });
        var currentState = new DataverseRegistrationState();

        var diff = PluginRegistrationDiffCalculator.Calculate(manifest, currentState);

        var change = Assert.Single(diff.StepChanges);
        Assert.Equal(PluginDiffAction.Create, change.Action);
    }

    [Fact]
    public void Calculate_DisabledStepWithNoOtherChanges_IsUpdatedAndReasonExplainsReEnable()
    {
        var stepId = Guid.NewGuid();
        var manifest = CreateManifest(new PluginManifestStep
        {
            StepId = stepId,
            MessageName = "Update",
            EntityName = "account",
            Stage = 20,
            StageName = "Preoperation",
            Mode = 0,
            ModeName = "Synchronous",
            Rank = 1,
            FilteringAttributes = ["name"],
        });
        var currentState = new DataverseRegistrationState();
        currentState.StepsById[stepId] = new DataverseStepState
        {
            StepId = stepId,
            PluginTypeName = "SamplePlugin",
            MessageName = "Update",
            EntityName = "account",
            Stage = 20,
            Mode = 0,
            Rank = 1,
            FilteringAttributes = ["name"],
            IsDisabled = true,
        };

        var diff = PluginRegistrationDiffCalculator.Calculate(manifest, currentState);

        var change = Assert.Single(diff.StepChanges);
        Assert.Equal(PluginDiffAction.Update, change.Action);
        Assert.Contains(change.Reasons, reason => reason.Contains("disabled in Dataverse and will be re-enabled"));
    }

    [Fact]
    public void Calculate_EnabledStepWithNoChanges_StaysUnchanged()
    {
        var stepId = Guid.NewGuid();
        var manifest = CreateManifest(new PluginManifestStep
        {
            StepId = stepId,
            MessageName = "Update",
            EntityName = "account",
            Stage = 20,
            StageName = "Preoperation",
            Mode = 0,
            ModeName = "Synchronous",
            Rank = 1,
            FilteringAttributes = ["name"],
        });
        var currentState = new DataverseRegistrationState();
        currentState.StepsById[stepId] = new DataverseStepState
        {
            StepId = stepId,
            PluginTypeName = "SamplePlugin",
            MessageName = "Update",
            EntityName = "account",
            Stage = 20,
            Mode = 0,
            Rank = 1,
            FilteringAttributes = ["name"],
            IsDisabled = false,
        };

        var diff = PluginRegistrationDiffCalculator.Calculate(manifest, currentState);

        var change = Assert.Single(diff.StepChanges);
        Assert.Equal(PluginDiffAction.Unchanged, change.Action);
    }

    [Fact]
    public void Calculate_ImageMatchingDataverseDefaults_IsUnchanged()
    {
        var (manifest, currentState) = CreateImageScenario(
            desiredAlias: null,
            desiredMessagePropertyName: null,
            currentAlias: "target",
            currentMessagePropertyName: "Target");

        var diff = PluginRegistrationDiffCalculator.Calculate(manifest, currentState);

        Assert.All(diff.ImageChanges, change => Assert.Equal(PluginDiffAction.Unchanged, change.Action));
    }

    [Fact]
    public void Calculate_ImageWithChangedEntityAlias_IsUpdated()
    {
        var (manifest, currentState) = CreateImageScenario(
            desiredAlias: "preimg",
            desiredMessagePropertyName: null,
            currentAlias: "target",
            currentMessagePropertyName: "Target");

        var diff = PluginRegistrationDiffCalculator.Calculate(manifest, currentState);

        Assert.Contains(diff.ImageChanges, change => change.Action == PluginDiffAction.Update);
    }

    [Fact]
    public void Calculate_ImageWithChangedMessagePropertyName_IsUpdated()
    {
        var (manifest, currentState) = CreateImageScenario(
            desiredAlias: null,
            desiredMessagePropertyName: "SubordinateId",
            currentAlias: "target",
            currentMessagePropertyName: "Target");

        var diff = PluginRegistrationDiffCalculator.Calculate(manifest, currentState);

        Assert.Contains(diff.ImageChanges, change => change.Action == PluginDiffAction.Update);
    }

    private static (PluginManifestDocument Manifest, DataverseRegistrationState CurrentState) CreateImageScenario(
        string? desiredAlias,
        string? desiredMessagePropertyName,
        string currentAlias,
        string currentMessagePropertyName)
    {
        var stepId = Guid.NewGuid();
        var imageId = Guid.NewGuid();

        var manifest = CreateManifest(new PluginManifestStep
        {
            StepId = stepId,
            MessageName = "Update",
            EntityName = "account",
            Stage = 40,
            StageName = "Postoperation",
            Mode = 0,
            ModeName = "Synchronous",
            Rank = 1,
            FilteringAttributes = ["name"],
            Images =
            [
                new PluginManifestImage
                {
                    ImageId = imageId,
                    Name = "target",
                    Type = "PreImage",
                    EntityAlias = desiredAlias,
                    MessagePropertyName = desiredMessagePropertyName,
                    Attributes = ["name"],
                }
            ],
        });

        var currentState = new DataverseRegistrationState();
        currentState.PluginTypeIdsByName["SamplePlugin"] = Guid.NewGuid();
        currentState.StepsById[stepId] = new DataverseStepState
        {
            StepId = stepId,
            PluginTypeName = "SamplePlugin",
            MessageName = "Update",
            EntityName = "account",
            Stage = 40,
            Mode = 0,
            Rank = 1,
            FilteringAttributes = ["name"],
        };
        currentState.ImagesById[imageId] = new DataverseImageState
        {
            ImageId = imageId,
            StepId = stepId,
            Name = "target",
            Type = "PreImage",
            EntityAlias = currentAlias,
            MessagePropertyName = currentMessagePropertyName,
            Attributes = ["name"],
        };

        return (manifest, currentState);
    }

    private static PluginManifestDocument CreateManifest(params PluginManifestStep[] steps)
    {
        return new PluginManifestDocument
        {
            Plugins =
            [
                new PluginManifestPlugin
                {
                    TypeName = "SamplePlugin",
                    Steps = [.. steps],
                }
            ]
        };
    }
}
