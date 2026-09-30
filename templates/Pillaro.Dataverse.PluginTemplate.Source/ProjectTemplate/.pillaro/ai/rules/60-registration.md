<!-- Managed by pillaro-dv ai-sync (Pillaro Dataverse Plugin Framework). Do not edit: the next sync overwrites this file. Your own rules belong in AGENTS.md, outside the managed block. -->

# Registration Metadata — highest-risk area for AI output

> [!IMPORTANT]
> Read [`AGENTS.md`](../../../AGENTS.md) first. This file is one entry in its rule catalog.
> Full model: [Plugin Registration API](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/plugin-registration-api.md).

| ID | Rule | Source |
|---|---|---|
| PF-REG-001 | `RegisterTask(...)` (runtime) and `Register(...)` (deployment) MUST stay aligned — stage, message, entity, mode. Changing one means checking the other. | [plugin-registration-api.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/plugin-registration-api.md) |
| PF-REG-002 | Step ID and image ID MUST be non-empty GUIDs; the validator rejects `Guid.Empty` and placeholder patterns. | [plugin-registration-api.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/plugin-registration-api.md) |
| PF-REG-003 | NEVER invent a GUID without an explicit policy for where it comes from. | — |
| PF-REG-004 | A synchronous Update step MUST have filtering attributes; prefer `WhenChanged(...)` for readability and typed flow. | [plugin-registration-api.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/plugin-registration-api.md) |
| PF-REG-005 | Pre-images: PreValidation, PreOperation or PostOperation, never on Create. Post-images: PostOperation only, never on Delete. `WithBothImage(...)`: PostOperation, not Create/Delete. MainOperation (Custom API): no images. | [plugin-registration-api.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/plugin-registration-api.md) |
| PF-REG-006 | Image keys (entity alias, defaulting to the image name) unique within the pre-image and within the post-image collection of a step; image IDs unique across the manifest. | [plugin-registration-api.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/plugin-registration-api.md) |
| PF-REG-007 | In `Register(...)` select attributes with typed selectors (`c => c.FirstName`), as `/examples` do; string literals only while the early-bound type does not exist yet. | [plugin-registration-api.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/plugin-registration-api.md) |
| PF-REG-008 | Always set `WithName(...)`, in the form `{StepPrefix} {entity} {Message} {Stage} {Mode}` (drop entity for custom API/action). An unset name is not managed by deployment. | decision D4, [plugin-registration-api.md — Step Naming](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/plugin-registration-api.md#step-naming) |
| PF-REG-009 | The step name describes coordinates, not purpose. Purpose is already carried by the plugin and task class names. | decision D4 |
| PF-REG-010 | One step per entity × message × stage × mode. A new task with existing coordinates extends that step (filtering attributes and image columns = union of all its tasks); never a duplicate step. | observed in large production solutions |
| PF-REG-011 | `RegisterTask<T>(...)` order is execution order. Group by stage; register value-setting tasks before tasks that read the value. No commented-out registrations. | [execution-pipeline.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/execution-pipeline.md) |
| PF-REG-012 | A Custom API's main operation is `RegisterTask<T>(PluginStage.Mainoperation, …)` + `OnMessage(...).MainOperation()`. It creates no `SdkMessageProcessingStep` (deploy shows `[TYPE-ONLY]`); the Custom API is bound to the plugin type through `CustomAPI.PluginTypeId`. | [plugin-registration-api.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/plugin-registration-api.md#custom-api-mainoperation-registration) |

## Where GUIDs come from (PF-REG-002/003 — hard boundary)

There is no `pillaro new-step` generator yet (tracked as a P1 follow-up). Until it exists:

- **Never copy a GUID from `/docs` or `/examples`.** Those are real IDs from the framework's own
  registered environments; a collision across solutions corrupts registration metadata on deploy.
- **Never invent one either.** Ask a human for a freshly generated GUID (e.g. from
  `[Guid]::NewGuid()` / `New-Guid` run by the developer, or the Plugin Registration Tool), and use
  exactly the value given.
- All-zero placeholders (`"00000000-0000-0000-0000-000000000000"`) are fine in a draft you know will
  be replaced before running `validate` — the validator rejects them, so a forgotten placeholder is
  caught, not silently deployed.

## Keep runtime and deployment registration aligned

```csharp
// Runtime dispatch (constructor)
RegisterTask<UpdateAddressLabel>(PluginStage.Preoperation, ["Create", "Update"], Contact.EntityLogicalName, PluginMode.Synchronous);
```

```csharp
// Deployment metadata (Register(...)) — same stage, message set, entity, mode
registration
    .OnUpdate<Contact>("<step-id>")
    .PreOperation()
    .Synchronous()
    .WhenChanged(c => c.Address1_Line1, c => c.Address1_City /* ... */)
    .WithName($"{StepPrefix} contact Update PreOperation Synchronous");
```

```text
❌ Wrong — RegisterTask says PreOperation, Register(...) says PreValidation.
The task fires on the stage nobody validated against, or on two stages at once.
```

## One step per coordinates — tasks share it (PF-REG-010)

A Dataverse step is identified by its coordinates: **entity × message × stage × mode**. Every task
registered with the same coordinates runs inside that one step. In a mature solution, one entity
plugin commonly carries 20–40 tasks on a handful of steps, so the usual job is not "add a plugin",
it is "add a task to an existing step".

When adding a task to an existing plugin:

1. Find the step in `Register(...)` with the same coordinates as the new `RegisterTask<T>(...)`.
2. **If it exists, extend it** — add the new task's trigger attributes to its `WhenChanged(...)` /
   filtering attributes and its needed columns to the existing image. Do not add a second step with
   the same coordinates.
3. **If it does not exist, add a step** — and a new GUID from a human (PF-REG-003).
4. A shared step's filtering attributes and image columns are the **union** of all its tasks' needs.
   When removing or narrowing a task, recompute the union from the remaining tasks — never remove an
   attribute another task on the same step still validates.

```csharp
// ✅ New task needs `telephone1` on the existing contact Update PreOperation step — extend it
registration
    .OnUpdate<Contact>("<existing-step-id>")
    .PreOperation()
    .Synchronous()
    .WhenChanged(c => c.FirstName, c => c.LastName, c => c.Telephone1)
    .WithName($"{StepPrefix} contact Update PreOperation Synchronous");
```

```csharp
// ❌ A second step with identical coordinates — both fire, every task on them runs twice
registration
    .OnUpdate<Contact>("<new-step-id>")
    .PreOperation()
    .Synchronous()
    .WhenChanged(c => c.Telephone1);
```

## Registration order is execution order (PF-REG-011)

Tasks matching the same step run in the order of their `RegisterTask<T>(...)` calls. Group the
constructor by stage (PreValidation → PreOperation → PostOperation → custom messages) and, within a
stage, register a task that sets a value **before** any task that reads or validates that value.
Delete registrations you no longer need — do not leave them commented out; git keeps the history.

## Filtering attributes and task validation must agree

A synchronous Update step's filtering attributes and the task's own attribute validation
(`EntityWithAtLeastOneAttribute(...)`) describe the same fact from two places. If the task validates
an attribute that is not in the step's filtering attributes, a change to only that attribute never
triggers the task at all — a real bug found in the framework's own example plugin (`ScheduledStart`
was validated but not filtered). Keep the two sets aligned, or drop the unused one from validation.

## Image name must match what the task expects

```csharp
// Registration: image named "image" (the framework's default)
.WithPreImage("<image-id>", "image", c => c.Address1_Line1, /* ... */)
```

```csharp
// Task: reads the default-named image via the PreImage property (see 20-task.md)
var previousCity = PreImage?.Address1_City;
```

If the task calls `GetPreImage("SomeOtherName")`, the registration's image name must be exactly
`"SomeOtherName"` — the two are looked up by name, not linked automatically.

## ➡️ Related

- [10-plugin.md](10-plugin.md)
- [Plugin Registration API](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/plugin-registration-api.md)
- [Early-Bound Entity Generation](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/early-bound-generation.md)
