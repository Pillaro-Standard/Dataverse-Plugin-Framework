using Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

namespace Pillaro.Dataverse.PluginFramework.Tests.Tests.PluginCommands;

public class PluginManifestValidatorTests
{
    [Fact]
    public void Validate_CustomApiMainOperationStep_IsValid()
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

        var errors = PluginManifestValidator.Validate(manifest);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Update")]
    [InlineData("Delete")]
    public void Validate_MainOperationStepOnPlatformMessage_ReturnsError(string messageName)
    {
        var manifest = CreateManifest(new PluginManifestStep
        {
            StepId = Guid.NewGuid(),
            MessageName = messageName,
            EntityName = "account",
            Stage = 30,
            StageName = "Mainoperation",
            Mode = 0,
            ModeName = "Synchronous",
            Rank = 1,
            FilteringAttributes = ["name"],
        });

        var errors = PluginManifestValidator.Validate(manifest);

        Assert.Contains(errors, error => error.Contains("MainOperation is supported only for Custom API messages"));
    }

    [Fact]
    public void Validate_MainOperationStepWithImage_ReturnsError()
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
            Images =
            [
                new PluginManifestImage
                {
                    ImageId = Guid.NewGuid(),
                    Name = "image",
                    Type = "PostImage",
                    Attributes = ["name"],
                }
            ],
        });

        var errors = PluginManifestValidator.Validate(manifest);

        Assert.Contains(errors, error => error.Contains("defines images in MainOperation stage"));
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
