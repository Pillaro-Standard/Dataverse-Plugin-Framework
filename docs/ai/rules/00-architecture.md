# Architecture and Code Location

> [!IMPORTANT]
> Read [`AGENTS.md`](../../../AGENTS.md) first. This file is one entry in its rule catalog.

| ID | Rule | Source |
|---|---|---|
| PF-ARCH-001 | All business logic MUST live in the `Logic` project. | [architecture.md](../../plugins/architecture.md) |
| PF-ARCH-002 | `Plugins` is a deployment shell only — NEVER put a task, feature, or domain logic there. | [architecture.md](../../plugins/architecture.md), [getting-started.md](../../plugins/getting-started.md) |
| PF-ARCH-003 | Tests reference `Logic`, NEVER the merged `Plugins` output. | [limitations.md](../../limitations.md) |
| PF-ARCH-004 | Structure: `Plugins/` (orchestration), `Tasks/` (business logic), `Features/` (reusable services). | [getting-started.md §3.3](../../plugins/getting-started.md#33-create-the-basic-folder-structure) |
| PF-ARCH-005 | `Tests/` structure mirrors `Tasks/` structure. | [testing.md](../../tests/testing.md) |
| PF-ARCH-006 | New files go into the existing structure. A new top-level folder needs human sign-off. | — |

## Why this split exists

`Plugins` produces the merged, signed, deployable assembly. After ILMerge, its output is not a good
reference target — merged assemblies can introduce duplicate type and dependency problems, and tests
that reference the merged output are fragile in ways unrelated to the business logic they test. Keep
business logic in `Logic`, which both `Plugins` and `Tests` reference independently.

```csharp
// ✅ Correct: business logic in Logic/Tasks/
namespace YourSolution.Logic.Tasks.Contact
{
    public class ValidateNames(IServiceProvider serviceProvider, TaskContext taskContext)
        : TaskBase<Logic.Contact>(serviceProvider, taskContext)
    {
        // ...
    }
}
```

```csharp
// ❌ Wrong: business logic living in Plugins
namespace YourSolution.Plugins
{
    public class ContactPlugin : PluginBase
    {
        public override void Register(IPluginRegistration registration)
        {
            // ❌ a query or a business rule here belongs in a Task, not in the plugin class
            if (SomeDataverseQuery().Any()) { /* ... */ }
        }
    }
}
```

## `src/` is not a layout example

The framework's own `src/` has no `Logic` project — it is the framework itself, not a consuming
solution. Do not copy its layout for a solution built from the template; the template's
`Logic` + `Plugins` + `Tests` split is the one to follow.

## ➡️ Related

- [10-plugin.md](./10-plugin.md)
- [20-task.md](./20-task.md)
- [Architecture](../../plugins/architecture.md)
