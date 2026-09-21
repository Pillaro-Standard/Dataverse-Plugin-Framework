# Pillaro Dataverse Plugin Framework — end-to-end functional document

**Publisher:** Pillaro Labs s.r.o.
**Offer:** Pillaro Dataverse Plugin Framework
**Offer type:** Dynamics 365 apps on Dataverse and Power Apps
**Listing option:** Get it now (free)
**Document version:** 1.0 (framework solution 1.0.0.2, examples solution 1.0.0.1)

This document is for the Microsoft certification team. It describes what the package
installs, how to configure it, and the end-to-end scenarios that can be verified in a test
environment. Every scenario below can be completed with a single System Administrator user
and standard Dataverse Contact and Task records. No external service, no licence key and no
sign-up are required.

## 1. What the package installs

The Package Deployer package imports two managed solutions, in this order:

| Order | Solution | Version | Role |
|---|---|---|---|
| 1 | Pillaro Framework (`PillaroFramework_1_0_0_2_managed.zip`) | 1.0.0.2 | Runtime and administration layer. Marketplace solution anchor. |
| 2 | Pillaro Plugin Framework Examples (`PillaroPluginFrameworkExamples_1_0_0_1_managed.zip`) | 1.0.0.1 | Example plug-in registrations that exercise the framework. |

The **Pillaro Framework** solution contains:

- the **Pillaro Plugin Framework** model-driven app,
- the **Runtime Setting**, **Autonumbering** and **Plugin Log** tables with their forms and views,
- the framework plug-in assembly and its step registrations,
- four security roles: Pillaro Log Reader, Pillaro Setting Manager, Pillaro Setting Reader,
  plus privileges added to System Administrator and System Customizer.

The **Pillaro Plugin Framework Examples** solution contains the example plug-in assembly and
its step registrations on the out-of-the-box **Contact** and **Task** tables. It adds no
tables, no forms and no site map changes.

Neither solution changes or removes any out-of-the-box site map.

## 2. Security and data handling

For the security validation in the certification checklist:

- **Custom deployment code.** The package contains a `PackageImportExtension` class required
  by Package Deployer. Its `InitializeCustomExtension`, `BeforeImportStage` and
  `AfterPrimaryImport` methods are empty or return `true`. The package runs no custom
  deployment logic and reads no data from the target environment during import.
- **External data sources.** None. All plug-ins run inside the Dataverse sandbox and use only
  `IOrganizationService` against the installing environment.
- **Outbound connections.** None. The offer does not require S2S outbound access or the CRM
  Secure Store.
- **Service accounts.** The package creates no service account and no application user.
- **Customer data.** No data leaves the environment. Plugin Log records are written to the
  installing environment and are never transmitted anywhere.
- **Licensing.** No licence check, no telemetry, no call home. The software is Apache-2.0.

## 3. Prerequisites for the test environment

- A Dataverse environment with a Dynamics 365 or Power Apps licence.
- A user with the System Administrator security role.
- No other Pillaro solution installed.

## 4. Installation

1. Install the package with Package Deployer, or accept the AppSource installation into the
   target environment. Both solutions are imported in the order in the table above.
2. After the import completes, open **Power Apps** and confirm that both solutions are listed
   under **Solutions**.
3. Confirm that the **Pillaro Plugin Framework** model-driven app appears in the app list.

Expected result: two managed solutions installed, one model-driven app available, no import
warnings that block the installation.

## 5. Administrator journey

The administrator configures the framework from the Pillaro Plugin Framework app before the
example scenarios produce visible results.

### 5.1 Runtime settings

Open the **Pillaro Plugin Framework** app and go to **Runtime Settings**. Create two records:

| Key | Type | Value | Purpose |
|---|---|---|---|
| `MinimalSeverityLevel` | Int | `0` | Enables debug-level logging so the certification run sees every log entry. The recommended production value is `3`. |
| `ForbiddenWords` | JSON | `["Admin","Test"]` | Values rejected by the Contact name validation example. |

Expected result: both records save. `MinimalSeverityLevel` is a minimum severity threshold;
lowering it increases the amount of logging.

