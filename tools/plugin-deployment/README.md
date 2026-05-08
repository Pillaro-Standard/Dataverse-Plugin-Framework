# Plugin Deployment Batch Scripts

These scripts provide a Windows-friendly developer and pipeline entry point for Dataverse plugin registration deployment.

They follow the same practical idea as spkl scripts:

- developers can keep local credentials outside the repository,
- CI/CD pipelines can pass credentials through secure environment variables,
- Visual Studio and DevOps can call the same commands.

## Deployment Behavior

Plugin deployment synchronizes Dataverse registration metadata with the C# registration manifest.

For every plugin type discovered from the deployed assembly:

```text
Dataverse registration state for plugin type == C# Register(...) manifest for plugin type
```

This means:

- steps defined in C# and missing in Dataverse are created,
- steps defined in both places but different are updated,
- steps existing in Dataverse but no longer defined in C# are deleted,
- images defined in C# and missing in Dataverse are created,
- images defined in both places but different are updated,
- images existing in Dataverse for in-scope steps but no longer defined in C# are deleted.

Delete operations are scoped to plugin types discovered from the deployed assembly. The scripts must not affect other plugin types or unrelated manually registered plugins.

## Scripts

| Script | Purpose |
|---|---|
| `plugin-manifest.bat` | Generates plugin deployment manifest from compiled plugin assembly. |
| `plugin-validate.bat` | Validates plugin manifest. |
| `plugin-diff.bat` | Compares manifest with Dataverse and shows create/update/delete changes. |
| `plugin-deploy.bat` | Deploys assembly and synchronizes scoped plugin registration metadata. |
| `_load-profile.bat` | Internal credential/profile loader. |
| `_run-pillaro-dv.bat` | Internal CLI runner. |
| `local-profile.template.cmd` | Template for local developer credentials. |

## Recommended Local Flow

Use this flow during local development:

```bat
call tools\plugin-deployment\plugin-manifest.bat src\Contoso.Plugins\bin\Debug\net462\Contoso.Plugins.dll artifacts\plugin-manifest.json
call tools\plugin-deployment\plugin-validate.bat artifacts\plugin-manifest.json
call tools\plugin-deployment\plugin-diff.bat artifacts\plugin-manifest.json
call tools\plugin-deployment\plugin-deploy.bat src\Contoso.Plugins\bin\Debug\net462\Contoso.Plugins.dll artifacts\plugin-manifest.json ContosoCore
```

Do not skip `plugin-diff.bat` when removing or renaming steps/images. The diff is the review point before Dataverse is synchronized.

## Local Developer Setup

Create the local profiles folder:

```bat
mkdir %USERPROFILE%\.pillaro\dataverse-plugin-framework\profiles
```

Copy the profile template:

```bat
copy tools\plugin-deployment\local-profile.template.cmd %USERPROFILE%\.pillaro\dataverse-plugin-framework\profiles\default.cmd
```

Edit:

```text
%USERPROFILE%\.pillaro\dataverse-plugin-framework\profiles\default.cmd
```

Set your local development Dataverse URL and credentials.

The profile file is intentionally stored outside the repository.

## Selecting a Local Profile

Default profile:

```bat
set PILLARO_DV_PROFILE=default
```

Named profile:

```bat
set PILLARO_DV_PROFILE=customer-dev
```

This loads:

```text
%USERPROFILE%\.pillaro\dataverse-plugin-framework\profiles\customer-dev.cmd
```

## Local Usage

Generate manifest:

```bat
tools\plugin-deployment\plugin-manifest.bat src\Contoso.Plugins\bin\Debug\net462\Contoso.Plugins.dll artifacts\plugin-manifest.json
```

Validate manifest:

```bat
tools\plugin-deployment\plugin-validate.bat artifacts\plugin-manifest.json
```

Diff Dataverse:

```bat
tools\plugin-deployment\plugin-diff.bat artifacts\plugin-manifest.json
```

Deploy:

```bat
tools\plugin-deployment\plugin-deploy.bat src\Contoso.Plugins\bin\Debug\net462\Contoso.Plugins.dll artifacts\plugin-manifest.json ContosoCore
```

## Visual Studio Post-Build Example

```bat
call "$(SolutionDir)tools\plugin-deployment\plugin-manifest.bat" "$(TargetPath)" "$(SolutionDir)artifacts\plugin-manifest.json"
call "$(SolutionDir)tools\plugin-deployment\plugin-diff.bat" "$(SolutionDir)artifacts\plugin-manifest.json"
call "$(SolutionDir)tools\plugin-deployment\plugin-deploy.bat" "$(TargetPath)" "$(SolutionDir)artifacts\plugin-manifest.json" "ContosoCore"
```

## Pipeline Usage

In CI/CD, do not use local profiles. Set Dataverse connection values through secure pipeline variables.

Required pipeline variables depend on the selected authentication mode. At minimum, configure:

```text
PILLARO_DV_AUTH_MODE
PILLARO_DV_AUTH_TYPE
PILLARO_DV_URL
PILLARO_DV_SOLUTION
```

Then call the same scripts:

```bat
call tools\plugin-deployment\plugin-manifest.bat src\Contoso.Plugins\bin\Release\net462\Contoso.Plugins.dll artifacts\plugin-manifest.json
call tools\plugin-deployment\plugin-validate.bat artifacts\plugin-manifest.json
call tools\plugin-deployment\plugin-diff.bat artifacts\plugin-manifest.json
call tools\plugin-deployment\plugin-deploy.bat src\Contoso.Plugins\bin\Release\net462\Contoso.Plugins.dll artifacts\plugin-manifest.json %PILLARO_DV_SOLUTION%
```

## Azure DevOps YAML Example

```yaml
steps:
- script: dotnet build src/Contoso.Plugins/Contoso.Plugins.csproj -c Release
  displayName: Build plugins

- script: tools\plugin-deployment\plugin-manifest.bat src\Contoso.Plugins\bin\Release\net462\Contoso.Plugins.dll artifacts\plugin-manifest.json
  displayName: Generate plugin manifest

- script: tools\plugin-deployment\plugin-validate.bat artifacts\plugin-manifest.json
  displayName: Validate plugin manifest

- script: tools\plugin-deployment\plugin-diff.bat artifacts\plugin-manifest.json
  displayName: Diff Dataverse plugin registration

- script: tools\plugin-deployment\plugin-deploy.bat src\Contoso.Plugins\bin\Release\net462\Contoso.Plugins.dll artifacts\plugin-manifest.json $(DataverseSolutionName)
  displayName: Deploy Dataverse plugins
```

## Required CLI

The scripts expect a `pillaro-dv` CLI command on PATH.

Override it with:

```bat
set PILLARO_DV_CLI=C:\tools\pillaro-dv\pillaro-dv.exe
```

## Credential Variables

### ClientSecret

```text
PILLARO_DV_URL
PILLARO_DV_AUTH_TYPE=ClientSecret
PILLARO_DV_TENANT_ID
PILLARO_DV_CLIENT_ID
PILLARO_DV_CLIENT_SECRET
```

### ConnectionString

```text
PILLARO_DV_AUTH_TYPE=ConnectionString
PILLARO_DV_CONNECTION_STRING
```

### Interactive

```text
PILLARO_DV_URL
PILLARO_DV_AUTH_TYPE=Interactive
```

Interactive mode is intended only for local development.
