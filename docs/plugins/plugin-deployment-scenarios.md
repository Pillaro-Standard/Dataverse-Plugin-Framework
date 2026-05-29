# Plugin Deployment Acceptance Scenarios

This document defines the expected end-to-end behavior for Dataverse plugin deployment from the framework developer perspective.

## Main Developer Scenario

The developer has:

- a compiled plugin DLL,
- a `PillaroSettings.json` file in the plugin project folder,
- a Dataverse SDK connection string in an environment variable,
- plugin classes inheriting from `PluginBase`,
- plugin classes implementing `Register(IPluginRegistration registration)`.

The developer runs:

```bat
pillaro-dv plugin deploy --settings PillaroSettings.json --confirm
```

Expected result:

1. preflight prints the resolved configuration and prerequisites,
2. the command stops before deployment if anything required is missing,
3. the manifest is generated internally from the compiled DLL,
4. the manifest is validated,
5. the command connects to Dataverse using the configured environment variable,
6. the plugin assembly is created or updated,
7. plugin type records are created when missing,
8. existing plugin type records are reused when present,
9. steps and images are created, updated or deleted to match the registration manifest,
10. supported components are added to the configured solution,
11. the log clearly shows `CREATE`, `UPDATE`, `DELETE` and `OK` actions.

Plugin deployment must not call PAC.

## Required Settings

```json
{
  "solution": "PillaroPluginFrameworkExamples",
  "dataverse": {
    "connectionStringEnvironmentVariable": "DV_CONN"
  },
  "plugins": {
    "assembly": "bin/Debug/Pillaro.Dataverse.PluginFramework.Examples.Plugins.dll"
  }
}
```

Rules:

- `solution` is required.
- `plugins.assembly` is required.
- `dataverse.connectionStringEnvironmentVariable` defaults to `DV_CONN` when omitted.
- Relative paths are resolved from the current working directory.
- No plugin ID is configured.
- No PAC profile is configured for plugin deployment.
- No manifest path is configured for plugin deployment.

## Source of Truth

The source of truth is always the C# plugin implementation type.

For example:

```csharp
public sealed class ContactPlugin : PluginBase
{
    public override void Register(IPluginRegistration registration)
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

## Zero-State Deployment

When the plugin assembly does not exist in Dataverse, deployment must:

- create `pluginassembly`,
- create `plugintype` records for manifest plugin types,
- create all manifest steps,
- create all manifest images,
- add supported components to the configured solution.

The user must not provide a plugin assembly ID.

## Repeat Deployment

When the plugin assembly already exists in Dataverse, deployment must:

- update `pluginassembly.content`,
- reuse existing plugin type records,
- update only changed steps and images,
- leave unchanged steps and images as `OK`,
- complete without duplicate records.

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
- an image present in Dataverse for an in-scope step but missing in C# must be deleted.

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

The tool resolves plugin types during SDK assembly deployment. For every resolved plugin type, the tool reads existing Dataverse steps connected to that plugin type.

This is required to detect stale steps that were removed from C#.

## Image Ownership

A Dataverse image is considered in scope when it belongs to an in-scope Dataverse step.

Images are not independently owned. Their lifecycle follows the owning step.

## Required Log Output

The deployment log must clearly show what happened.

Example:

```text
Plugin assembly:
  CREATE Contoso.Plugins

Plugin types:
  CREATE Contoso.Plugins.ContactPlugin

Steps:
  CREATE 5072086e-1508-f111-8407-000d3ab261ac Contoso.Plugins.ContactPlugin Update contact PreOperation Synchronous
  UPDATE 5056ef4c-0e08-f111-8407-000d3ab261ac Contoso.Plugins.ContactPlugin Update contact PreValidation Synchronous
  DELETE 13fd7baa-421a-4b21-9d1a-e5e5a5ad0001 Contoso.Plugins.ContactPlugin Update contact PostOperation Asynchronous
  OK     4e56ef4c-0e08-f111-8407-000d3ab261ac Contoso.Plugins.ContactPlugin Create contact PreValidation Synchronous

Images:
  CREATE 7f8a44bb-4d4f-4cd9-9e22-efb1472a1001 PreImage 'PreImage' on step 5072086e-1508-f111-8407-000d3ab261ac
  UPDATE c7876278-a6f7-4b87-a47c-0e9ecb391002 PostImage 'PostImage' on step 5072086e-1508-f111-8407-000d3ab261ac
  DELETE 96a9c69a-5a40-48dd-9d23-5b3d76bd0001 PreImage 'OldImage' on step 5072086e-1508-f111-8407-000d3ab261ac
```

## Safety Rules

The tool must fail before applying changes when:

- settings file is missing,
- `solution` is missing,
- `plugins.assembly` is missing,
- plugin assembly file is missing,
- configured Dataverse connection string environment variable is missing,
- manifest validation fails,
- SDK message cannot be resolved,
- SDK message filter cannot be resolved for a message/entity combination,
- synchronous Update step has no filtering attributes,
- step/image IDs look like placeholders,
- confirmation policy requires confirmation and `--confirm` is missing.

## Solution Membership

The deploy command ensures solution membership for:

- plugin assembly,
- SDK message processing steps.

Plugin type records are deployed as part of plugin assembly metadata. Step images are deployed as part of step metadata.

## NuGet Tooling Expectation

The NuGet package should include deployment tooling in a predictable location.

Expected shape:

```text
/tools
  DeployPlugins.bat
  plugin-deployment/*.bat
  pillaro-dv or install instructions for pillaro-dv
```

`DeployPlugins.bat` should be the friendly entry point and should call:

```bat
pillaro-dv plugin deploy --settings PillaroSettings.json --confirm
```
