# Framework — Deployment required

This framework must be deployed to a Dataverse environment for the examples and any custom code in this repository to work correctly. Deployment ensures runtime components (registered plugins and required solution artifacts) are available to execute and validate logic.

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

- Import or install the solution into Dataverse and register plugins.
- Configure the `MinimalSeverityLevel` runtime setting to `0` or `1` for full debug-level logging, or `3` for the recommended production default.
- Ensure appropriate security roles are assigned to users or service accounts.

> Note: The repository examples and plugins will not behave as intended until the framework is deployed and the runtime setting above is applied.
