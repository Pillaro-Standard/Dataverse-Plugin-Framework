# Pillaro Plugin Framework Examples Solution

This folder contains the ready-to-import **Pillaro Plugin Framework Examples** Power Platform solution.

Use this solution when you want to try the Pillaro Dataverse Plugin Framework in a real Dataverse environment without building your own plugin project first. It installs example plugin registrations for standard Dataverse tables such as **Contact** and **Task**, so you can create or update records and immediately see how validation, runtime settings, autonumbering, task execution, and framework logging work together.

The examples are intended for learning, demos, and framework validation. They are not intended to be installed directly into production environments.

## How the solutions fit together

There are two Power Platform solutions in this repository:

| Solution | Folder | Purpose |
|---|---|---|
| **Pillaro Framework** | [`../framework`](../framework/README.md) | Required runtime/admin solution. It provides the Pillaro Plugin Framework app, Runtime Settings, Autonumberings, Plugin Logs, and security roles. |
| **Pillaro Plugin Framework Examples** | this folder | Example plugins and registrations that run on top of the framework solution. |

Install the **Pillaro Framework** solution first. The examples solution depends on it. Without the framework solution, the example plugins cannot use the framework runtime configuration, autonumbering, or logging features.

Read the framework installation notes here: [`../framework/README.md`](../framework/README.md).

## Example solution files

| File | Use when |
|---|---|
| `PillaroPluginFrameworkExamples_1_0_0_0_managed.zip` | You want to install and try the examples in an environment. This is the recommended option for most users. |
| `PillaroPluginFrameworkExamples_1_0_0_0.zip` | You need the unmanaged solution for development or inspection. |

## What the examples demonstrate

| Area | What to try | Expected behavior |
|---|---|---|
| Runtime settings | Create or update a Contact with a forbidden first name or last name. | The plugin reads `ForbiddenWords` from Runtime Settings and blocks invalid names. |
| Contact update logic | Create or update a Contact address. | The plugin builds the Contact `Address 1: Name` value from the address fields. |
| Autonumbering | Create a Task. | The plugin generates a number and prefixes the Task subject. |
| Related record sync | Create or update a Task regarding a Contact. | The plugin updates the Contact description with the latest planned or completed activity dates. |
| Logging | Run any example scenario. | Plugin execution details are stored in the Pillaro Plugin Framework logs. |

## Quick start

### 1. Install the framework prerequisite

Install the **Pillaro Framework** solution from [`../framework`](../framework/README.md) first:

1. Open Power Apps.
2. Go to **Solutions**.
3. Import `../framework/PillaroFramework_1_0_0_1_managed.zip`.
4. Confirm that the **Pillaro Plugin Framework** model-driven app is available.

This prerequisite provides the Runtime Settings, Autonumberings, Plugin Logs, and security roles used by the examples.

### 2. Install the examples solution

Import the managed examples solution:

```text
PillaroPluginFrameworkExamples_1_0_0_0_managed.zip
```

After import, the environment contains the example plugin assembly and plugin step registrations. The scenarios below are now ready to run after you configure the required framework records.

### 3. Configure runtime settings

Open the **Pillaro Plugin Framework** app and go to **Runtime Settings**.

Create these settings:

| Key | Type | Value | Purpose |
|---|---|---|---|
| `MinimalSeverityLevel` | `Int` | `0` or `1` | Enables full debug-level framework logging while you try the examples. |
| `ForbiddenWords` | `JSON` | `["Admin","Test"]` | Values blocked by the Contact name validation example. |

For normal production use of the framework, `MinimalSeverityLevel = 3` is the recommended default. For examples and demos, `0` or `1` makes it easier to see all log output.

### 4. Configure autonumbering for Task examples

In the **Pillaro Plugin Framework** app, open **Autonumberings** and create this record:

| Field | Value |
|---|---|
| **Entity System Name** | `Task` |
| **Last Used Number {NUM}** | `1000` |
| **Number of Digits** | `6` |
| **Format** | `{date1}-{NUM}` |
| **Date 1 Format {date1}** | `yy-MM-dd` |

This enables generated Task numbers such as `26-04-07-001000`.

### 5. Run the Contact examples

Create or update a Contact:

1. Create a Contact with a normal first name and last name.
2. Update one of the address fields, such as **Street 1**, **City**, or **ZIP/Postal Code**.
3. Save the Contact.
4. Check that **Address 1: Name** was generated from the address fields.

Then try the validation scenario:

1. Create or update a Contact.
2. Set **First Name** or **Last Name** to `Admin` or `Test`.
3. Save the Contact.
4. The save should be blocked by the validation plugin.

### 6. Run the Task examples

Create a Task regarding a Contact:

1. Open an existing Contact.
2. Add a Task related to that Contact.
3. Set a subject and a due date.
4. Save the Task.
5. The Task subject should be prefixed with the generated autonumber.
6. Open the Contact again and check the **Description** field for the latest planned activity date.

To test completed activity sync, complete a related Task and check the Contact **Description** again.

### 7. Review plugin logs

Open the **Pillaro Plugin Framework** app and go to **Plugin Logs**.

You should see log records from the example plugin executions. Open a log record to review execution messages and details written by the framework tasks.

If you do not see logs, verify that:

- the **Pillaro Framework** solution is installed,
- `MinimalSeverityLevel` exists in Runtime Settings,
- the user running the examples has access to the framework log entities,
- the Contact or Task change matches one of the registered example steps.

## Notes

- Install the framework solution before installing the examples solution. It is a required prerequisite, not an optional add-on.
- Use the managed examples solution unless you specifically need the unmanaged source solution.
- The examples use standard Dataverse Contact and Task records.
- These examples are for demonstration and testing only.
