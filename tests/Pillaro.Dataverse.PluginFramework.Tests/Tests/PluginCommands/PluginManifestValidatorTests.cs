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

    [Theory]
    [InlineData(10, "Prevalidation")]
    [InlineData(20, "Preoperation")]
    [InlineData(40, "Postoperation")]
    public void Validate_PreImageInAnyStage_IsValid(int stage, string stageName)
    {
        var manifest = CreateManifest(CreateStep("Update", stage, stageName, CreateImage("PreImage", "target")));

        var errors = PluginManifestValidator.Validate(manifest);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(10, "Prevalidation")]
    [InlineData(20, "Preoperation")]
    public void Validate_PostImageOutsidePostOperation_ReturnsError(int stage, string stageName)
    {
        var manifest = CreateManifest(CreateStep("Update", stage, stageName, CreateImage("PostImage", "target")));

        var errors = PluginManifestValidator.Validate(manifest);

        Assert.Contains(errors, error => error.Contains("Post-images are available only in the PostOperation stage"));
    }

    [Fact]
    public void Validate_PostImageInPostOperation_IsValid()
    {
        var manifest = CreateManifest(CreateStep("Update", 40, "Postoperation", CreateImage("PostImage", "target")));

        var errors = PluginManifestValidator.Validate(manifest);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_CreateStepWithPostImageInPostOperation_IsValid()
    {
        var manifest = CreateManifest(CreateStep("Create", 40, "Postoperation", CreateImage("PostImage", "target")));

        var errors = PluginManifestValidator.Validate(manifest);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_CreateStepWithFilteringAttributes_IsValid()
    {
        var step = CreateStep("Create", 20, "Preoperation");
        step.FilteringAttributes = ["firstname", "lastname"];

        var errors = PluginManifestValidator.Validate(CreateManifest(step));

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_BothImageOnCreateStep_ReturnsError()
    {
        var manifest = CreateManifest(CreateStep("Create", 40, "Postoperation", CreateImage("Both", "target")));

        var errors = PluginManifestValidator.Validate(manifest);

        Assert.Contains(errors, error => error.Contains("cannot define Both image"));
    }

    [Fact]
    public void Validate_BothImageOnUpdatePostOperation_IsValid()
    {
        var manifest = CreateManifest(CreateStep("Update", 40, "Postoperation", CreateImage("Both", "target")));

        var errors = PluginManifestValidator.Validate(manifest);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_BothImageOnDeleteStep_ReturnsError()
    {
        var manifest = CreateManifest(CreateStep("Delete", 40, "Postoperation", CreateImage("Both", "target")));

        var errors = PluginManifestValidator.Validate(manifest);

        Assert.Contains(errors, error => error.Contains("cannot define Both image"));
    }

    [Fact]
    public void Validate_SameKeyInPreAndPostImageCollections_IsValid()
    {
        var manifest = CreateManifest(CreateStep(
            "Update",
            40,
            "Postoperation",
            CreateImage("PreImage", "target"),
            CreateImage("PostImage", "target")));

        var errors = PluginManifestValidator.Validate(manifest);

        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_DuplicateKeyWithinPreImageCollection_ReturnsError()
    {
        var manifest = CreateManifest(CreateStep(
            "Update",
            20,
            "Preoperation",
            CreateImage("PreImage", "target"),
            CreateImage("PreImage", "other", entityAlias: "target")));

        var errors = PluginManifestValidator.Validate(manifest);

        Assert.Contains(errors, error => error.Contains("Duplicate pre-image key 'target'"));
    }

    [Fact]
    public void Validate_BothImageCollidesWithPostImageKey_ReturnsError()
    {
        var manifest = CreateManifest(CreateStep(
            "Update",
            40,
            "Postoperation",
            CreateImage("Both", "target"),
            CreateImage("PostImage", "other", entityAlias: "target")));

        var errors = PluginManifestValidator.Validate(manifest);

        Assert.Contains(errors, error => error.Contains("Duplicate post-image key 'target'"));
    }

    private static PluginManifestStep CreateStep(
        string messageName,
        int stage,
        string stageName,
        params PluginManifestImage[] images)
    {
        return new PluginManifestStep
        {
            StepId = Guid.NewGuid(),
            MessageName = messageName,
            EntityName = "contact",
            Stage = stage,
            StageName = stageName,
            // Asynchronous, so that the "synchronous Update needs filtering attributes" rule stays out of the way.
            Mode = 1,
            ModeName = "Asynchronous",
            Rank = 1,
            Images = [.. images],
        };
    }

    private static PluginManifestImage CreateImage(string type, string name, string? entityAlias = null)
    {
        return new PluginManifestImage
        {
            ImageId = Guid.NewGuid(),
            Name = name,
            Type = type,
            EntityAlias = entityAlias,
            Attributes = ["firstname"],
        };
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
