using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Pillaro.Dataverse.PluginFramework.PluginRegistrations;
using Pillaro.Dataverse.PluginFramework.Plugins;

namespace Pillaro.Dataverse.PluginFramework.Tests.Tests.PluginCommands;

public class PluginRegistrationBuilderTests
{
    [Fact]
    public void OnCreate_TypedStep_SupportsTypedFilteringAttributesAndPostImage()
    {
        var descriptor = PluginRegistrationDiscovery.Discover<TypedCreatePlugin>();

        var step = Assert.Single(descriptor!.Steps);
        Assert.Equal("Create", step.MessageName);
        Assert.Equal("contact", step.EntityName);
        Assert.Equal(["firstname", "lastname"], step.FilteringAttributes.OrderBy(attribute => attribute));

        var image = Assert.Single(step.Images);
        Assert.Equal(PluginImageType.PostImage, image.Type);
        Assert.Equal(["firstname"], image.Attributes);
    }

    [Fact]
    public void OnDelete_TypedStep_SupportsTypedPreImage()
    {
        var descriptor = PluginRegistrationDiscovery.Discover<TypedDeletePlugin>();

        var step = Assert.Single(descriptor!.Steps);
        var image = Assert.Single(step.Images);
        Assert.Equal(PluginImageType.PreImage, image.Type);
        Assert.Equal(["lastname"], image.Attributes);
    }

    [Fact]
    public void OnMessage_TypedStep_SupportsTypedFilteringAttributes()
    {
        var descriptor = PluginRegistrationDiscovery.Discover<TypedCustomMessagePlugin>();

        var step = Assert.Single(descriptor!.Steps);
        Assert.Equal("pil_DoSomething", step.MessageName);
        Assert.Equal(["firstname"], step.FilteringAttributes);
    }

    [Fact]
    public void WithBothImage_RegistersBothImageType()
    {
        var descriptor = PluginRegistrationDiscovery.Discover<BothImagePlugin>();

        var image = Assert.Single(Assert.Single(descriptor!.Steps).Images);
        Assert.Equal(PluginImageType.Both, image.Type);
    }

    [Fact]
    public void WithImage_CarriesEntityAliasAndMessagePropertyName()
    {
        var descriptor = PluginRegistrationDiscovery.Discover<ImageOptionsPlugin>();

        var image = Assert.Single(Assert.Single(descriptor!.Steps).Images);
        Assert.Equal("subordinate", image.EntityAlias);
        Assert.Equal("SubordinateId", image.MessagePropertyName);
        Assert.Equal(["firstname"], image.Attributes);
    }

