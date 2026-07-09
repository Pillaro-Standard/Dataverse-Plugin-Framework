# Pillaro Framework Solution

This folder contains the **Pillaro Framework** Power Platform solution.

Install this solution before installing the [Pillaro Plugin Framework Examples solution](../examples/README.md) or before running framework-based plugins that rely on the model-driven app, runtime settings, autonumbering, or framework logs.

The framework solution is the runtime/admin layer for the examples. It provides:

- the **Pillaro Plugin Framework** model-driven app,
- Runtime Settings,
- Autonumberings,
- Plugin Logs,
- framework security roles.

The examples solution does not replace this solution. It only adds example plugin registrations and scenarios that use the framework features installed from this folder.

## Solution files

| File | Use when |
|---|---|
| `PillaroFramework_1_0_0_1_managed.zip` | You want to install the framework into an environment. This is the recommended option for most users. |
| `PillaroFramework_1_0_0_1.zip` | You need the unmanaged solution for development or inspection. |

## Versions and releases

All versions and release changes are documented in the [changelog.md](./changelog.md) file.

## Required runtime setting

- **MinimalSeverityLevel** (Int) = `0` — enables full debug-level framework logging.

`MinimalSeverityLevel` is a minimum severity threshold. Use `3` as the recommended production default. Use `0` or `1` only temporarily in production when full diagnostics are required.

## Security roles

The framework solution includes the following security roles with appropriate access to framework entities (Runtime Setting and Plugin Log):

- **System Administrator** — full access to all framework entities  
- **System Customizer** — full access to all framework entities  
- **Pillaro Log Reader** — read-only access to Plugin Log records  
- **Pillaro Setting Manager** — full access to Runtime Setting records  
- **Pillaro Setting Reader** — read-only access to Runtime Setting records  

Ensure that users or service accounts executing plugins have at least one of these roles assigned, or create custom roles with equivalent privileges.

## Minimal deployment checklist

- Import `PillaroFramework_1_0_0_1_managed.zip` into Dataverse.
- Confirm that the **Pillaro Plugin Framework** app is available.
- Configure the `MinimalSeverityLevel` runtime setting to `0` or `1` for full debug-level logging, or `3` for the recommended production default.
- Ensure appropriate security roles are assigned to users or service accounts.
- If you want to run the repository examples, continue with the [examples solution quick start](../examples/README.md).

> Note: The repository examples will not behave as intended until this framework solution is installed and the runtime setting above is configured.
