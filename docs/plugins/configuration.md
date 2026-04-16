# Runtime Configuration

> [!IMPORTANT]
> The framework provides runtime settings stored in Dataverse and accessed through `SettingsService`.

---

## 📑 Navigation

- [🔍 What runtime configuration is](#-what-runtime-configuration-is)
- [🗂️ Where settings are stored](#️-where-settings-are-stored)
- [🔐 Access and security roles](#-access-and-security-roles)
- [🧱 Supported value types](#-supported-value-types)
- [⚙️ `SettingsService`](#️-settingsservice)
- [💾 Caching behavior](#-caching-behavior)
- [💻 Examples](#-examples)
- [✅ Design recommendations](#-design-recommendations)
- [➡️ Related documents](#️-related-documents)

---

## 🔍 What runtime configuration is

Runtime configuration allows plugin behavior to be controlled from Dataverse without changing code.

Use it for values such as:

- feature switches
- thresholds
- runtime lists
- severity levels
- configurable business behavior

This makes it possible to adjust selected runtime behavior without rebuilding and redeploying the solution.

---

## 🗂️ Where settings are stored

Framework settings are stored in the runtime settings (pl_setting) table provided by the framework solution in Dataverse.

Each setting is identified by:

- `Key`

And can store its value in one of several typed fields.

The UI allows settings to be managed directly in the framework model-driven app.

> [!NOTE]
> A setting is expected to be unique by key.

---

## 🔐 Access and security roles

Runtime settings are managed in Dataverse and access to them is controlled through framework security roles.

Typical roles include:

- `Pillaro Setting Reader`
- `Pillaro Setting Manager`

This separation allows you to distinguish between:

- users who can read runtime settings
- users who can create or modify runtime settings

> [!NOTE]
> Settings access is part of framework administration, not only plugin implementation.
> Make sure the right users and support roles have the appropriate security role assigned.

---

## 🧱 Supported value types

The runtime settings model supports these value types:

- `Bool`
- `Int`
- `Decimal`
- `Date`
- `Text`
- `Json`

This allows the same settings table to support both simple scalar values and structured JSON-based configuration.

Typical examples:

- `MinimalSeverityLevel` → `Int`
- `ForbiddenWords` → `Json`
    
---

## ⚙️ `SettingsService`

`SettingsService` is the framework service used to read runtime settings from Dataverse.

It is available in `TaskBase<TEntity>` as:

- `SettingService`

Supported access methods include:

- `GetTextValue(key)`
- `GetJsonValue(key, throwException = true)`
- `GetModel<TModel>(key, throwException = true)`
- `GetIntegerValue(key)`
- `GetBoolValue(key)`
- `GetDecimalValue(key)`
- `GetDateTimeValue(key)`

If a required value is missing or empty, the service throws an exception for the corresponding access method.

---

## 💾 Caching behavior

`SettingsService` uses an internal cache.

By default:

- cache lifetime is `60` seconds

This means:

- repeated reads of the same key do not always hit Dataverse immediately
- runtime settings are still refreshable without redeploying code
- changes may not be visible instantly during the cache window

> [!NOTE]
> Runtime settings are dynamic, but not real-time reactive.
> Short caching is part of the design.

---

## 💻 Examples

### Read an integer value

~~~csharp
protected override void DoExecute()
{
    var minimalSeverityLevel = SettingService.GetIntegerValue("MinimalSeverityLevel");

    AddLogDetail("MinimalSeverityLevel", minimalSeverityLevel);
}
~~~

### Read JSON configuration

~~~csharp
protected override void DoExecute()
{
    var forbiddenWordsJson = SettingService.GetJsonValue("ForbiddenWords");
    AddLogDetail("ForbiddenWords", forbiddenWordsJson);
}
~~~

### Read a typed model from JSON

~~~csharp
protected override void DoExecute()
{
    var config = SettingService.GetModel<MyConfigModel>("MyConfig");
    AddLogMessageLine("Runtime configuration loaded.");
}
~~~

Use JSON when the setting represents:

- a list
- a structured object
- more than one related value

---

## ✅ Design recommendations

Use these rules by default:

- use runtime settings for values that may change without code changes
- use typed fields for simple scalar values
- use JSON for structured configuration
- keep keys stable and explicit
- do not use runtime settings as a substitute for normal code structure
- do not put business logic into configuration itself

Use runtime settings for:

- configuration
- thresholds
- flags
- environment-specific values

Do not use runtime settings for:

- core domain logic
- hidden control flow that makes behavior unreadable
- values that should stay fixed in code

> [!TIP]
> A runtime setting should change behavior clearly, not make behavior mysterious.

---

## ➡️ Related documents

- [Task Model](./task-model.md)
- [Data Access](./data-access.md)
- [Logging](./logging.md)
- [Error Handling](./error-handling.md)