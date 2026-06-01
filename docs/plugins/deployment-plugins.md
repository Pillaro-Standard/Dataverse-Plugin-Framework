# Deployment Plugins

This page describes the standard way to deploy Dataverse plugins built with Pillaro Dataverse Plugin Framework.

The goal is simple: install the framework package, rebuild the plugin project, configure `PillaroSettings.json`, set a Dataverse connection string environment variable, and run the generated deployment script.

> [!IMPORTANT]
> Deployment tools are generated after the plugin project is rebuilt.
> If you do not see the `Tools` folder or `PillaroSettings.json` in Visual Studio, rebuild the plugin project first, then enable **Show All Files**, find the generated files, and choose **Include In Project**.

---

## Prerequisites

Before deploying plugins, make sure that plugin registration attributes are configured in code.

The deployment process uses these attributes as the source of truth for plugin step registration. If the attributes are missing or incomplete, the deployment tool cannot correctly create or update plugin steps in Dataverse.

The deployment process reads registration metadata such as message, stage, mode, entity name, filtering attributes, images, rank, configuration, and solution membership from the plugin registration attributes.

See [Plugin Registration API](./plugin-registration-api.md) for details about how to configure plugin registration attributes.

---

## 1. Install the Package

Install `Pillaro.Dataverse.PluginFramework` into the Dataverse plugin project.

After the package is installed, rebuild the plugin project. The build creates:

```text
PillaroSettings.json
Tools/
  Deployment/
    DeployPlugins.bat
    DeployPlugins.ps1
    README.md
```

Include the generated `Tools` folder and `PillaroSettings.json` in the project so they are visible in Visual Studio and can be committed with the solution.

---

## 2. Configure PillaroSettings.json

`PillaroSettings.json` lives in the plugin project root.

Example:

```json
{
  "schemaVersion": 1,
  "solution": "MyDataverseSolution",
  "dataverse": {
    "connectionStringEnvironmentVariable": "DV_CONN"
  },
  "defaultProfile": "debug",
  "profiles": {
    "debug": {
      "pluginAssemblyPath": "bin/Debug/MySolution.Plugins.dll"
    },
    "release": {
      "pluginAssemblyPath": "bin/Release/MySolution.Plugins.dll"
    },
    "ci": {
      "pluginAssemblyPath": "${BUILD_BINARIESDIRECTORY}/MySolution.Plugins.dll"
    }
  }
}
```

Required values:

| Field | Description |
|---|---|
| `solution` | Dataverse solution unique name. Deployed plugin components are added to this solution. |
| `dataverse.connectionStringEnvironmentVariable` | Name of the environment variable containing the Dataverse connection string. |
| `defaultProfile` | Profile used by direct CLI runs when `--profile` is omitted. The generated wrapper scripts default to the `debug` profile unless a profile is passed. |
| `profiles.<name>.pluginAssemblyPath` | Path to the built plugin DLL for the selected profile. |

Use `debug` for local development, `release` for local release-build verification, and `ci` for Azure DevOps. Set each `pluginAssemblyPath` to the DLL path produced by your project. Classic .NET Framework plugin projects commonly use `bin/Debug/<assembly>.dll`; SDK-style projects may include a target framework folder such as `bin/Debug/net462/<assembly>.dll`.

---

## 3. Set the Connection String

The deployment script reads the Dataverse connection string from an environment variable.

PowerShell example for the current terminal session:

```powershell
$env:DV_CONN = 'AuthType=ClientSecret;Url=https://your-org.crm4.dynamics.com;ClientId=your-client-id;ClientSecret=your-client-secret;TenantId=your-tenant-id'
```

PowerShell example for the current Windows user:

```powershell
[Environment]::SetEnvironmentVariable(
    'DV_CONN',
    'AuthType=ClientSecret;Url=https://your-org.crm4.dynamics.com;ClientId=your-client-id;ClientSecret=your-client-secret;TenantId=your-tenant-id',
    'User')
```

Restart Visual Studio or the terminal after setting a user-level variable.

Do not commit connection strings into source control.

---

## 4. Deploy Locally

Build or rebuild the plugin project first.

From the plugin project root, run:

```bat
.\Tools\Deployment\DeployPlugins.bat
```

The batch wrapper accepts an optional profile as the first argument and defaults to `debug`:

```bat
.\Tools\Deployment\DeployPlugins.bat release
```

Or run the PowerShell wrapper:

```powershell
.\Tools\Deployment\DeployPlugins.ps1 -Profile debug
```

For a release build:

```powershell
.\Tools\Deployment\DeployPlugins.ps1 -Profile release
```

The script deploys the plugin assembly and synchronizes plugin registration metadata defined in code.

The generated wrappers are the recommended entry point. Direct CLI usage is `pillaro-dv deploy [options]` and is configured by `PillaroSettings.json`; supported options are `--settings`, `--profile`, `--just-assembly`, and `-h`/`--help`. The solution unique name comes from `solution`, the plugin assembly path comes from `profiles.<profile>.pluginAssemblyPath`, and the Dataverse connection string variable comes from `dataverse.connectionStringEnvironmentVariable`.

---

## 5. Deploy from Azure DevOps

Store the Dataverse connection string as a secret variable, for example `DataverseConnectionString`, and map it to the environment variable expected by `PillaroSettings.json`.

Example:

```yaml
- powershell: |
    dotnet build .\MySolution.Plugins\MySolution.Plugins.csproj -c Release
    .\MySolution.Plugins\Tools\Deployment\DeployPlugins.ps1 -Profile ci
  displayName: Deploy Dataverse plugins
  env:
    DV_CONN: $(DataverseConnectionString)
```

The `ci` profile should point to the plugin DLL produced by the pipeline build.

---

## Troubleshooting

| Problem | What to check |
|---|---|
| `Tools/Deployment` is missing | Rebuild the plugin project, enable **Show All Files**, then include the generated `Tools` folder. |
| `PillaroSettings.json` is missing | Rebuild the plugin project and include the generated file. |
| Assembly not found | Check `pluginAssemblyPath` for the selected profile and rebuild the plugin project. |
| Connection string not found | Check that the environment variable name matches `dataverse.connectionStringEnvironmentVariable`. |
| Visual Studio cannot see the variable | Restart Visual Studio after setting a user-level environment variable. |

## ➡️ Related documents

- [Plugin Registration API](./plugin-registration-api.md) - Configure plugin registration metadata in code.
- [Getting Started](./getting-started.md) - Build the first deployable plugin assembly.
- [CI/CD Pipelines](../ci-cd-pipelines.md) - Run build, package, and deployment automation from pipelines.
