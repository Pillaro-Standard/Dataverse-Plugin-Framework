---
name: Plugins
description: Plugin classes — task registration and deployment metadata (steps, images, filtering attributes).
applyTo: "**/*Logic/Plugins/**/*.cs"
---

# Plugin classes

Full rules and reasons: [`10-plugin.md`](../../docs/ai/rules/10-plugin.md) and
[`60-registration.md`](../../docs/ai/rules/60-registration.md). Paths, namespaces and the reference
plugin: [`project-setup.md`](../project-setup.md).

A plugin class does two things and nothing else: it registers tasks for runtime dispatch, and it
declares the deployment metadata of its steps. All business logic is in tasks.

## Rules

- Inherit from the solution's own `PluginBase`, never the framework's directly (PF-PLUG-001).
- Name the plugin after the entity (`ContactPlugin`) or the business capability, never after a task
  (PF-PLUG-004). Before adding a plugin, look for an existing `<Entity>Plugin` — usually the right
  change is a new task on it.
- The constructor contains only `RegisterTask<T>(...)` calls: no conditions, no queries (PF-PLUG-002).
- Registration order is execution order. Group by stage (PreValidation → PreOperation →
  PostOperation → custom messages), register a task that sets a value before a task that reads it,
  and delete registrations you no longer need instead of commenting them out (PF-REG-011).
- `Register(IPluginRegistration)` declares metadata only — no queries (PF-PLUG-003).
- `RegisterTask<T>(...)` and `Register(...)` describe the same step. Stage, message, entity and mode
  must match; changing one means checking the other (PF-REG-001).
- One step per entity × message × stage × mode. If the step already exists, extend it: its filtering
  attributes and image columns are the union of what all its tasks need. Never add a second step with
  the same coordinates (PF-REG-010).
- A synchronous Update step has filtering attributes, preferably via `WhenChanged(...)` (PF-REG-004).
  They must agree with the attributes the tasks validate — an attribute the task checks but the step
  does not filter on never triggers the task.
- Select attributes with typed selectors (`c => c.FirstName`). String literals only while the entity
  has no early-bound type (PF-REG-007).
- Always set `WithName($"{StepPrefix} <entity> <Message> <Stage> <Mode>")`. The name describes the
  coordinates, not the purpose (PF-REG-008/009).
- Images: a pre-image never on Create, a post-image only in PostOperation and never on Delete,
  `WithBothImage(...)` (framework 1.2.0 and later) only in PostOperation and not on Create or Delete,
  no images on MainOperation (PF-REG-005). The image name must be exactly the name the task reads
  (`"image"` for `PreImage`).
- A Custom API main operation is `RegisterTask<T>(PluginStage.Mainoperation, …)` together with
  `OnMessage(...).MainOperation()` (PF-REG-012).

## Step and image GUIDs

Never invent a GUID and never copy one from `/docs` or `/examples` (PF-REG-002/003). Ask the
developer for a new one (`New-Guid`) and use exactly the value given. While you wait, an all-zero
placeholder is acceptable in a draft — `validate` rejects it, so it cannot be deployed by accident.

## Shape

```csharp
public class ContactPlugin : PluginBase
{
    public ContactPlugin(string unsecureConfig, string secureConfig)
        : base(unsecureConfig, secureConfig)
    {
        // PreOperation, Update only: one step
        RegisterTask<RecordJobTitleChange>(PluginStage.Preoperation, ["Update"], Contact.EntityLogicalName, PluginMode.Synchronous);
        // PostOperation, Delete only: one step with a pre-image
        RegisterTask<ArchiveDeletedContact>(PluginStage.Postoperation, ["Delete"], Contact.EntityLogicalName, PluginMode.Synchronous);
    }

    // Every RegisterTask above has its step here: same message, stage and mode.
    public override void Register(IPluginRegistration registration)
    {
        registration
            .OnUpdate<Contact>("<step id from the developer>")
            .PreOperation()
            .Synchronous()
            .WithName($"{StepPrefix} contact Update PreOperation Synchronous")
            .WhenChanged(c => c.JobTitle);

        registration
            .OnDelete<Contact>("<step id from the developer>")
            .PostOperation()
            .Synchronous()
            .WithName($"{StepPrefix} contact Delete PostOperation Synchronous")
            .WithPreImage("<image id from the developer>", "image", c => c.FirstName, c => c.LastName, c => c.ParentCustomerId);
    }
}
```
