# Early-Bound Entity Generation

This page describes how to generate Dataverse early-bound entity classes for projects that use Pillaro Dataverse Plugin Framework.

The framework package prepares a small local tooling folder in consuming plugin projects. The tooling uses the Power Platform CLI (`pac modelbuilder`) so developers can generate strongly typed Dataverse classes from the metadata of the target environment.

Generated early-bound classes are useful when you want:

- IntelliSense for Dataverse entities and fields
- strongly typed entity access in plugin tasks
- typed attribute selection in the plugin registration API
- generated logical-name constants for filtering attributes and images

See also [Plugin Registration API](./plugin-registration-api.md) for examples of using typed early-bound properties when configuring plugin steps.

---

## 1. Install the Framework Package

Install `Pillaro.Dataverse.PluginFramework` into the Dataverse plugin project and rebuild the project.

After rebuild, the package creates:

```text
Tools/
  EarlyBound/
    EarlyBoundSettings.json
    GenerateEarlyBound.bat
    README.md
```

> [!NOTE]
> The `Tools` folder is generated after rebuild.
> If Visual Studio does not show it, enable **Show All Files**, locate the generated folder, and choose **Include In Project** if you want the tooling visible in the project tree.

---

## 2. Install and Authenticate Power Platform CLI

Install the Power Platform CLI:

```text
https://learn.microsoft.com/power-platform/developer/cli/introduction
```

Authenticate against the Dataverse environment whose metadata should be used:

```powershell
pac auth create --url https://your-org.crm4.dynamics.com
```

List available authentication profiles:

```powershell
pac auth list
```

The command displays all stored Power Platform CLI authentication profiles, including their index, active profile marker, environment URL, user and cloud.

If you have multiple profiles, select the correct one using the index from the list:

```powershell
pac auth select --index 1
```

The generated wrapper uses the active `pac` authentication profile. Always verify that the selected profile points to the correct Dataverse environment before generating metadata.

---

## 3. Configure EarlyBoundSettings.json

The generated settings file lives here:

```text
Tools/EarlyBound/EarlyBoundSettings.json
```

This file is user-owned. The package creates it only when it is missing and does not overwrite your changes during later rebuilds.

The most important settings are:

| Setting | Purpose |
|---|---|
| `namespace` | Namespace for generated C# classes. Usually match your Logic project namespace, for example `YourSolution.Logic.EarlyBound`. |
| `entityNamesFilter` | Logical names of Dataverse entities to generate. Keep the list focused on entities used by the solution. |
| `generateSdkMessages` | Whether SDK message request/response classes should be generated. Keep `false` if you only need entities. |
| `messageNamesFilter` | Message names to generate when `generateSdkMessages` is enabled. Keep it empty when messages are not needed. |
| `emitFieldsClasses` | Generates field-name constants, useful for filtering attributes and images. |

Example entity-focused configuration:

```json
{
  "namespace": "YourSolution.Logic.EarlyBound",
  "serviceContextName": "ServiceContext",
  "generateSdkMessages": false,
  "emitFieldsClasses": true,
  "entityNamesFilter": [
    "account",
    "contact",
    "systemuser"
  ],
  "messageNamesFilter": []
}
```

> [!IMPORTANT]
> Do not put an empty string into `messageNamesFilter`.
> Keep the array empty (`[]`) when no messages should be generated.

---

## 4. Generate Classes

Run the generated wrapper from the plugin project root:

```bat
.\Tools\EarlyBound\GenerateEarlyBound.bat
```

The wrapper runs:

```text
pac modelbuilder build --outdirectory "EarlyBound" --settingsTemplateFile "Tools/EarlyBound/EarlyBoundSettings.json"
```

Generated C# files are written to:

```text
EarlyBound/
```

Classic .NET Framework projects can include generated files through the package target. SDK-style projects usually include generated `.cs` files automatically through default compile items.

---

## 5. Use Generated Types

Generated classes can be used in task validation, task execution, and plugin registration metadata.

Example runtime task registration:

```csharp
RegisterTask<ValidateContactTask>(
    PluginStage.Preoperation,
    ["Create", "Update"],
    Contact.EntityLogicalName,
    PluginMode.Synchronous);
```

Example typed plugin registration:

```csharp
registration
    .OnUpdate<Contact>("00000000-0000-0000-0000-000000000000")
    .PreOperation()
    .Synchronous()
    .WhenChanged(c => c.FirstName, c => c.LastName)
    .WithPreImage(
        "00000000-0000-0000-0000-000000000000",
        "PreImage",
        c => c.FirstName,
        c => c.LastName);
```

Typed registration reads logical names from generated early-bound attributes, which reduces manual string handling and gives developers IntelliSense.

---

## File Ownership

| File | Owner | Update behavior |
|---|---|---|
| `Tools/EarlyBound/EarlyBoundSettings.json` | User | Created only if missing. Never overwritten by package rebuilds. |
| `Tools/EarlyBound/GenerateEarlyBound.bat` | Package | Regenerated on build when managed tool updates are enabled. |
| `Tools/EarlyBound/README.md` | Package | Regenerated on build when managed tool updates are enabled. |
| `EarlyBound/**/*.cs` | Generated output | Recreated by `pac modelbuilder` when generation is run. |

If you intentionally do not want the package to refresh managed tool files, set `PillaroUpdateManagedToolFiles` to `false` in the project file.

If you do not want the package to create early-bound tooling at all, set `PillaroGenerateEarlyBoundTools` to `false`.

---

## Troubleshooting

| Problem | What to check |
|---|---|
| `Tools/EarlyBound` is missing | Rebuild the plugin project, enable **Show All Files**, then include the generated folder if needed. |
| `pac` is not recognized | Install the Power Platform CLI and restart the terminal or Visual Studio. |
| Generation uses the wrong environment | Run `pac auth list`, then `pac auth select --index <n>`. |
| No entity classes are generated | Check `entityNamesFilter` and verify the logical names exist in the selected environment. |
| Generated files are not visible in Visual Studio | Reload the project, rebuild, or enable **Show All Files**. |

## Related Documents

- [Getting Started](./getting-started.md) - First plugin setup and generated tooling overview.
- [Plugin Registration API](./plugin-registration-api.md) - Use early-bound classes in deployment metadata.
- [Deployment Plugins](./deployment-plugins.md) - Deploy the built plugin assembly and synchronized plugin steps.
