<!-- Managed by pillaro-dv ai-sync (Pillaro Dataverse Plugin Framework). Do not edit: the next sync overwrites this file. Your own rules belong in AGENTS.md, outside the managed block. -->

# Plugin Classes

> [!IMPORTANT]
> Read [`AGENTS.md`](../../../AGENTS.md) first. This file is one entry in its rule catalog.

| ID | Rule | Source |
|---|---|---|
| PF-PLUG-001 | A plugin inherits from your **solution's own** `PluginBase`, never directly from the framework's. | [getting-started.md §7](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/getting-started.md#7-create-your-solution-pluginbase) |
| PF-PLUG-002 | The constructor contains only `RegisterTask<T>(...)` calls. No conditionals, no Dataverse queries. | [plugin-model.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/plugin-model.md) |
| PF-PLUG-003 | Deployment metadata only in `Register(IPluginRegistration)`. | [plugin-registration-api.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/plugin-registration-api.md) |
| PF-PLUG-004 | Name plugins by entity (`ContactPlugin`) or by business capability, not by task. | [plugin-model.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/plugin-model.md) |

## Correct shape

```csharp
public sealed class ContactPlugin : PluginBase
{
    public ContactPlugin(string unsecureConfig, string secureConfig)
        : base(unsecureConfig, secureConfig)
    {
        RegisterTask<ValidateNames>(PluginStage.Prevalidation, ["Create", "Update"], Contact.EntityLogicalName, PluginMode.Synchronous);
    }

    // One step per message the task is registered for - same stage and mode as RegisterTask above.
    public override void Register(IPluginRegistration registration)
    {
        registration
            .OnCreate<Contact>("<step-id-from-a-human>")
            .PreValidation()
            .Synchronous()
            .WithName($"{StepPrefix} contact Create PreValidation Synchronous")
            .WithFilteringAttributes(c => c.FirstName, c => c.LastName);

        registration
            .OnUpdate<Contact>("<step-id-from-a-human>")
            .PreValidation()
            .Synchronous()
            .WithName($"{StepPrefix} contact Update PreValidation Synchronous")
            .WhenChanged(c => c.FirstName, c => c.LastName);
    }
}
```

## Wrong shapes to reject on sight

```csharp
// ❌ business decision in the constructor
public ContactPlugin(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
{
    if (SomeCondition())                              // constructors register tasks, nothing else
        RegisterTask<ValidateNames>(...);
}

// ❌ Dataverse query in the constructor or in Register(...)
public override void Register(IPluginRegistration registration)
{
    var setting = _organizationService.Retrieve(...); // never — Register only declares metadata
    // ...
}
```

## Runtime registration and deployment metadata must agree

`RegisterTask(...)` (constructor, runtime dispatch) and `Register(...)` (deployment metadata) are two
independent declarations of the *same* fact — which task runs for which message/entity/stage/mode.
Changing one without the other (PF-REG-001, see
[60-registration.md](60-registration.md)) means the task either never fires at runtime, or Dataverse
invokes a step whose task silently does nothing.

## ➡️ Related

- [20-task.md](20-task.md)
- [60-registration.md](60-registration.md)
- [Plugin Model](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/plugin-model.md)
