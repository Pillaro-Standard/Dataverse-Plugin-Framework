# Plugin Registration API

This document describes the fluent registration API for Dataverse plugin deployment metadata.

The goal is to keep plugin registration readable for developers while preserving deterministic 1:1 identifiers with Dataverse records.

## Design Goals

- registration is visible directly in the plugin class
- framework plugins can override `Register(IPluginRegistration registration)` to provide deployment metadata
- one plugin class can define multiple Dataverse steps
- one Dataverse step can define multiple images
- step and image IDs are explicit and match Dataverse IDs
- IntelliSense guides the developer through valid registration order
- filtering attributes can be declared for Create and Update steps
- attributes can be selected either by logical-name constants or by typed early-bound entity properties

## Example

The examples above use `Guid.Empty` placeholders intentionally. Replace them with real non-empty Dataverse step and image IDs before running deployment validation.

```csharp
using Pillaro.Dataverse.PluginFramework.PluginRegistrations;
using Pillaro.Dataverse.PluginFramework.Plugins;

public sealed class ContactPlugin : PluginBase
{
    public ContactPlugin(string unsecureConfig, string secureConfig)
        : base(unsecureConfig, secureConfig)
    {
        RegisterTask<ValidateNames>(PluginStage.Prevalidation, ["Create", "Update"], Contact.EntityLogicalName, PluginMode.Synchronous);
        RegisterTask<UpdateAddressLabel>(PluginStage.Preoperation, ["Create", "Update"], Contact.EntityLogicalName, PluginMode.Synchronous);
    }

    public override void Register(IPluginRegistration registration)
    {
        registration
            .OnCreate<Contact>("00000000-0000-0000-0000-000000000000")
            .PreValidation()
            .Synchronous()
            .Rank(1)
            .InSolution("MyDataverseSolution");

        registration
            .OnUpdate<Contact>("00000000-0000-0000-0000-000000000000")
            .PreValidation()
            .Synchronous()
            .Rank(1)
            .InSolution("MyDataverseSolution")
            .WhenChanged(Contact.Fields.FirstName, Contact.Fields.LastName);

        registration
            .OnUpdate<Contact>("00000000-0000-0000-0000-000000000000")
            .PreOperation()
            .Synchronous()
            .Rank(1)
            .InSolution("MyDataverseSolution")
            .WhenChanged(
                Contact.Fields.FirstName,
                Contact.Fields.LastName,
                Contact.Fields.Address1_Line1,
                Contact.Fields.Address1_Line2,
                Contact.Fields.Address1_Line3,
                Contact.Fields.Address1_City,
                Contact.Fields.Address1_PostalCode,
                Contact.Fields.Address1_StateOrProvince,
                Contact.Fields.Address1_Country)
            .WithPreImage(
                "00000000-0000-0000-0000-000000000000",
                "image",
                Contact.Fields.Address1_Line1,
                Contact.Fields.Address1_Line2,
                Contact.Fields.Address1_Line3,
                Contact.Fields.Address1_City,
                Contact.Fields.Address1_PostalCode,
                Contact.Fields.Address1_StateOrProvince,
                Contact.Fields.Address1_Country);
    }
}
```

`RegisterTask(...)` calls in the constructor are runtime task registration. `Register(IPluginRegistration registration)` is deployment metadata only. Both are intentionally separate: the constructor tells the framework what task to execute at runtime, while `Register(...)` tells deployment tooling which Dataverse steps and images should exist. Developers must keep these two declarations aligned.

## Attribute Selection Modes

There are two supported ways to select Dataverse attributes.

### Text / Logical Name Constants

Use this mode when you already have generated logical-name constants or when you need a simple string-based fallback.

```csharp
registration
    .OnUpdate<Contact>("00000000-0000-0000-0000-000000000000")
    .PreOperation()
    .Synchronous()
    .InSolution("MyDataverseSolution")
    .WhenChanged(
        Contact.Fields.FirstName,
        Contact.Fields.LastName)
    .WithPreImage(
        "00000000-0000-0000-0000-000000000000",
        "PreImage",
        Contact.Fields.FirstName,
        Contact.Fields.LastName);
```

### Typed Early-Bound Properties

Use this mode when you want IntelliSense over the early-bound entity type. The registration API reads `AttributeLogicalNameAttribute` from the selected property.

```csharp
registration
    .OnUpdate<Contact>("00000000-0000-0000-0000-000000000000")
    .PreOperation()
    .Synchronous()
    .InSolution("MyDataverseSolution")
    .WhenChanged(
        c => c.FirstName,
        c => c.LastName)
    .WithPreImage(
        "00000000-0000-0000-0000-000000000000",
        "PreImage",
        c => c.FirstName,
        c => c.LastName);
```

