# Plugin Deployment Acceptance Scenarios

This document defines the expected end-to-end behavior for Dataverse plugin deployment from the framework developer perspective.

## Main Developer Scenario

As a developer, I install or reference the Dataverse Plugin Framework package.

The package provides deployment tooling under a `tools` folder, including a simple batch entry point such as:

```bat
DeployPlugins.bat
```

The developer configures local Dataverse access through a short local profile outside the repository:

```bat
set "DV_PAC=ContosoDev"
set "DV_CONN=AuthType=ClientSecret;Url=https://dev.crm4.dynamics.com;ClientId=...;ClientSecret=...;TenantId=..."
```

Then the developer runs:

```bat
DeployPlugins.bat
```

The script should:

1. find the compiled plugin assembly,
2. discover plugin classes with `public static void Register(IPluginRegistration registration)`,
3. generate the manifest,
4. validate the manifest,
5. push the plugin assembly/package through PAC CLI when configured,
6. read the current Dataverse registration state,
7. compare Dataverse state with the manifest,
8. apply the required create/update/delete operations,
9. write a clear deployment log.

## Source of Truth

The source of truth is always the C# plugin implementation type.

For example:

```csharp
public sealed class ContactPlugin : PluginBase
{
    public static void Register(IPluginRegistration registration)
    {
        registration
            .OnUpdate<Contact>("5072086e-1508-f111-8407-000d3ab261ac")
            .PreOperation()
            .Synchronous()
            .WhenChanged(c => c.FirstName, c => c.LastName)
            .WithPreImage(
                "7f8a44bb-4d4f-4cd9-9e22-efb1472a1001",
                "PreImage",
                c => c.FirstName,
                c => c.LastName);
    }
}
```

All Dataverse registration records for this plugin type must match this definition.

## One-to-One Rule

For every plugin type discovered from the assembly:

```text
Dataverse registration state for plugin type == C# Register(...) manifest for plugin type
```

This means:

- a step present in C# but missing in Dataverse must be created,
- a step present in both but different must be updated,
- a step present in Dataverse for the same plugin type but missing in C# must be deleted,
- an image present in C# but missing in Dataverse must be created,
- an image present in both but different must be updated,
- an image present in Dataverse for a managed step but missing in C# must be deleted.

## Cleanup Scope

Cleanup must be strictly scoped.

The deployment tool may delete only records that belong to plugin types discovered from the deployed assembly and managed by the framework manifest.

The deployment tool must not delete:

- plugin steps for other plugin types,
- plugin steps for plugin types not present in the current assembly,
- plugin steps without framework ownership evidence,
- unrelated manually registered Dataverse plugins outside the current deployment scope.

## Plugin Type Boundary

The deployment boundary is the Dataverse `plugintype` record that corresponds to the C# plugin implementation type.

The tool must resolve the `plugintype` by `typename` after PAC plugin push has completed.

For every resolved plugin type, the tool must read all existing Dataverse steps connected to that plugin type, not only steps that are already known by manifest IDs.

This is required to detect stale steps that were removed from C#.

## Step Ownership

A Dataverse step is considered in scope when:

- its `plugintypeid` matches a plugin type discovered from the assembly, and
- the plugin type has a valid `Register(IPluginRegistration registration)` method.

Optional future hardening: add a framework marker into supported metadata/configuration if Dataverse offers a reliable place for it.

## Image Ownership

A Dataverse image is considered in scope when:

- it belongs to an in-scope Dataverse step.

Images are not independently owned. Their lifecycle follows the owning step.

## Required Log Output

The deployment log must clearly show what happened.

Example:

```text
Plugin type: Contoso.Plugins.ContactPlugin

Steps:
  CREATE  5072086e-1508-f111-8407-000d3ab261ac Update contact PreOperation Sync
  UPDATE  5056ef4c-0e08-f111-8407-000d3ab261ac Update contact PreValidation Sync
  DELETE  13fd7baa-421a-4b21-9d1a-e5e5a5ad0001 Update contact PostOperation Async
  OK      4e56ef4c-0e08-f111-8407-000d3ab261ac Create contact PreValidation Sync

Images:
  CREATE  7f8a44bb-4d4f-4cd9-9e22-efb1472a1001 PreImage on step 5072086e-1508-f111-8407-000d3ab261ac
  UPDATE  c7876278-a6f7-4b87-a47c-0e9ecb391002 PostImage on step 5072086e-1508-f111-8407-000d3ab261ac
  DELETE  96a9c69a-5a40-48dd-9d23-5b3d76bd0001 OldImage on step 5072086e-1508-f111-8407-000d3ab261ac
```

## Safety Rules

The tool must fail before applying changes when:

- manifest validation fails,
- plugin type cannot be resolved after PAC push,
- SDK message cannot be resolved,
- SDK message filter cannot be resolved for a message/entity combination,
- synchronous Update step has no filtering attributes,
- step/image IDs look like placeholders,
- confirmation policy requires confirmation and `--confirm` is missing.

## NuGet Tooling Expectation

The NuGet package should include deployment tooling in a predictable location.

Expected shape:

```text
/tools
  DeployPlugins.bat
  plugin-deployment/*.bat
  pillaro-dv or install instructions for pillaro-dv
```

`DeployPlugins.bat` should be the friendly entry point.

It should call the lower-level scripts internally.

## Current Implementation Gap

The current implementation already supports:

- registration API,
- manifest generation,
- manifest validation,
- PAC plugin push integration,
- reading Dataverse state by manifest IDs,
- create/update step and image metadata.

The current implementation still needs:

- friendly `DeployPlugins.bat`,
- NuGet packaging of tools,
- reading all existing steps by plugin type,
- delete detection for stale steps/images,
- applying delete operations in plugin-type scope,
- final log formatting using CREATE/UPDATE/DELETE/OK terminology.
