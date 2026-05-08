# Plugin Deployment Flow

This document describes how the static plugin registration API is used for Dataverse deployment.

The registration API itself does not deploy anything. It produces deterministic deployment metadata that the deployment tool can validate, compare with Dataverse and apply.

## Core Principle: Scoped Synchronization

Plugin deployment is a scoped synchronization between source code and Dataverse.

The source of truth is the static registration method on each plugin type:

```csharp
public static void Register(IPluginRegistration registration)
```

For every plugin type discovered from the deployed assembly, the deployment tool makes Dataverse match the manifest generated from C#:

```text
Dataverse registration state for plugin type == C# Register(...) manifest for plugin type
```

This means:

- missing steps are created,
- changed steps are updated,
- stale steps are deleted,
- missing images are created,
- changed images are updated,
- stale images are deleted.

Delete operations are intentionally part of deployment, but only inside the current plugin type scope.

## Deployment Scope

The deployment boundary is the Dataverse `plugintype` record that corresponds to the C# plugin implementation type.

The tool may modify only:

- plugin steps connected to plugin types discovered from the deployed assembly,
- plugin images connected to those in-scope steps.

The tool must not modify:

- plugin steps for other plugin types,
- plugin steps for plugin types not present in the current assembly,
- unrelated manually registered Dataverse plugins outside the current deployment scope,
- unrelated solution components.

## PAC First

The deployment tooling must not duplicate logic already provided by Microsoft Power Platform CLI.

Use `pac` for:

- authentication profiles and environment selection,
- local developer login,
- non-interactive authentication in automation,
- plugin assembly or plugin package push,
- solution import/export/pack/unpack where applicable,
- solution component operations where `pac` supports the required scenario.

Use Pillaro CLI only for framework-specific behavior:

- discovering `public static void Register(IPluginRegistration registration)`,
- generating the plugin registration manifest,
- validating Pillaro registration policy,
- calculating the registration diff,
- applying scoped step and image synchronization,
- producing reviewable deployment output for developers and pipelines.

## High-Level Flow

```text
plugin assembly
  ↓
pillaro-dv plugin manifest
  ↓
pillaro-dv plugin validate
  ↓
pillaro-dv plugin diff
  ↓
pac plugin push / pac solution ... where applicable
  ↓
pillaro-dv synchronizes scoped step/image metadata
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
        .WhenChanged(c => c.FirstName, c => c.LastName)
        .WithPreImage(
            "7f8a44bb-4d4f-4cd9-9e22-efb1472a1001",
            "PreImage",
            c => c.FirstName,
            c => c.LastName);
}
```

The `stepId` is expected to match Dataverse `SdkMessageProcessingStepId`.

The image ID is expected to match Dataverse `SdkMessageProcessingStepImageId`.

## Deployment Tool Responsibilities

The Pillaro deployment tool is responsible for:

1. loading the compiled plugin assembly,
2. discovering all static `Register(IPluginRegistration registration)` methods,
3. building a manifest from descriptors,
4. validating the manifest,
5. delegating auth and supported deployment operations to `pac`,
6. resolving Dataverse plugin types by `typename`,
7. reading all existing steps for every in-scope plugin type,
8. reading all images for every in-scope step,
9. calculating a scoped diff,
10. applying create/update/delete operations for steps and images,
11. adding created/updated components to the configured solution,
12. writing deployment logs.

## Recommended CLI Commands

Generate and validate the manifest:

```bash
pillaro-dv plugin manifest \
  --assembly ./bin/Release/net462/Contoso.Plugins.dll \
  --output ./artifacts/plugin-manifest.json

pillaro-dv plugin validate \
  --manifest ./artifacts/plugin-manifest.json
```

Use local PAC auth profile:

```bash
pac auth create --environment https://org.crm4.dynamics.com
pac auth name --name ContosoDev
```

Run diff using the active PAC profile:

```bash
pillaro-dv plugin diff \
  --manifest ./artifacts/plugin-manifest.json \
  --auth-type PacCli
```

Run diff using a named PAC profile:

```bash
pillaro-dv plugin diff \
  --manifest ./artifacts/plugin-manifest.json \
  --auth-type PacCli \
  --pac-auth-profile ContosoDev
```

Deploy:

```bash
pillaro-dv plugin deploy \
  --manifest ./artifacts/plugin-manifest.json \
  --assembly ./bin/Release/net462/Contoso.Plugins.dll \
  --auth-type PacCli \
  --pac-auth-profile ContosoDev \
  --solution ContosoCore
```

## Delegation to PAC CLI

### Authentication

Pillaro CLI should use `pac auth` as the preferred authentication model. It can select a profile with:

```bash
pac auth select --name ContosoDev
```

and verify the active profile with:

```bash
pac auth who
```

