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
- filtering attributes and images can be declared for every message, not only `Update`
- attributes can be selected either by logical-name constants or by typed early-bound entity properties
- entity registration supports both early-bound types and logical name strings

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
            .Rank(1);

        registration
            .OnUpdate<Contact>("00000000-0000-0000-0000-000000000000")
            .PreValidation()
            .Synchronous()
            .Rank(1)
            .WhenChanged(Contact.Fields.FirstName, Contact.Fields.LastName);

        registration
            .OnUpdate<Contact>("00000000-0000-0000-0000-000000000000")
            .PreOperation()
            .Synchronous()
            .Rank(1)
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

## Entity Registration Modes

Plugin steps can be registered using two different approaches for specifying the target entity.

### Early-Bound Type Registration

Use generic methods with early-bound entity types when you have generated entity classes. This approach provides compile-time type safety and IntelliSense support:

~~~csharp
registration
    .OnCreate<Contact>("4e56ef4c-0e08-f111-8407-000d3ab261ac")
    .PreValidation()
    .Synchronous();

registration
    .OnUpdate<Contact>("5056ef4c-0e08-f111-8407-000d3ab261ac")
    .PreValidation()
    .Synchronous()
    .WhenChanged(c => c.FirstName, c => c.LastName);

registration
    .OnDelete<Contact>("6056ef4c-0e08-f111-8407-000d3ab261ac")
    .PreOperation()
    .Synchronous();

registration
    .OnMessage<Contact>("7056ef4c-0e08-f111-8407-000d3ab261ac", "MyCustomAction")
    .PreOperation()
    .Synchronous();
~~~

The entity logical name is automatically extracted from the `EntityLogicalNameAttribute` on the early-bound type.

### String-Based Logical Name Registration

Use string-based overloads when:
- early-bound classes don't exist for the entity
- registering steps for custom entities without generated types
- working with entities dynamically
- you prefer explicit logical names over generic type parameters

~~~csharp
registration
    .OnCreate("contact", "4e56ef4c-0e08-f111-8407-000d3ab261ac")
    .PreValidation()
    .Synchronous();

registration
    .OnUpdate("contact", "5056ef4c-0e08-f111-8407-000d3ab261ac")
    .PreValidation()
    .Synchronous()
    .WhenChanged("firstname", "lastname");

registration
    .OnDelete("contact", "6056ef4c-0e08-f111-8407-000d3ab261ac")
    .PreOperation()
    .Synchronous();

registration
    .OnMessage("contact", "7056ef4c-0e08-f111-8407-000d3ab261ac", "MyCustomAction")
    .PreOperation()
    .Synchronous();
~~~

With string-based registration:
- The first parameter is always the entity logical name (e.g., "contact", "account", "new_customentity")
- The second parameter is always the step ID
- For `OnMessage`, the third parameter is the message name
- Filtering attributes and image attributes must be specified as strings (typed expressions are not available)
- The fluent API returns non-generic builder interfaces

Both registration modes generate identical deployment metadata. Choose the mode that best fits your project structure and entity availability.

## Attribute Selection Modes

There are two supported ways to select Dataverse attributes.

### Text / Logical Name Constants

Use this mode when you already have generated logical-name constants or when you need a simple string-based fallback.

```csharp
registration
    .OnUpdate<Contact>("00000000-0000-0000-0000-000000000000")
    .PreOperation()
    .Synchronous()
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
    .WhenChanged(
        c => c.FirstName,
        c => c.LastName)
    .WithPreImage(
        "00000000-0000-0000-0000-000000000000",
        "PreImage",
        c => c.FirstName,
        c => c.LastName);
```

Typed selection is available for every entity-typed entry point - `OnCreate<TEntity>(...)`, `OnUpdate<TEntity>(...)`, `OnDelete<TEntity>(...)` and `OnMessage<TEntity>(...)`. The entity type flows through the fluent chain, so only properties from `Contact` are offered by IntelliSense.

If you need to generate these types for your solution, see [Early-Bound Entity Generation](./early-bound-generation.md).

## Filtering Attributes

Filtering attributes are stored on `sdkmessageprocessingstep.filteringattributes`, which Dataverse accepts for any message. They can be declared on every step this API can register, with either `WithFilteringAttributes(...)` or its alias `WhenChanged(...)`, and with either string constants or typed expressions.

For Create steps:

~~~csharp
// Early-bound with constants
registration
    .OnCreate<Contact>("00000000-0000-0000-0000-000000000000")
    .PreValidation()
    .Synchronous()
    .Rank(1)
    .WithFilteringAttributes(Contact.Fields.FirstName, Contact.Fields.LastName);

