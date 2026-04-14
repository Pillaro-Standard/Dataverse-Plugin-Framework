# Autonumbering

> [!IMPORTANT]
> The framework provides configurable autonumbering stored in Dataverse and generated through `AutoNumberingService`.

---

## 📑 Navigation

- [🔍 What autonumbering is](#-what-autonumbering-is)
- [🗂️ Configuration entity](#️-configuration-entity)
- [🧱 Supported format parts](#-supported-format-parts)
- [⚙️ `AutoNumberingService`](#️-autonumberingservice)
- [🔄 Transactional autonumbering](#-transactional-autonumbering)
- [💻 Examples](#-examples)
- [✅ Design recommendations](#-design-recommendations)
- [➡️ Related documents](#️-related-documents)

---

## 🔍 What autonumbering is

Autonumbering generates formatted business identifiers for Dataverse records.

Use it for values such as:

- document numbers
- offer numbers
- order numbers
- invoice numbers
- grouped yearly or parent-based sequences

The numbering behavior is controlled by configuration stored in Dataverse.

---

## 🗂️ Configuration entity

Autonumbering configuration is stored in:

- `pl_autonumbering`

The configuration supports:

- entity-based numbering
- parent-based numbering
- grouping-based numbering
- child configuration inheriting format from a parent configuration

This makes it possible to have:

- one global numbering definition for an entity
- separate numbering per parent record
- separate numbering per grouping value such as year

### Configuration attributes

- `pl_entityname` → entity logical name
- `pl_formatstring` → format template with placeholders
- `pl_number` → current sequence number
- `pl_digitcount` → padding for `{NUM}` placeholder
- `pl_dateformat1`, `pl_dateformat2`, `pl_dateformat3` → date format strings
- `pl_parentlookupattribute` → attribute name for parent-based numbering
- `pl_parentlookupid` → parent entity ID
- `pl_groupingvalue` → grouping value (e.g., year)
- `pl_parentautonumberingid` → reference to primary configuration
- `pl_useparentconfiguration` → whether to inherit format from parent

---

## 🧱 Supported format parts

The format string supports these main placeholders:

- `{NUM}` → current sequence number (padded based on `pl_digitcount`)
- `{date1}` → formatted using `pl_dateformat1`
- `{date2}` → formatted using `pl_dateformat2`
- `{date3}` → formatted using `pl_dateformat3`
- `{grouping}` → replaced with grouping value

It also supports dynamic field replacement from:

- the current entity (e.g., `{fieldname}` or `{fieldname:date1}`)
- a parent lookup entity (e.g., `{lookupfield.attributename}`)

Examples:

- `ORD-{date1}-{NUM}`
- `INV-{grouping}-{NUM}`
- `OFR-{customerid.name}-{NUM}`
- `DOC-{createdon:date1}-{NUM}`

The service also supports configurable digit padding for `{NUM}` via `pl_digitcount`.

---

## ⚙️ `AutoNumberingService`

`AutoNumberingService` is the framework service used to generate numbers.

### Constructor

~~~csharp
AutoNumberingService(IOrganizationService organizationService, int retryAttempts = 5)
~~~

### Main methods

**`GetAutoNumber(entityName, entityId, parentEntityId)`**

Uses the `pl_GetAutoNumber` Dataverse custom API request with built-in retry logic.

Returns the generated number as a string.

**`GetTransactionAutoNumber(entityName, entityId, parentEntityId, groupingValue)`**

Builds the number from configuration and returns an `AutoNumberingResponse` containing:

- `Number` → generated formatted value
- `Request` → `UpdateRequest` for persisting the incremented counter

This makes it possible to generate the number and update the sequence safely within a transaction.

---

## 🔄 Transactional autonumbering

The transactional variant returns:

- `AutoNumberingResponse.Number`
- `AutoNumberingResponse.Request`

`Number` is the generated formatted value.

`Request` is an `UpdateRequest` prepared with concurrency control using row-version matching.

This is important because autonumbering must not silently overwrite sequence state when multiple records are generated at the same time.

The update request uses `ConcurrencyBehavior.IfRowVersionMatches`, so sequence updates are protected against conflicting writes.

> [!IMPORTANT]
> If you use transactional autonumbering, you must execute the returned update request.
> It is part of the numbering consistency model.

---

## 💻 Examples

### Generate a simple autonumber

~~~csharp
protected override void DoExecute()
{
    var service = new AutoNumberingService(OrganizationServiceProvider.Admin);
    var number = service.GetAutoNumber("salesorder", ContextEntity.Id, null);

    ContextEntity["ordernumber"] = number;
    AddLogDetail("GeneratedNumber", number);
}
~~~

### Generate a transactional autonumber

~~~csharp
protected override void DoExecute()
{
    var service = new AutoNumberingService(OrganizationServiceProvider.Admin);

    var response = service.GetTransactionAutoNumber(
        "invoice",
        ContextEntity.Id,
        null,
        DateTime.Now.Year.ToString());

    ContextEntity["invoicenumber"] = response.Number;
    OrganizationServiceProvider.Admin.Execute(response.Request);

    AddLogDetail("GeneratedNumber", response.Number);
}
~~~

### Parent-based autonumbering

~~~csharp
protected override void DoExecute()
{
    var service = new AutoNumberingService(OrganizationServiceProvider.Admin);

    var parentId = ((EntityReference)ContextEntity["customerid"]).Id;

    var response = service.GetTransactionAutoNumber(
        "quote",
        ContextEntity.Id,
        parentId,
        null);

    ContextEntity["quotenumber"] = response.Number;
    OrganizationServiceProvider.Admin.Execute(response.Request);
}
~~~

### Using dynamic field tokens

~~~csharp
protected override void DoExecute()
{
    var service = new AutoNumberingService(OrganizationServiceProvider.Admin);

    // Format string in config: "CUST-{accountid.accountnumber}-{NUM}"
    var response = service.GetTransactionAutoNumber(
        "opportunity",
        ContextEntity.Id,
        null,
        null);

    ContextEntity["opportunitynumber"] = response.Number;
    OrganizationServiceProvider.Admin.Execute(response.Request);

    AddLogDetail("GeneratedNumber", response.Number);
}
~~~

---

## ✅ Design recommendations

- keep autonumbering configuration in Dataverse
- use transactional autonumbering when sequence consistency matters
- always execute the returned update request when using the transactional variant
- use grouping when separate sequence ranges are required
- use parent-based configuration when numbering depends on a related record
- keep format strings readable and predictable
- configure `pl_digitcount` to ensure consistent number length
- the entity record must be created before calling `GetTransactionAutoNumber` if using dynamic field tokens

Use autonumbering for:

- business identifiers
- configurable sequence patterns
- grouped numbering
- parent-based numbering

Do not use autonumbering for:

- values that are not real identifiers
- hidden logic that users cannot understand
- formats that are too complex to support safely

> [!TIP]
> Keep the numbering format understandable for both users and support teams.

---

## ➡️ Related documents

- [Task Model](./task-model.md)
- [Runtime Configuration](./configuration.md)
- [Data Access](./data-access.md)