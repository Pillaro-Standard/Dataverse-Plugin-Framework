# Registration Metadata — highest-risk area for AI output

> [!IMPORTANT]
> Read [`AGENTS.md`](../../../AGENTS.md) first. This file is one entry in its rule catalog.
> Full model: [Plugin Registration API](../../plugins/plugin-registration-api.md).

| ID | Rule | Source |
|---|---|---|
| PF-REG-001 | `RegisterTask(...)` (runtime) and `Register(...)` (deployment) MUST stay aligned — stage, message, entity, mode. Changing one means checking the other. | [plugin-registration-api.md](../../plugins/plugin-registration-api.md) |
| PF-REG-002 | Step ID and image ID MUST be non-empty GUIDs; the validator rejects `Guid.Empty` and placeholder patterns. | [plugin-registration-api.md](../../plugins/plugin-registration-api.md) |
| PF-REG-003 | NEVER invent a GUID without an explicit policy for where it comes from. | — |
| PF-REG-004 | A synchronous Update step MUST have filtering attributes; prefer `WhenChanged(...)` for readability and typed flow. | [plugin-registration-api.md](../../plugins/plugin-registration-api.md) |
| PF-REG-005 | Create steps cannot have a pre-image; Delete steps cannot have a post-image; images only in Pre/PostOperation. | [plugin-registration-api.md](../../plugins/plugin-registration-api.md) |
| PF-REG-006 | Image names unique per image type within a step; image IDs unique across the manifest. | [plugin-registration-api.md](../../plugins/plugin-registration-api.md) |
| PF-REG-007 | Prefer typed attribute selection (`c => c.FirstName`) or `Fields` constants; string literals only when the early-bound type does not exist yet. | [plugin-registration-api.md](../../plugins/plugin-registration-api.md) |
| PF-REG-008 | Always set `WithName(...)`, in the form `{StepPrefix} {entity} {Message} {Stage} {Mode}` (drop entity for custom API/action). An unset name is not managed by deployment. | decision D4, [plugin-registration-api.md — Step Naming](../../plugins/plugin-registration-api.md#step-naming) |
| PF-REG-009 | The step name describes coordinates, not purpose. Purpose is already carried by the plugin and task class names. | decision D4 |

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
    .WhenChanged(Contact.Fields.Address1_Line1, Contact.Fields.Address1_City /* ... */)
    .WithName($"{StepPrefix} contact Update PreOperation Synchronous");
```

```text
❌ Wrong — RegisterTask says PreOperation, Register(...) says PreValidation.
The task fires on the stage nobody validated against, or on two stages at once.
```

## Filtering attributes and task validation must agree

A synchronous Update step's filtering attributes and the task's own attribute validation
(`EntityWithAtLeastOneAttribute(...)`) describe the same fact from two places. If the task validates
an attribute that is not in the step's filtering attributes, a change to only that attribute never
triggers the task at all — a real bug found in this repository's own example plugin (`ScheduledStart`
was validated but not filtered). Keep the two sets aligned, or drop the unused one from validation.

## Image name must match what the task expects

```csharp
// Registration: image named "image" (the framework's default)
.WithPreImage("<image-id>", "image", Contact.Fields.Address1_Line1, /* ... */)
```

```csharp
// Task: reads the default-named image via the PreImage property (see 20-task.md)
var previousCity = PreImage?.Address1_City;
```

If the task calls `GetPreImage("SomeOtherName")`, the registration's image name must be exactly
`"SomeOtherName"` — the two are looked up by name, not linked automatically.

## ➡️ Related

- [10-plugin.md](./10-plugin.md)
- [Plugin Registration API](../../plugins/plugin-registration-api.md)
- [Early-Bound Entity Generation](../../plugins/early-bound-generation.md)