    [Fact]
    public void WithPreImage_WhenKeyIsAlreadyUsedInPreImageCollection_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => PluginRegistrationDiscovery.Discover<DuplicateImageKeyPlugin>());

        Assert.Contains("Image keys must be unique", exception.Message);
    }

    [Fact]
    public void WithPreImageAndPostImage_MayShareTheSameKey()
    {
        var descriptor = PluginRegistrationDiscovery.Discover<SharedImageKeyPlugin>();

        Assert.Equal(2, Assert.Single(descriptor!.Steps).Images.Count);
    }

    private sealed class TypedCreatePlugin(string unsecureConfig, string secureConfig)
        : PluginBase(unsecureConfig, secureConfig)
    {
        public override void Register(IPluginRegistration registration)
        {
            registration
                .OnCreate<TestContact>("2c4a0d1a-9b3d-4f61-9e5a-9d1c3f0a7b21")
                .PostOperation()
                .Synchronous()
                .WhenChanged(contact => contact.FirstName, contact => contact.LastName)
                .WithPostImage("6f0c8bd4-3c96-4d4a-9e0f-6a2f0b7c1d33", "target", contact => contact.FirstName);
        }
    }

    private sealed class TypedDeletePlugin(string unsecureConfig, string secureConfig)
        : PluginBase(unsecureConfig, secureConfig)
    {
        public override void Register(IPluginRegistration registration)
        {
            registration
                .OnDelete<TestContact>("0a1b2c3d-4e5f-4a6b-8c9d-0e1f2a3b4c5d")
                .PreValidation()
                .Synchronous()
                .WithPreImage("1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c5e", "target", contact => contact.LastName);
        }
    }

    private sealed class TypedCustomMessagePlugin(string unsecureConfig, string secureConfig)
        : PluginBase(unsecureConfig, secureConfig)
    {
        public override void Register(IPluginRegistration registration)
        {
            registration
                .OnMessage<TestContact>("3a2b1c0d-9e8f-4a7b-8c6d-5e4f3a2b1c0d", "pil_DoSomething")
                .PreOperation()
                .Asynchronous()
                .WithFilteringAttributes(contact => contact.FirstName);
        }
    }

    private sealed class BothImagePlugin(string unsecureConfig, string secureConfig)
        : PluginBase(unsecureConfig, secureConfig)
    {
        public override void Register(IPluginRegistration registration)
        {
            registration
                .OnUpdate<TestContact>("4a3b2c1d-0e9f-4a8b-8c7d-6e5f4a3b2c1d")
                .PostOperation()
                .Synchronous()
                .WhenChanged(contact => contact.FirstName)
                .WithBothImage("5a4b3c2d-1e0f-4a9b-8c8d-7e6f5a4b3c2d", "target", contact => contact.FirstName);
        }
    }

    private sealed class ImageOptionsPlugin(string unsecureConfig, string secureConfig)
        : PluginBase(unsecureConfig, secureConfig)
    {
        public override void Register(IPluginRegistration registration)
        {
            registration
                .OnMessage<TestContact>("6a5b4c3d-2e1f-4a0b-8c9d-8e7f6a5b4c3d", "Merge")
                .PreOperation()
                .Synchronous()
                .WithImage(
                    new PluginImageOptions("7a6b5c4d-3e2f-4a1b-8c0d-9e8f7a6b5c4d", PluginImageType.PreImage, "subordinate")
                    {
                        EntityAlias = "subordinate",
                        MessagePropertyName = "SubordinateId",
                    },
                    contact => contact.FirstName);
        }
    }

    private sealed class DuplicateImageKeyPlugin(string unsecureConfig, string secureConfig)
        : PluginBase(unsecureConfig, secureConfig)
    {
        public override void Register(IPluginRegistration registration)
        {
            registration
                .OnUpdate<TestContact>("8a7b6c5d-4e3f-4a2b-8c1d-0e9f8a7b6c5d")
                .PreOperation()
                .Synchronous()
                .WhenChanged(contact => contact.FirstName)
                .WithPreImage("9a8b7c6d-5e4f-4a3b-8c2d-1e0f9a8b7c6d", "target", contact => contact.FirstName)
                .WithPreImage("0b9a8c7d-6e5f-4a4b-8c3d-2e1f0b9a8c7d", "target", contact => contact.LastName);
        }
    }

    private sealed class SharedImageKeyPlugin(string unsecureConfig, string secureConfig)
        : PluginBase(unsecureConfig, secureConfig)
    {
        public override void Register(IPluginRegistration registration)
        {
            registration
                .OnUpdate<TestContact>("1c0b9a8d-7e6f-4a5b-8c4d-3e2f1c0b9a8d")
                .PostOperation()
                .Synchronous()
                .WhenChanged(contact => contact.FirstName)
                .WithPreImage("2d1c0b9a-8e7f-4a6b-8c5d-4e3f2d1c0b9a", "target", contact => contact.FirstName)
                .WithPostImage("3e2d1c0b-9a8f-4a7b-8c6d-5e4f3e2d1c0b", "target", contact => contact.FirstName);
        }
    }
}

[EntityLogicalName("contact")]
internal sealed class TestContact : Entity
{
    public TestContact()
        : base("contact")
    {
    }

    [AttributeLogicalName("firstname")]
    public string FirstName
    {
        get => GetAttributeValue<string>("firstname");
        set => SetAttributeValue("firstname", value);
    }

    [AttributeLogicalName("lastname")]
    public string LastName
    {
        get => GetAttributeValue<string>("lastname");
        set => SetAttributeValue("lastname", value);
    }
}
