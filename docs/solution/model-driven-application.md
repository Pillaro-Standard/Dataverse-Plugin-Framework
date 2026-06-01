# Model-Driven Application

## Overview

The framework includes a model-driven application called Pillaro Plugin Framework that provides runtime management and diagnostics capabilities for plugin-based solutions. The application offers a centralized interface for monitoring plugin execution, managing runtime configuration, and controlling autonumbering sequences.

---

## Key Features

### 📊 Dashboards

The application provides interactive dashboards that display:

- **Plugin Execution Overview** — summary of plugin activity and performance metrics
- **Error Trends** — visualization of failures and exceptions over time
- **Configuration Status** — current runtime settings and their values

Dashboards help administrators and developers quickly assess system health and identify potential issues.

---

### 🔍 Diagnostics and Logging

The application includes dedicated views for accessing diagnostic information:

- **Plugin Logs** — detailed execution logs captured during plugin runtime
- **Error Records** — structured error information including stack traces and context
- **Execution History** — timeline of plugin invocations and their outcomes

These views support troubleshooting, performance analysis, and audit requirements.

---

### ⚙️ Runtime Settings

Runtime Settings allow you to configure plugin behavior without redeploying code.

- **Centralized Configuration** — manage settings from a single location
- **Dynamic Loading** — plugins can read settings at runtime using `SettingsService`
- **Environment-Specific** — maintain different settings per environment (Dev, Test, Production)

Common use cases:
- Feature flags (enable/disable functionality)
- Thresholds and limits
- Integration endpoints and credentials
- Logging verbosity levels

See [Runtime Configuration](../plugins/configuration.md) for detailed implementation guidance.

---

### 🔢 Autonumbering

The Autonumbering feature provides flexible number sequence generation for business entities.

- **Custom Sequences** — define patterns like `INV-{YYYY}-{0000}`
- **Entity-Specific** — separate sequences per entity or business scenario
- **Thread-Safe** — guaranteed uniqueness in concurrent environments
- **Prefix/Suffix Support** — combine static and dynamic components

Plugins can generate numbers using the autonumbering service:

See [Autonumbering](../plugins/autonumbering.md) for setup and usage details.

---

## Installation

The Pillaro Plugin Framework application is included in the **Power Platform Solution Framework** package located in the `power-platform-solutions/framework` directory.

### Deployment Steps

1. **Locate the solution package**  
   Navigate to `power-platform-solutions/framework` in the repository

2. **Import the solution**  
   - Open your Dataverse environment
   - Navigate to **Solutions**
   - Select **Import** and upload the solution file
   - Follow the import wizard

3. **Verify installation**  
   - Confirm the Pillaro Plugin Framework application appears in your app list
   - Assign appropriate security roles (see [Security Roles](#security-roles))

4. **Configure required runtime settings**  
   - Open the Pillaro Plugin Framework application
   - Navigate to **Runtime Settings**
   - Create the required `MinimalSeverityLevel` setting (Key: `MinimalSeverityLevel`, Type: `Int`, Value: `0`)
   - This setting enables debug-level logging for development environments

   > [!NOTE]
   > For complete deployment requirements and additional configuration details, see the [Framework Deployment Guide](../../power-platform-solutions/framework/README.md).

---

## Security Roles

The framework solution includes predefined security roles for managing access to application features:

| Role | Access Level | Description |
|------|--------------|-------------|
| **System Administrator** | Full access | Complete control over all framework entities |
| **System Customizer** | Full access | Complete control over all framework entities |
| **Pillaro Log Reader** | Read-only | View plugin logs and execution history |
| **Pillaro Setting Manager** | Full access | Manage runtime settings and autonumbering |
| **Pillaro Setting Reader** | Read-only | View runtime settings (cannot modify) |

Assign roles based on user responsibilities:
- **Developers** → Pillaro Log Reader (for diagnostics)
- **Administrators** → Pillaro Setting Manager (for configuration)
- **Auditors** → Pillaro Log Reader (for compliance)

---

## Usage Scenarios

### Scenario 1: Monitor Plugin Execution

1. Open the **Pillaro Plugin Framework** application
2. Navigate to **Dashboards** → **Plugin Execution Overview**
3. Review execution counts, success rates, and performance metrics
4. Drill into **Plugin Logs** for detailed execution records

### Scenario 2: Configure Feature Flags

1. Open the **Pillaro Plugin Framework** application
2. Navigate to **Runtime Settings**
3. Create a new setting (e.g., `EnableNewFeature`, type `Boolean`, value `true`)
4. Plugin code reads this setting and adjusts behavior accordingly

### Scenario 3: Troubleshoot Errors

1. Open the **Pillaro Plugin Framework** application
2. Navigate to **Diagnostics** → **Error Records**
3. Filter by date range or entity type
4. Review stack traces and context information
5. Correlate errors with plugin logs for root cause analysis

### Scenario 4: Manage Number Sequences

1. Open the **Pillaro Plugin Framework** application
2. Navigate to **Autonumbering**
3. Create or edit a sequence definition
4. Set pattern, prefix, current value, and increment
5. Plugins generate numbers using the configured sequence

---

## Best Practices

### Logging Configuration by Environment

Configure the `MinimalSeverityLevel` runtime setting according to the minimum severity that should be saved.

The value works as a threshold. The configured severity and all higher severities are saved.

| MinimalSeverityLevel | Saved severities | Typical use |
|---:|---|---|
| `1` or lower | `Debug`, `Info`, `Warning`, `Error` | Full diagnostic logging. Useful for development, testing, initial setup, or temporary deep diagnostics. |
| `2` | `Info`, `Warning`, `Error` | Informational logging without Debug details. Useful for test or controlled support scenarios. |
| `3` | `Warning`, `Error` | Recommended default for production environments. |
| `4` | `Error` | Error-only logging. Useful when production log volume must be kept minimal. |

Recommended defaults:

- **Development/Test environments** — use `0` or `1` when full diagnostic visibility is needed.
- **Production environments** — use `3` as the recommended default.
- **High-volume production environments** — use `4` if only errors should be retained.
- **Temporary production diagnostics** — use `0` or `1` only when full diagnostic visibility is required for investigation.

> [!WARNING]
> Full logging (`MinimalSeverityLevel = 0` or `1`) can generate a large amount of diagnostic data.
> In production environments, this may negatively affect performance and increase Dataverse storage usage.
> Full logging is not recommended for normal production operation. Enable it only temporarily when detailed diagnostics are required, and increase the level again after the investigation is finished.

Use only values from `0` to `4` for normal configuration. Values higher than `4` are not recommended because no standard framework severity is higher than `Error`.

### Use Logs Instead of Debugging

Instead of attaching debuggers to plugin processes:

- Add diagnostic log entries at key execution points
- Include relevant context (entity ID, values, decision points)
- Review execution flow through the **Plugin Logs** view in the application
- Use correlation IDs to trace related operations

This approach is faster, works in all environments (including production), and provides historical execution data.

---

## Support

For questions, issues, or feature requests related to the Pillaro Plugin Framework application:

- 💬 [Discussions](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/discussions)
- 🐛 [Issues](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/issues)

## ➡️ Related documents

- [Runtime Configuration](../plugins/configuration.md) — how to use `SettingsService` in plugins
- [Autonumbering](../plugins/autonumbering.md) — detailed autonumbering setup and patterns
- [Logging](../plugins/logging.md) — plugin logging capabilities and log levels
- [Error Handling](../plugins/error-handling.md) — exception handling and validation failures
