using Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;
using Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.RegistrationState;

namespace Pillaro.Dataverse.PluginFramework.Tests.Tests.PluginCommands;

public class DataverseRegistrationUpserterTests
{
    [Fact]
    public void BuildImageUpsertPlan_WhenStepIsUnchangedAndImageChanged_PlansImageUpsert()
    {
        var stepId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        var image = new PluginManifestImage
        {
            ImageId = imageId,
            Name = "Target",
            Type = "PreImage",
            Attributes = ["name"]
        };
        var step = new PluginManifestStep
        {
            StepId = stepId,
            MessageName = "Update",
            EntityName = "account",
            Stage = 20,
            StageName = "PreOperation",
            Mode = 0,
            ModeName = "Synchronous",
            Rank = 1,
            Images = [image]
        };
        var manifest = new PluginManifestDocument
        {
            Plugins =
            [
                new PluginManifestPlugin
                {
                    TypeName = "SamplePlugin",
                    Steps = [step]
                }
            ]
        };
        var imageChanges = new[]
        {
            new PluginImageDiff
            {
                Action = PluginDiffAction.Update,
                StepId = stepId,
                ImageId = imageId,
                Name = "Target",
                Type = "PreImage"
            }
        };

        var operations = DataverseRegistrationUpserter.BuildImageUpsertPlan(manifest, imageChanges);

        var operation = Assert.Single(operations);
        Assert.Same(step, operation.Step);
        Assert.Same(image, operation.Image);
        Assert.Equal(PluginDiffAction.Update, operation.Action);
    }

    [Theory]
    [InlineData("Create", "Id")]
    [InlineData("create", "Id")]
    [InlineData("Update", "Target")]
    [InlineData("Delete", "Target")]
    [InlineData("Assign", "Target")]
    [InlineData("Merge", "Target")]
    [InlineData("Route", "Target")]
    [InlineData("SetState", "EntityMoniker")]
    [InlineData("SetStateDynamicEntity", "EntityMoniker")]
    [InlineData("Send", "EmailId")]
    [InlineData("DeliverIncoming", "EmailId")]
    [InlineData("DeliverPromote", "EmailId")]
    [InlineData("pil_MyCustomApi", "Target")]
    public void GetMessagePropertyName_DerivesValueFromStepMessage(string messageName, string expected)
    {
        Assert.Equal(expected, DataverseRegistrationUpserter.GetMessagePropertyName(messageName));
    }

    [Theory]
    [InlineData("fax", "FaxId")]
    [InlineData("template", "TemplateId")]
    [InlineData("email", "EmailId")]
    public void GetMessagePropertyName_ForSendMessage_DerivesValueFromEntity(string entityName, string expected)
    {
        Assert.Equal(expected, DataverseRegistrationUpserter.GetMessagePropertyName("Send", entityName));
    }

    [Fact]
    public void GetMessagePropertyName_ForMergeMessage_DefaultsToTarget()
    {
        Assert.Equal("Target", DataverseRegistrationUpserter.GetMessagePropertyName("Merge", "account"));
    }

    [Fact]
    public void ResolveMessagePropertyName_WhenImageSetsItExplicitly_UsesTheImageValue()
    {
        var step = new PluginManifestStep { MessageName = "Merge", EntityName = "account" };
        var image = new PluginManifestImage { MessagePropertyName = " SubordinateId " };

        Assert.Equal("SubordinateId", DataverseRegistrationUpserter.ResolveMessagePropertyName(step, image));
    }

    [Fact]
    public void ResolveMessagePropertyName_WhenImageLeavesItUnset_DerivesFromStep()
    {
        var step = new PluginManifestStep { MessageName = "Create", EntityName = "contact" };
        var image = new PluginManifestImage();

        Assert.Equal("Id", DataverseRegistrationUpserter.ResolveMessagePropertyName(step, image));
    }
}