### 5.2 Autonumbering

Go to **Autonumberings** and create one record:

| Field | Value |
|---|---|
| Entity System Name | `Task` |
| Last Used Number {NUM} | `1000` |
| Number of Digits | `6` |
| Format | `{date1}-{NUM}` |
| Date 1 Format {date1} | `yy-MM-dd` |

Expected result: the record saves and the sequence is ready to issue numbers such as
`26-09-21-001000`.

### 5.3 Security roles

Confirm that the four Pillaro security roles are present under **Settings → Security →
Security roles**. A user holding **Pillaro Log Reader** can read Plugin Log records but not
modify them; a user holding **Pillaro Setting Reader** can read Runtime Settings but not
change them. These roles are optional for the scenarios below, which run as System
Administrator.

## 6. End-to-end scenarios

Each scenario is independent and can be run in any order once section 5 is complete.

### Scenario 1 — Validation driven by a runtime setting

**Steps**

1. Go to a Contact form and create a new Contact.
2. Set **First Name** to `Admin`.
3. Save.

**Expected result**

The save is blocked and the user is shown a validation error. The value came from the
`ForbiddenWords` runtime setting, not from compiled code: adding a word to that setting and
retrying blocks the new word as well, with no redeployment.

**What it demonstrates:** fail-fast validation before execution, and runtime configuration
read from Dataverse.

### Scenario 2 — Business logic on update

**Steps**

1. Create a Contact with an ordinary first name and last name, for example `Jane Doe`.
2. Fill in **Street 1**, **City** and **ZIP/Postal Code**.
3. Save.

**Expected result**

**Address 1: Name** is populated by the plug-in from the address fields.

**What it demonstrates:** a task executing after its validation passes, writing a derived
value back to the record.

### Scenario 3 — Autonumbering

**Steps**

1. Open the Contact created in scenario 2.
2. Add a Task related to that Contact, with a subject and a due date.
3. Save.

**Expected result**

The Task subject is prefixed with a generated number in the configured format, for example
`26-09-21-001000 Call the customer`. Creating a second Task issues the next number in the
sequence; numbers are never reused.

**What it demonstrates:** concurrency-safe sequence generation configured by an
administrator rather than by a developer.

### Scenario 4 — Related record synchronisation

**Steps**

1. With the Task from scenario 3 still open, note its due date.
2. Open the parent Contact.

**Expected result**

The Contact **Description** field contains the latest planned activity date taken from the
related Task. Completing the Task and reopening the Contact updates the description with the
completed activity date.

**What it demonstrates:** a task reacting to a change on one table and updating a related
record deterministically.

### Scenario 5 — Diagnostic logging

**Steps**

1. Open the **Pillaro Plugin Framework** app and go to **Plugin Logs**.
2. Open the most recent record.

**Expected result**

The log lists the plug-in and the tasks that ran for the scenarios above, with the execution
messages written by each task, the input context, the execution time and the execution depth.
A validation failure from scenario 1 appears as its own entry with the reason it failed.

**What it demonstrates:** the diagnostic layer that lets a developer trace a production
execution without attaching a debugger.

## 7. Uninstall

1. In **Power Apps → Solutions**, delete **Pillaro Plugin Framework Examples**.
2. Then delete **Pillaro Framework**.

Expected result: both solutions uninstall in that order. All components installed by the
managed solutions — the app, the three tables and their data, both plug-in assemblies, all
step registrations and the four security roles — are removed. The out-of-the-box Contact and
Task tables are left in their original state, and records created during testing remain.

## 8. Known scope of this offer

The examples solution is a demonstration and validation aid. Its Terms of Use state that it
is intended for learning rather than production, and the repository documentation says the
same. A customer running the framework in production installs the Pillaro Framework solution
and consumes the framework itself as a NuGet package in their own plug-in project; the
examples solution is not a prerequisite for that.

## 9. Support

| Channel | Address |
|---|---|
| Documentation | https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/tree/main/docs |
| Issues | https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/issues |
| Support contact | *to be completed before submission* |
