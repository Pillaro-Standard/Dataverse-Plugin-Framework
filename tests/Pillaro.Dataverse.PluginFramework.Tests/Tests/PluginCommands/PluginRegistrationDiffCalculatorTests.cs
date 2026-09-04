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