Typed selection is available for `OnUpdate<TEntity>(...)` steps. It keeps the entity type from `OnUpdate<Contact>(...)` through the fluent chain, so only properties from `Contact` are offered by IntelliSense.

## Filtering Attributes

Filtering attributes can be declared for Create and Update steps in this framework registration metadata.

For Create steps, use `WithFilteringAttributes(...)`:

```csharp
registration
    .OnCreate<Contact>("00000000-0000-0000-0000-000000000000")
    .PreValidation()
    .Synchronous()
    .Rank(1)
    .InSolution("MyDataverseSolution")
    .WithFilteringAttributes(Contact.Fields.FirstName, Contact.Fields.LastName);
```

For Update steps, use `WhenChanged(...)` or `WithFilteringAttributes(...)`. `WhenChanged(...)` is preferred for readability and keeps the entity-specific fluent flow:

```csharp
registration
    .OnUpdate<Contact>("00000000-0000-0000-0000-000000000000")
    .PreOperation()
    .Synchronous()
    .InSolution("MyDataverseSolution")
    .WhenChanged(Contact.Fields.FirstName, Contact.Fields.LastName);
```

## Multiple Images Per Step

A step can have multiple images. Each image has its own Dataverse `SdkMessageProcessingStepImageId`.

```csharp
registration
    .OnUpdate<Contact>("00000000-0000-0000-0000-000000000000")
    .PreOperation()
    .Synchronous()
    .InSolution("MyDataverseSolution")
    .WhenChanged(c => c.FirstName, c => c.LastName)
    .WithPreImage(
        "00000000-0000-0000-0000-000000000000",
        "PreImage",
        c => c.FirstName,
        c => c.LastName)
    .WithPostImage(
        "00000000-0000-0000-0000-000000000000",
        "PostImage",
        c => c.FirstName,
        c => c.LastName);
```

The fluent chain keeps every image attached to the exact step where it is declared. No additional linking attribute or registration ID is needed.

## Runtime vs Deployment Metadata

The constructor remains responsible for runtime task registration:

```csharp
RegisterTask<UpdateAddressLabel>(PluginStage.Preoperation, ["Create", "Update"], Contact.EntityLogicalName, PluginMode.Synchronous);
```

The plugin `Register` method is responsible for deployment metadata only:

```csharp
public override void Register(IPluginRegistration registration)
{
    registration
        .OnUpdate<Contact>("00000000-0000-0000-0000-000000000000")
        .PreOperation()
        .Synchronous()
        .InSolution("MyDataverseSolution")
        .WhenChanged(Contact.Fields.FirstName, Contact.Fields.LastName);
}
```

Deployment tooling discovers framework plugins from `PluginBase`, calls `Register(...)`, and builds a deterministic manifest without executing the plugin pipeline.

These declarations are intentionally separate because they serve different Dataverse concerns. `RegisterTask(...)` controls runtime dispatch inside the plugin execution pipeline. `Register(IPluginRegistration registration)` controls deployment metadata for assemblies, steps, images, filtering attributes, configuration, and solution membership. The framework does not automatically infer one from the other, so keep runtime task registration and deployment metadata aligned when adding, removing, or changing steps.

## Discovery

Use `PluginRegistrationDiscovery` to read registration metadata:

```csharp
var descriptor = PluginRegistrationDiscovery.Discover<ContactPlugin>();
```

Or scan a whole assembly:

```csharp
var descriptors = PluginRegistrationDiscovery.DiscoverFromAssembly(typeof(ContactPlugin).Assembly);
```

## Validation Rules

The deployment manifest validator enforces basic safety rules:

- `stepId` must be a non-empty GUID and should be the Dataverse `SdkMessageProcessingStepId`.
- image IDs must be non-empty GUIDs and should be Dataverse `SdkMessageProcessingStepImageId` values.
- placeholder-looking GUIDs such as `00000000-0000-0000-0000-000000000001` are rejected.
- synchronous Update steps on an entity should define filtering attributes; `WhenChanged(...)` is preferred for readability and typed update flow.
- filtering attributes are supported for Create and Update steps. Use `WithFilteringAttributes(...)` for Create steps; use `WhenChanged(...)` or `WithFilteringAttributes(...)` for Update steps.
- image names must be unique within a step.
- image IDs must be unique across the manifest.
- images should be used only in PreOperation or PostOperation stages.
- Create steps cannot define pre-images.
- Delete steps cannot define post-images.

## Notes

- a step can define multiple pre-images and/or post-images when the Dataverse step supports them.
- entity logical names are read from `EntityLogicalNameAttribute` on early-bound entity classes.
- typed attribute selection reads logical names from `AttributeLogicalNameAttribute` on early-bound entity properties.
- custom API and custom action messages can be registered with `OnMessage(...)`.
