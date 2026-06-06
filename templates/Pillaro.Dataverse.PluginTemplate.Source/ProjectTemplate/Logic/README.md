# Pillaro Dataverse Plugin Template - Setup Guide

This is the main setup guide for projects created from the **Pillaro Dataverse Plugin Template**.

It lives in the **Logic** project so users see it where they spend the most time.

## Quick setup checklist

1. Create a new solution from the Visual Studio template.
2. Run **Build > Rebuild Solution**.
3. Include generated files and folders in Visual Studio if needed:
   - `Logic\Tools\EarlyBound`
   - `Plugins\Tools\Deployment`
   - `Plugins\PillaroSettings.json`
4. Set `"Solution": "ExampleSolution"` in `PillaroSettings.json`.
5. Configure the `DVCON` environment variable.
6. Configure the local Dataverse connection in `<YourSolutionName>.Tests\appsettings.Development.json`.
7. Do not commit `appsettings.Development.json`.
8. Run the `Connect_Should_Return_Valid_UserId` test to verify the Dataverse connection.

---

## 1. Rebuild the solution

After creating the project from the template, first rebuild the whole solution:

```text
Build > Rebuild Solution
```

During the rebuild, helper folders and files are generated into the projects.

Some generated files may be physically present on disk but not yet included in the Visual Studio project.

---

## 2. Include generated files in Visual Studio

For generated folders or files that are present on disk but not visible as part of the project:

1. In Solution Explorer, enable **Show All Files**.
2. Find the generated folder or file.
3. Right-click it.
4. Select **Include In Project**.

Use this process for the items listed below.

---

## 3. Logic project

After the first rebuild, the **Logic** project will contain generated helper folders.

Include only:

```text
Tools\EarlyBound
```

The `Deployment` and `ILMerge` folders do not need to be included in the **Logic** project.

### Early-bound entity generation

To generate early-bound entities, Microsoft Power Platform CLI must be installed. The required command-line tool is:

```text
pac
```

More details are available in:

```text
Tools\EarlyBound\README.md
```

---

## 4. Plugins project

After the first rebuild, the **Plugins** project will contain a generated `Tools` folder.

Include:

```text
Tools\Deployment
```

This folder contains scripts and tools used to deploy the plugin assembly, plugins, and plugin steps to Dataverse.

More details are available in:

```text
Tools\Deployment\README.md
```

Also include the following file from the **Plugins** project root:

```text
PillaroSettings.json
```

---

## 5. Configure `PillaroSettings.json`

Open:

```text
Plugins\PillaroSettings.json
```

Set the `Solution` property to the Dataverse solution where plugins and plugin steps should be registered.

Use the Dataverse solution **unique name**, not the display name.

Example:

```json
{
  "Solution": "ExampleSolution"
}
```

---

## 6. Configure deployment connection

The deployment tools use the environment variable:

```text
DVCON
```

This variable contains the Dataverse connection string used by the build and deployment tooling.

The setup of `DVCON` is described in:

```text
Tools\Deployment\README.md
```

---

## 7. Configure Dataverse connection for tests

To run the integration and example tests, configure the local Dataverse connection in:

```text
appsettings.Development.json
```

Set the connection string named:

```text
Dataverse
```

Use the Dataverse environment where the tests should run.

After the connection string is configured, run the following test to verify that the Dataverse connection works:

```text
Connect_Should_Return_Valid_UserId
```

---

## 8. Known issues

### Test project does not run correctly after creating the solution from the template

In some cases, the test project may not run correctly immediately after the solution is created from the Visual Studio template.

The test runner may show build or runtime errors even if the project configuration is correct.

If this happens, use the following steps:

1. Run **Clean Solution** in Visual Studio.
2. Delete the following folders from the **Tests** project:

   ```text
   bin
   obj
   ```

3. Run the test again.

After cleaning the solution and deleting the `bin` and `obj` folders from the test project, the test should run correctly.

Recommended verification test:

```text
Connect_Should_Return_Valid_UserId
```

---

## 9. Do not commit local development settings

The file:

```text
appsettings.Development.json
```

can contain local connection strings, client secrets, user-specific configuration, or other sensitive information.

It should not be committed to the repository.

Add it to `.gitignore`:

```gitignore
# Local development settings
**/appsettings.Development.json
```

If the file has already been committed, remove it from git tracking while keeping it locally:

```bash
git rm --cached appsettings.Development.json
```

Then commit the `.gitignore` change.

---

## 10. ILMerge

The ILMerge post-build action is already configured by the template.

No manual ILMerge setup is required.

The generated plugin project folder:

```text
Tools\ILMerge
```

is used internally by the build process and does not need to be included manually.

It is copied into the `Plugins` project before `PostBuildEvent` runs, so the post-build merge executes from the project-local `Tools\ILMerge\ILMerge.exe` path.

---

## 11. Additional documentation

Detailed documentation is available in the generated tool folders:

```text
Tools\EarlyBound\README.md
Tools\ILMerge\README.md
Tools\Deployment\README.md
```

These documents describe:

- early-bound entity generation,
- ILMerge setup and post-build merge behavior,
- Microsoft Power Platform CLI setup,
- deployment scripts,
- `DVCON` configuration,
- plugin and plugin step registration.

---

## 12. More information

More information about Pillaro Labs and our work is available at:

https://www.pillaro.cz

The full source-open project documentation is available in the GitHub repository:

https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework
