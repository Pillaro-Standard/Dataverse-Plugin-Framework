# Plugin Registration API

This document describes the proposed fluent registration API for Dataverse plugin deployment metadata.

The goal is to keep plugin registration readable for developers while preserving deterministic 1:1 identifiers with Dataverse records.

## Design Goals

- registration is visible directly in the plugin class
- one plugin class can define multiple Dataverse steps
- step and image IDs are explicit and match Dataverse IDs
- IntelliSense guides the developer through valid registration order
- update filtering attributes are available only for Update steps
- deployment confirmation policy is attached directly to the step it belongs to

## Example

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

    public static void Register(IPluginRegistration registration)
    {
        registration
            .OnCreate<Contact>("4e56ef4c-0e08-f111-8407-000d3ab261ac")
            .PreValidation()
            .Synchronous()
            .Rank(1);

        registration
            .OnUpdate<Contact>("5056ef4c-0e08-f111-8407-000d3ab261ac")
            .PreValidation()
            .Synchronous()
            .Rank(1)
            .WhenChanged(Contact.Fields.FirstName, Contact.Fields.LastName);

        registration
            .OnUpdate<Contact>("5072086e-1508-f111-8407-000d3ab261ac")
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
                "00000000-0000-0000-0000-000000000001",
                "image",
                Contact.Fields.Address1_Line1,
                Contact.Fields.Address1_Line2,
                Contact.Fields.Address1_Line3,
                Contact.Fields.Address1_City,
                Contact.Fields.Address1_PostalCode,
                Contact.Fields.Address1_StateOrProvince,
                Contact.Fields.Address1_Country)
            .RequiresConfirmation(
                PluginRisk.Medium,
                "Synchronous contact update step with pre-image.",
                PluginDeploymentScope.TestAndProduction);
    }
}
```

## Runtime vs Deployment Metadata

The constructor remains responsible for runtime task registration:

```csharp
RegisterTask<UpdateAddressLabel>(PluginStage.Preoperation, ["Create", "Update"], Contact.EntityLogicalName, PluginMode.Synchronous);
```

The static `Register` method is deployment metadata only:

```csharp
public static void Register(IPluginRegistration registration)
```

Deployment tooling can discover this method through reflection and build a deterministic manifest without executing plugin runtime logic.

## Discovery

Use `PluginRegistrationDiscovery` to read registration metadata:

```csharp
var descriptor = PluginRegistrationDiscovery.Discover<ContactPlugin>();
```

Or scan a whole assembly:

```csharp
var descriptors = PluginRegistrationDiscovery.DiscoverFromAssembly(typeof(ContactPlugin).Assembly);
```

## Notes

- `stepId` must be a non-empty GUID and should be the Dataverse `SdkMessageProcessingStepId`.
- image IDs must be non-empty GUIDs and should be Dataverse `SdkMessageProcessingStepImageId` values.
- entity logical names are read from `EntityLogicalNameAttribute` on early-bound entity classes.
- custom API and custom action messages can be registered with `OnMessage(...)`.
