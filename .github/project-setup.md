# Project setup

The values the Copilot instructions and prompts use for paths, namespaces and reference files. In
this repository they point at `/examples`. In your own solution, change the two input values and the
reference files; everything else follows from them.

## Input values

| Value | This repository |
|---|---|
| `LogicProject` | `examples/Pillaro.Dataverse.PluginFramework.Examples.Logic` |
| `TestsProject` | `examples/Pillaro.Dataverse.PluginFramework.Examples.Tests` |

## Derived paths

| Value | Path |
|---|---|
| `PluginsPath` | `{LogicProject}/Plugins` |
| `TasksPath` | `{LogicProject}/Tasks/<Entity>` |
| `FeaturesPath` | `{LogicProject}/Features/<FeatureName>` |
| `EarlyBoundEntityFile` | `{LogicProject}/EarlyBound/Entities/<entity>.cs` — one file per entity, e.g. `contact.cs` |
| `EarlyBoundSettingsFile` | `{LogicProject}/Tools/EarlyBound/EarlyBoundSettings.json` — generated entities are listed under `entityNamesFilter` |
| `EarlyBoundGenerator` | `{LogicProject}/Tools/EarlyBound/GenerateEarlyBound.bat` — run by the developer |
| `TestsPath` | `{TestsProject}/Tests/<Entity>` |
| `TestRepositoriesPath` | `{TestsProject}/Data/Repositories` |

## Namespaces

The namespace is the project's root namespace (`RootNamespace` in the `.csproj`, otherwise the
project name) followed by the folder path:

| Code | Namespace |
|---|---|
| Plugin | `<Logic root namespace>.Plugins` |
| Task | `<Logic root namespace>.Tasks.<Entity>` |
| Feature | `<Logic root namespace>.Features.<FeatureName>` |
| Early-bound types | the `namespace` value in `EarlyBoundSettings.json` — the Logic root namespace, used as `Logic.Contact` (PF-DATA-004). Projects set up with framework 1.2.2 or older may still have `<Logic root namespace>.EarlyBound`; tell the developer, since `Logic.Contact` then does not compile inside `Tasks/Contact/` |
| Test | `<Tests root namespace>.Tests.<Entity>` |

## Reference files

Copy the code shape from these, not from memory:

| Reference | This repository | What it shows |
|---|---|---|
| Task that validates | `{LogicProject}/Tasks/Contact/ValidateNames.cs` | the chain decides when to check, `DoExecute()` checks the rule and throws `DataverseValidationException` |
| Task that writes | `{LogicProject}/Tasks/Contact/RecordJobTitleChange.cs` | primary constructor, `Logic.Contact.Fields` constants, update queue |
| Task with pre-image | `{LogicProject}/Tasks/Contact/ArchiveDeletedContact.cs` | a precondition in the chain instead of in `DoExecute()`, reading the pre-image |
| Plugin | `{PluginsPath}/ContactPlugin.cs` | `RegisterTask<T>(...)` aligned with `Register(...)`, typed selectors, step names, images |
| Test | `{TestsProject}/Tests/Contact/ArchiveDeletedContactTests.cs` | test class shape, repositories, `CreateTestEntity(...)`, re-reading after the act |
| Rejection test | `{TestsProject}/Tests/Contact/ValidateNamesTests.cs` | asserting `FaultException<OrganizationServiceFault>` and the user's message |
| Test data repository | `{TestRepositoriesPath}/ContactRepository.cs` | `GetNew(...)` with a record valid for every task |

A project created from the template has only `ExampleTask` and `ExamplePlugin`, which have no
early-bound types yet. Until the solution has its own first task, use the reference files above from
this repository.
