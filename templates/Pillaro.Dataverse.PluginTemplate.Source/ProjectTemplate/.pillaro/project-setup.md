# Project setup

The values the AI instructions use for paths, namespaces and reference files in this solution.
`ai-sync` created this file from the projects it found; it never overwrites it. Correct the values if
they are wrong, and add anything your agents should know about this solution.

## Input values

| Value | This solution |
|---|---|
| `LogicProject` | `Logic` |
| `TestsProject` | `Tests` |

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
| Early-bound types | the `namespace` value in `EarlyBoundSettings.json` — normally the Logic root namespace, used as `Logic.Contact` (PF-DATA-004) |
| Test | `<Tests root namespace>.Tests.<Entity>` |

## Reference files

Copy the code shape from these, not from memory. Until this solution has its own tasks, they are
copies from the framework's examples; replace the links with your own files once you have them.
Never copy a step or image GUID from them (PF-REG-003).

| Reference | File | What it shows |
|---|---|---|
| Task that validates | [`ValidateNames.cs`](ai/examples/ValidateNames.cs.txt) | the chain decides when to check, `DoExecute()` checks the rule and throws `DataverseValidationException` |
| Task that writes | [`RecordJobTitleChange.cs`](ai/examples/RecordJobTitleChange.cs.txt) | primary constructor, `Logic.Contact.Fields` constants, update queue |
| Task with pre-image | [`ArchiveDeletedContact.cs`](ai/examples/ArchiveDeletedContact.cs.txt) | a precondition in the chain, reading the pre-image |
| Plugin | [`ContactPlugin.cs`](ai/examples/ContactPlugin.cs.txt) | `RegisterTask<T>(...)` aligned with `Register(...)`, typed selectors, step names, images |
| Test | [`ArchiveDeletedContactTests.cs`](ai/examples/ArchiveDeletedContactTests.cs.txt) | test class shape, repositories, `CreateTestEntity(...)`, re-reading after the act |
| Rejection test | [`ValidateNamesTests.cs`](ai/examples/ValidateNamesTests.cs.txt) | asserting `FaultException<OrganizationServiceFault>` and the user's message |
| Test data repository | [`ContactRepository.cs`](ai/examples/ContactRepository.cs.txt) | `GetNew(...)` with a record valid for every task |

## This solution

Add here what an agent should know and cannot see in the code: the dev environment's purpose, test
users for role-dependent rules, naming conventions of the team, settings records the tasks read.