### Plugin Assembly / Package Push

Where possible, assembly/package upload should be delegated to:

```bash
pac plugin push --pluginId <plugin-assembly-or-package-id> --pluginFile <path> --type Assembly
```

If `--environment` is omitted, PAC uses the active organization from the current auth profile.

### Solution Operations

Where possible, solution import/export/pack/unpack/component operations should be delegated to `pac solution ...` commands instead of being reimplemented.

Pillaro CLI should only fall back to Dataverse SDK calls for registration metadata operations that are not exposed by PAC CLI in a usable form.

## Deployment Order

The deploy flow applies changes in this order:

1. verify PAC auth profile,
2. generate/validate manifest,
3. push plugin assembly/package through `pac plugin push` when applicable,
4. resolve plugin types by `typename`,
5. read all existing Dataverse steps for in-scope plugin types,
6. read all images for in-scope steps,
7. calculate registration diff,
8. delete stale images,
9. delete stale steps,
10. create/update sdk message processing steps,
11. create/update step images,
12. add created/updated components to the configured solution.

Images are deleted before steps so Dataverse step dependencies are removed safely.

## Dataverse Tables

Direct Dataverse access should be limited to what is not covered by PAC CLI. The deploy engine may still need to read or update:

- `plugintype`
- `sdkmessage`
- `sdkmessagefilter`
- `sdkmessageprocessingstep`
- `sdkmessageprocessingstepimage`

Avoid direct custom handling of authentication, solution packaging, solution import/export and assembly/package push when PAC CLI supports the scenario.

## Diff Rules

The diff identifies:

- whether PAC assembly/package push is required,
- plugin type presence after PAC push,
- step create/update/delete,
- image create/update/delete,
- missing Dataverse records,
- stale Dataverse records inside the current plugin type scope,
- deployment policy steps that require confirmation.

## Safe Defaults

Recommended defaults:

- `diff` is read-only,
- `deploy` synchronizes in-scope steps and images,
- delete operations are allowed only inside the resolved plugin type scope,
- production deploy should go through CI/CD approvals,
- steps with `RequiresConfirmation` cannot be deployed silently,
- local authentication should default to PAC CLI profile instead of storing secrets in repository files.

## DevOps Usage

A CI/CD pipeline should:

1. build the plugin assembly,
2. create/select a PAC auth profile using secure pipeline credentials,
3. generate the manifest,
4. validate the manifest,
5. publish the manifest as a build artifact,
6. run a Dataverse diff,
7. delegate assembly/package push to PAC CLI,
8. synchronize scoped registration metadata through Pillaro CLI,
9. deploy to TEST/UAT/PROD only through approvals.

Example pipeline shape:

```yaml
steps:
- script: dotnet build src/Contoso.Plugins/Contoso.Plugins.csproj -c Release
  displayName: Build plugins

- script: pac auth create --environment $(DataverseUrl) <secure-auth-options>
  displayName: Authenticate PAC CLI

- script: pillaro-dv plugin manifest --assembly src/Contoso.Plugins/bin/Release/net462/Contoso.Plugins.dll --output artifacts/plugin-manifest.json
  displayName: Generate plugin manifest

- script: pillaro-dv plugin validate --manifest artifacts/plugin-manifest.json
  displayName: Validate plugin manifest

- script: pillaro-dv plugin diff --manifest artifacts/plugin-manifest.json --auth-type PacCli
  displayName: Diff Dataverse plugin registration

- script: pillaro-dv plugin deploy --manifest artifacts/plugin-manifest.json --assembly src/Contoso.Plugins/bin/Release/net462/Contoso.Plugins.dll --auth-type PacCli --solution $(SolutionName)
  displayName: Deploy Dataverse plugins
```

## Visual Studio Usage

For local development, Visual Studio can call the same CLI command after build. The developer should already have a selected PAC profile:

```bash
pac auth create --environment https://dev.crm4.dynamics.com
pac auth name --name ContosoDev
```

Then Visual Studio can call:

```bash
pillaro-dv plugin deploy \
  --assembly $(TargetPath) \
  --manifest $(SolutionDir)/artifacts/plugin-manifest.json \
  --auth-type PacCli \
  --pac-auth-profile ContosoDev \
  --solution ContosoCore
```

The important rule is that Visual Studio and DevOps must use the same manifest generation and deploy flow. Local deployment should not be a separate implementation.

## First Implementation Slice

The first deploy implementation should support:

- manifest generation from compiled assembly,
- validation of duplicate step IDs and image IDs,
- PAC CLI authentication profile verification,
- assembly/package push delegated to PAC CLI where possible,
- Dataverse diff for all in-scope plugin type steps and images,
- create/update/delete step synchronization,
- create/update/delete image synchronization.
