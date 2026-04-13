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

---

## 🧱 Supported format parts

The format string supports these main placeholders:

- `{NUM}` → current sequence number
- `{date1}`
- `{date2}`
- `{date3}`
- `{grouping}`

It also supports dynamic field replacement from:

- the current entity
- a parent lookup entity

Examples:

- `ORD-{date1}-{NUM}`
- `INV-{grouping}-{NUM}`
- `OFR-{customerid.name}-{NUM}`

The service also supports configurable digit padding for `{NUM}`.

---

## ⚙️ `AutoNumberingService`

`AutoNumberingService` is the framework service used to generate numbers.

Main methods:

- `GetAutoNumber(entityName, entityId, parentEntityId)`
- `GetTransactionAutoNumber(entityName, entityId, parentEntityId, groupingValue)`

`GetAutoNumber(...)` uses the `pl_GetAutoNumber` Dataverse request.

`GetTransactionAutoNumber(...)` builds the number from configuration and returns both:

- generated number
- update request for persisting the incremented counter

This makes it possible to generate the number and update the sequence safely.

---

## 🔄 Transactional autonumbering

The transactional variant returns:

- `Number`
- `Request`

`Number` is the generated formatted value.

`Request` is an `UpdateRequest` prepared with concurrency control.

This is important because autonumbering must not silently overwrite sequence state when multiple records are generated at the same time.

The update request uses row-version matching, so sequence updates are protected against conflicting writes.

> [!IMPORTANT]
> If you use transactional autonumbering, do not ignore the returned update request.
> It is part of the numbering consistency model.

---

## 💻 Examples

### Generate a simple autonumber

    protected override void DoExecute()
    {
        var service = new AutoNumberingService(OrganizationServiceProvider.Admin);
        var number = service.GetAutoNumber("salesorder", ContextEntity.Id, null);

        ContextEntity["ordernumber"] = number;
        AddLogDetail("GeneratedNumber", number);
    }

### Generate a transactional autonumber

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

### Parent-based autonumbering

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

---

## ✅ Design recommendations

- keep autonumbering configuration in Dataverse
- use transactional autonumbering when sequence consistency matters
- execute the returned update request when using the transactional variant
- use grouping when separate sequence ranges are required
- use parent-based configuration when numbering depends on a related record
- keep format strings readable and predictable

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

- [Task Model](./plugins/task-model.md)
- [Runtime Configuration](./configuration.md)
- [Data Access](./plugins/data-access.md)