// Early-bound with typed expressions
registration
    .OnCreate<Contact>("00000000-0000-0000-0000-000000000000")
    .PreValidation()
    .Synchronous()
    .Rank(1)
    .WithFilteringAttributes(c => c.FirstName, c => c.LastName);

// String-based
registration
    .OnCreate("contact", "00000000-0000-0000-0000-000000000000")
    .PreValidation()
    .Synchronous()
    .Rank(1)
    .WithFilteringAttributes("firstname", "lastname");
~~~

For Update steps, use `WhenChanged(...)` or `WithFilteringAttributes(...)`. `WhenChanged(...)` is preferred for readability:

~~~csharp
// Early-bound with constants
registration
    .OnUpdate<Contact>("00000000-0000-0000-0000-000000000000")
    .PreOperation()
    .Synchronous()
    .WhenChanged(Contact.Fields.FirstName, Contact.Fields.LastName);

// Early-bound with typed expressions
registration
    .OnUpdate<Contact>("00000000-0000-0000-0000-000000000000")
    .PreOperation()
    .Synchronous()
    .WhenChanged(c => c.FirstName, c => c.LastName);

// String-based
registration
    .OnUpdate("contact", "00000000-0000-0000-0000-000000000000")
    .PreOperation()
    .Synchronous()
    .WhenChanged("firstname", "lastname");
~~~

## Images

A step can have multiple images. Each image has its own Dataverse `SdkMessageProcessingStepImageId`.

Which images a step can carry follows the Dataverse rules, and the manifest validator enforces them:

| Message | Pre-image | Post-image | `Both` |
| --- | --- | --- | --- |
| `Create` | not available - the record does not exist yet | PostOperation only | not available |
| `Update` | any stage | PostOperation only | PostOperation only |
| `Delete` | any stage | not available - the record is gone | not available |
| `Assign`, `Route`, `Merge`, `SetState`, `Send`, `DeliverIncoming`, `DeliverPromote` | any stage | PostOperation only | PostOperation only |

Images are keyed by entity alias, which defaults to the image name. A key must be unique within the pre-image collection and within the post-image collection, but the same key may appear in both - the plugin reads them from `PreEntityImages` and `PostEntityImages` separately.

Images are not available for `MainOperation()` (Custom API) registrations.

~~~csharp
// Early-bound with typed expressions
registration
    .OnUpdate<Contact>("00000000-0000-0000-0000-000000000000")
    .PostOperation()
    .Synchronous()
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

// String-based
registration
    .OnUpdate("contact", "00000000-0000-0000-0000-000000000000")
    .PostOperation()
    .Synchronous()
    .WhenChanged("firstname", "lastname")
    .WithPreImage(
        "00000000-0000-0000-0000-000000000000",
        "PreImage",
        "firstname",
        "lastname")
    .WithPostImage(
        "00000000-0000-0000-0000-000000000000",
        "PostImage",
        "firstname",
        "lastname");
~~~

The fluent chain keeps every image attached to the exact step where it is declared. No additional linking attribute or registration ID is needed.

### `Both` Images

`WithBothImage(...)` registers a single image with Dataverse image type `Both`, which the platform exposes through `PreEntityImages` and `PostEntityImages` at once. It is valid only where both halves are valid, that is a PostOperation step on a message other than `Create` or `Delete`:

~~~csharp
registration
    .OnUpdate<Contact>("00000000-0000-0000-0000-000000000000")
    .PostOperation()
    .Synchronous()
    .WhenChanged(c => c.FirstName)
    .WithBothImage(
        "00000000-0000-0000-0000-000000000000",
        "target",
        c => c.FirstName,
        c => c.LastName);
~~~

### Entity Alias And Message Property Name

`WithImage(PluginImageOptions)` is the full form. Use it when the key in `PreEntityImages`/`PostEntityImages` should differ from the image name, or when the message exposes the record under more than one request property and the derived default is not the one you need:

~~~csharp
registration
    .OnMessage<Account>("00000000-0000-0000-0000-000000000000", "Merge")
    .PreOperation()
    .Synchronous()
    .WithImage(
        new PluginImageOptions("00000000-0000-0000-0000-000000000000", PluginImageType.PreImage, "subordinate")
        {
            EntityAlias = "subordinate",
            MessagePropertyName = "SubordinateId",
        },
        a => a.Name);
~~~

Both properties are optional. Left unset, the alias falls back to the image name and the message property name is derived from the step message.

## Custom API MainOperation Registration

A Custom API main operation is associated with the plugin type through `CustomAPI.PluginTypeId` in Dataverse. It must not be registered as a normal `SdkMessageProcessingStep` - Dataverse rejects such a step for Custom API messages.

Declare the handler with `OnMessage(...)` and the `MainOperation()` stage:

~~~csharp
public sealed class InvoicePlugin : PluginBase
{
    public const string MessageName = "pil_CopyInvoice";

