# Project Setup

Single source of truth for code generation. Edit only INPUT variables.

---

## INPUT VARIABLES (edit these)

| Variable | Value |
|----------|-------|
| LogicProject | examples/Pillaro.Dataverse.PluginFramework.Examples.Logic |
| TestsProject | examples/Pillaro.Dataverse.PluginFramework.Examples.Tests |

---

## DERIVED VALUES (do not edit)

### Namespaces

Derived from project folder name (last segment, dots preserved):

- **LogicNamespace**: `Pillaro.Dataverse.PluginFramework.Examples.Logic`
- **TestsNamespace**: `Pillaro.Dataverse.PluginFramework.Examples.Tests`

### Paths

| Path | Derived From |
|------|--------------|
| PluginsPath | `{LogicProject}/Plugins` |
| TasksPath | `{LogicProject}/Tasks` |
| FeaturesPath | `{LogicProject}/Features` |
| EarlyBoundPath | `{LogicProject}/EarlyBound` |
| EntityConfigPath (= EntityConfigFile) | `{LogicProject}/EarlyBoundSettings.json` |
| EarlyBoundEntityFile | `{EarlyBoundPath}/Entities/<entity>.cs` — one file per entity, e.g. `contact.cs`, `account.cs`, `task.cs`. There is no single combined `EarlyBoundTypes.cs`/`EarlyBounds.cs` — check the per-entity file. |
| TestCasesPath | `{TestsProject}/Tests` |
| TestRepositoriesPath | `{TestsProject}/Data/Repositories` |

### Reference files (primary code-shape references, per `generate-task-and-plugin.prompt.md`)

| Variable | Value | Why this one |
|----------|-------|---------------|
| TaskReferenceFile | `{TasksPath}/Contact/ValidateNames.cs` | Canonical task shape: primary constructor, `Entity.Fields.X` attribute names, `WithValidation`/`ThrowWithWarning` chain |
| PluginReferenceFile | `{PluginsPath}/ContactPlugin.cs` | Canonical plugin shape: `RegisterTask<T>(...)` + `Register(IPluginRegistration)` kept aligned, `WithName(...)` step naming |
| TestReferenceFile | `{TestsProject}/Tests/Tasks/SummarySyncTest.cs` | Active (not commented out) integration test using `TestDataService`, repositories, and Arrange/Act/Assert |

### Namespaces

| Namespace | Derived From |
|-----------|--------------|
| PluginNamespace | `{LogicNamespace}.Plugins` |
| TaskNamespace | `{LogicNamespace}.Tasks.<Entity>` |
| FeatureNamespace | `{LogicNamespace}.Features.<TaskName>` |
| TestNamespace | `{TestsNamespace}.Tests` |

---

## FRAMEWORK CONSTANTS

| Constant | Value | Description |
|----------|-------|-------------|
| PluginBaseClass | `PluginBase` | Local plugin base in Plugins folder |
| TaskBaseType | `TaskBase<Logic.Entity>` | Generic task base from framework |
| RegisterTaskInPluginMethod | `RegisterTask<TTask>(...)` | Plugin task registration method |
| EarlyBoundPrefix | `Logic.` | Prefix for early-bound entity access |
| EntitiesConfigKey | `entityNamesFilter` | Key in EarlyBoundSettings.json for entity list |

---

## RULES

1. **Namespace = Folder**: Namespace must exactly match folder path
2. **Entity validation**: Only validate entities in LogicProject
3. **Early-bound source**: Only files under `{EarlyBoundPath}` in target project
4. **No cross-project inference**: Never use other projects for validation