# Plugin Deployment Flow

This document describes how the static plugin registration API can be used for Dataverse deployment.

The registration API itself does not deploy anything. It produces deterministic deployment metadata that a deployment tool can validate, compare with Dataverse and apply.

## High-Level Flow

```text
plugin assembly
  ↓
reflection discovery
  ↓
registration descriptor
  ↓
validation
  ↓
manifest
  ↓
diff against Dataverse
  ↓
apply changes or fail with clear errors
```

## Source of Truth

The source of truth is the plugin class static registration method:

```csharp
public static void Register(IPluginRegistration registration)
{
    registration
        .OnUpdate<Contact>("5072086e-1508-f111-8407-000d3ab261ac")
        .PreOperation()
        .Synchronous()
        .Rank(1)
        .WhenChanged(Contact.Fields.FirstName, Contact.Fields.LastName)
        .WithPreImage(
            "00000000-0000-0000-0000-000000000001",
            "PreImage",
            Contact.Fields.FirstName,
            Contact.Fields.LastName);
}
```

The `stepId` is expected to match Dataverse `SdkMessageProcessingStepId`.

The image ID is expected to match Dataverse `SdkMessageProcessingStepImageId`.

## Deployment Tool Responsibilities

A deployment tool should be responsible for:

1. loading the compiled plugin assembly,
2. discovering all static `Register(IPluginRegistration registration)` methods,
3. building a manifest from descriptors,
4. validating the manifest,
5. loading the current Dataverse state,
6. calculating a diff,
7. applying the diff in a deterministic order,
8. writing deployment logs.

## Recommended CLI Commands

```bash
pillaro-dv plugin manifest \
  --assembly ./bin/Release/net462/Contoso.Plugins.dll \
  --output ./artifacts/plugin-manifest.json
```

```bash
pillaro-dv plugin validate \
  --manifest ./artifacts/plugin-manifest.json
```

```bash
pillaro-dv plugin diff \
  --manifest ./artifacts/plugin-manifest.json \
  --environment https://org.crm4.dynamics.com
```

```bash
pillaro-dv plugin deploy \
  --manifest ./artifacts/plugin-manifest.json \
  --assembly ./bin/Release/net462/Contoso.Plugins.dll \
  --environment https://org.crm4.dynamics.com \
  --solution ContosoCore
```

## Deployment Order

The deploy engine should apply changes in this order:

1. plugin assembly,
2. plugin types,
3. sdk message processing steps,
4. step images,
5. solution component membership,
6. optional cleanup of unmanaged records that are no longer present in the manifest.

Cleanup should be explicit and never enabled by default.

## Dataverse Tables

The deploy engine will normally work with these Dataverse tables:

- `pluginassembly`
- `plugintype`
- `sdkmessage`
- `sdkmessagefilter`
- `sdkmessageprocessingstep`
- `sdkmessageprocessingstepimage`
- `solutioncomponent`

## Manifest Shape

The registration descriptor can be serialized into a deployment manifest similar to this:

```json
{
  "pluginAssembly": {
    "name": "Contoso.Plugins",
    "path": "./bin/Release/net462/Contoso.Plugins.dll"
  },
  "plugins": [
    {
      "typeName": "Contoso.Plugins.ContactPlugin",
      "steps": [
        {
          "stepId": "5072086e-1508-f111-8407-000d3ab261ac",
          "messageName": "Update",
          "entityName": "contact",
          "stage": 20,
          "mode": 0,
          "rank": 1,
          "filteringAttributes": [
            "firstname",
            "lastname"
          ],
          "images": [
            {
              "imageId": "00000000-0000-0000-0000-000000000001",
              "type": "PreImage",
              "name": "PreImage",
              "attributes": [
                "firstname",
                "lastname"
              ]
            }
          ]
        }
      ]
    }
  ]
}
```

## Diff Rules

The diff should identify:

- assembly create/update,
- plugin type create/update,
- step create/update,
- image create/update,
- missing Dataverse records,
- unmanaged records that are not present in the manifest,
- destructive changes that require confirmation.

## Safe Defaults

Recommended defaults:

- `diff` is read-only,
- `deploy` does not delete anything,
- `cleanup` requires explicit command,
- production deploy must go through CI/CD approvals,
- destructive changes require confirmation,
- steps with `RequiresConfirmation` cannot be deployed silently.

## DevOps Usage

A CI/CD pipeline should:

1. build the plugin assembly,
2. generate the manifest,
3. validate the manifest,
4. publish the manifest as a build artifact,
5. run a Dataverse diff,
6. deploy to DEV/Sandbox automatically if allowed,
7. deploy to TEST/UAT/PROD only through approvals.

Example pipeline shape:

```yaml
- script: dotnet build src/Contoso.Plugins/Contoso.Plugins.csproj -c Release
  displayName: Build plugin assembly

- script: pillaro-dv plugin manifest --assembly src/Contoso.Plugins/bin/Release/net462/Contoso.Plugins.dll --output artifacts/plugin-manifest.json
  displayName: Generate plugin manifest

- script: pillaro-dv plugin validate --manifest artifacts/plugin-manifest.json
  displayName: Validate plugin manifest

- script: pillaro-dv plugin diff --manifest artifacts/plugin-manifest.json --environment $(DataverseUrl)
  displayName: Diff Dataverse plugin registration

- script: pillaro-dv plugin deploy --manifest artifacts/plugin-manifest.json --assembly src/Contoso.Plugins/bin/Release/net462/Contoso.Plugins.dll --environment $(DataverseUrl) --solution $(SolutionName)
  displayName: Deploy Dataverse plugins
```

## Visual Studio Usage

For local development, Visual Studio can call the same CLI command after build:

```bash
pillaro-dv plugin deploy \
  --assembly $(TargetPath) \
  --environment https://dev.crm4.dynamics.com \
  --solution ContosoCore
```

The important rule is that Visual Studio and DevOps must use the same manifest generation and deploy engine. Local deployment should not be a separate implementation.

## First Implementation Slice

The first deploy implementation should support:

- manifest generation from compiled assembly,
- validation of duplicate step IDs and image IDs,
- Dataverse diff for existing steps and images,
- create/update step,
- create/update images,
- no cleanup by default.

Assembly upload and solution membership can be added next, or delegated to existing tooling in the first iteration.