    public InvoicePlugin(string unsecureConfig, string secureConfig)
        : base(unsecureConfig, secureConfig)
    {
        RegisterTask<CopyInvoiceTask>(PluginStage.Mainoperation, [MessageName], Invoice.EntityLogicalName, PluginMode.Synchronous);
    }

    public override void Register(IPluginRegistration registration)
    {
        registration
            .OnMessage<Invoice>("00000000-0000-0000-0000-000000000000", MessageName)
            .MainOperation()
            .Synchronous();
    }
}
~~~

During deployment, a MainOperation registration keeps the plugin type in the manifest so the assembly and plugin type are deployed and updated, but no `SdkMessageProcessingStep` is created, updated, or deleted for it. Associate the Custom API with the plugin type by setting `CustomAPI.PluginTypeId` (typically as part of the solution that defines the Custom API). The deployment diff output marks these registrations as `[TYPE-ONLY]`.

MainOperation registrations cannot define images and are supported only for Custom API messages - the validator rejects `MainOperation()` combined with the platform messages `Create`, `Update`, or `Delete`.

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
        .WhenChanged(Contact.Fields.FirstName, Contact.Fields.LastName);
}
```

Deployment tooling discovers framework plugins from `PluginBase`, calls `Register(...)`, and builds a deterministic manifest without executing the plugin pipeline.

These declarations are intentionally separate because they serve different Dataverse concerns. `RegisterTask(...)` controls runtime dispatch inside the plugin execution pipeline. `Register(IPluginRegistration registration)` controls deployment metadata for assemblies, steps, images, filtering attributes, configuration, and solution membership. The framework does not automatically infer one from the other, so keep runtime task registration and deployment metadata aligned when adding, removing, or changing steps.

## Deployment connection

Plugin registration attributes describe how plugin steps should be registered in Dataverse. They define registration metadata directly in code, but they do not deploy the plugin by themselves.

To deploy the plugin steps into Dataverse, use the deployment process described in [Deployment Plugins](./deployment-plugins.md).

Before deployment, make sure that all required registration attributes are configured correctly, especially message, stage, mode, entity name, filtering attributes, and required images.

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
- filtering attributes are supported for every message. Use `WithFilteringAttributes(...)` or `WhenChanged(...)`.
- image keys (entity alias, defaulting to the image name) must be unique within the pre-image collection and within the post-image collection of a step. The same key may appear in both.
- image IDs must be unique across the manifest.
- post-images are available only in the PostOperation stage; pre-images are valid in PreValidation, PreOperation and PostOperation.
- Create steps cannot define pre-images (nor `Both` images, which include a pre-image).
- Delete steps cannot define post-images (nor `Both` images, which include a post-image).
- MainOperation registrations cannot define images.
- MainOperation registrations are supported only for Custom API messages, not for `Create`, `Update`, or `Delete`.

## Notes

- a step can define multiple pre-images and/or post-images when the Dataverse step supports them.
- `WithBothImage(...)` registers a single image with Dataverse image type `Both`, exposed through both `PreEntityImages` and `PostEntityImages`.
- `WithImage(PluginImageOptions)` is the full form, for a distinct `EntityAlias` or an explicit `MessagePropertyName`.
- entity logical names are read from `EntityLogicalNameAttribute` on early-bound entity classes for generic registration methods.
- string-based registration methods accept entity logical names directly as parameters.
- typed attribute selection reads logical names from `AttributeLogicalNameAttribute` on early-bound entity properties.
- custom API and custom action messages can be registered with `OnMessage(...)` or `OnMessage<TEntity>(...)`.
- image `EntityAlias` defaults to the image name; set it explicitly through `WithImage(PluginImageOptions)` when the key in `PreEntityImages`/`PostEntityImages` should differ from the name.
- image `MessagePropertyName` is derived automatically from the step message during deployment (`Id` for Create, `EntityMoniker` for SetState, `EmailId`/`FaxId`/`TemplateId` for Send depending on the entity, `EmailId` for DeliverIncoming/DeliverPromote, otherwise `Target`). Override it through `WithImage(PluginImageOptions)` for messages that expose the record under more than one property, such as `Merge` (`Target` or `SubordinateId`).
- both early-bound and string-based registration modes generate identical deployment metadata.
- string-based registration validates that the entity logical name is not null, empty, or whitespace.

## ➡️ Related documents

- [Early-Bound Entity Generation](./early-bound-generation.md) - Generate strongly typed Dataverse entity classes.
- [Deployment Plugins](./deployment-plugins.md) - Deploy registered plugin steps into Dataverse.
- [Plugin Model](./plugin-model.md) - Understand how plugin classes and runtime task registration fit together.
- [Plugin Step Configuration](./step-configuration.md) - Configure unsecure and secure values for plugin steps.
