# Plugin Generation

## Location & Namespace

- **Path**: `{PluginsPath}` from project-setup.md
- **Namespace**: `{PluginNamespace}` from project-setup.md

## Structure
~~~
public class <EntityOrFeature>Plugin : PluginBase
{
    public <EntityOrFeature>Plugin(string unsecureConfig, string secureConfig) 
        : base(unsecureConfig, secureConfig)
    {
        RegisterTask<TaskName>(PluginStage.X, "Message", Logic.Entity.EntityLogicalName, PluginMode.X);
    }
}
~~~

Plugin class should contain:
- constructor
- task registrations only

No additional methods unless required for registration grouping.

---

## Rules

| Do | Don't |
|----|-------|
| Register tasks | Contain business logic |
| Map events to tasks | Access services directly |
| Use `RegisterTask<T>(...)` | Duplicate registrations |

---

## Naming

- CamelCase
- Entity-based: `ContactPlugin`, `AccountPlugin`
- Feature-based: `AutoNumberingPlugin`, `NotificationPlugin`

---

## Base Classes

- Plugin must inherit from {PluginBaseClass}

---

## Responsibility

Plugin is orchestration only.

Plugin must:
- register tasks
- map pipeline events to tasks

Plugin must NOT:
- contain business logic
- access services directly
- perform validation logic

---

## Task Registration

Tasks must be registered using:
{RegisterTaskInPluginMethod}

Each registration must:
- target correct entity
- define message (Create, Update, Delete, etc.)
- define pipeline stage (PreOperation, PostOperation, etc.)
- be unique (no overlapping logic)

---

## Constraints

- Do not duplicate logic across plugins
- Do not register same logic multiple times
- Do not call services directly
- Do not access Entity attributes directly in plugin

All logic must be implemented in Tasks