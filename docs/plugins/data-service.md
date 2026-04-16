# DataService

> [!IMPORTANT]
> `DataService` is a framework data layer built on top of standard Dataverse service access.
> It provides prepared helpers and extensions for common data work inside tasks.

---

## 📑 Navigation

- [🔍 What `DataService` is](#-what-dataservice-is)
- [🎯 Why it matters](#-why-it-matters)
- [🧱 Main capability areas](#-main-capability-areas)
- [🔎 Query](#-query)
- [📦 Multiple requests](#-multiple-requests)
- [🔄 Transactional operations](#-transactional-operations)
- [🧭 Metadata and option sets](#-metadata-and-option-sets)
- [🗂️ Low-level record helpers](#️-low-level-record-helpers)
- [💻 Examples](#-examples)
- [✅ Design recommendations](#-design-recommendations)
- [➡️ Related documents](#️-related-documents)

---

## 🔍 What `DataService` is

`DataService` is part of the framework data access model.

It is not just a thin wrapper over `IOrganizationService`.
It provides prepared helpers and extensions for common Dataverse scenarios.

It includes support for areas such as:

- query
- multiple request batching
- transactional helper operations
- metadata and option set access
- low-level record loading helpers

In daily development, the most important part is usually **query**.

---

## 🎯 Why it matters

Without a prepared data layer, plugin code often ends up with repeated boilerplate for:

- setting up context-based access
- building queries
- loading records in a reusable way
- batching operations
- resolving metadata and option set values

`DataService` reduces that repeated setup.

The biggest practical value is that query access is already prepared and easier to use in normal task code.

> [!NOTE]
> Some parts of `DataService` are used frequently, especially query.
> Other parts are more situational, but still useful when the task needs them.

---

## 🧱 Main capability areas

The main capability areas of `DataService` are:

| Area | Purpose | Typical frequency |
|---|---|---|
| Query | Query Dataverse data through the framework query model | Very frequent |
| Multiple requests | Collect and execute batched Dataverse requests | Situational |
| Transactional operations | Run selected operations outside the normal transaction flow | Situational |
| Metadata and option sets | Resolve metadata, labels, and option values | Situational |
| Low-level record helpers | Load records directly in simple late-bound scenarios | Situational |

> [!IMPORTANT]
> Query is the most important day-to-day part of `DataService`.
> That is usually the main reason developers reach for it in normal task implementation.

---

## 🔎 Query

Query is the most important practical part of `DataService`.

The framework provides:

    Query<TEntity>()

This gives you a prepared query entry point for Dataverse data.

Why this matters:

- no repeated manual service context setup
- less repeated query boilerplate
- easier daily work inside tasks
- clearer query access in framework-style code

`Query<TEntity>()` is especially valuable because it removes a lot of the repetitive setup that developers otherwise tend to rebuild around standard Dataverse access.

> [!IMPORTANT]
> For most day-to-day task implementation, query is the biggest practical value of `DataService`.

> [!TIP]
> When documenting or teaching framework data access, start with query first.
> That is the part developers will use most often.

### Query example
~~~
var contacts = DataServiceProvider.User
    .Query<Contact>()
    .Where(x => x.LastName == "Smith")
    .ToList();
~~~
This is the most typical day-to-day usage pattern:

- choose a context through `DataServiceProvider`
- call `Query<TEntity>()`
- express the query in task code
- continue with task logic

---

## 📦 Multiple requests

`DataService` supports batching through a multiple request model.

Relevant capabilities include:

- storing requests under a key
- retrieving request collections
- executing batched requests
- controlling batch behavior through settings
- automatic batching by configured batch size

This is useful when:

- one task needs to queue multiple Dataverse requests
- you want controlled batched execution
- you need to work with grouped request collections instead of immediate one-by-one execution

This is not as common as query, but it is useful when a task performs larger sets of coordinated operations.

### Multiple request example

    protected override void DoExecute()
    {
        var requestKey = Guid.NewGuid();

        DataServiceProvider.Admin.AddRequest(requestKey, new UpdateRequest
        {
            Target = new Entity("contact")
            {
                Id = ContextEntity.Id
            }
        });

        DataServiceProvider.Admin.ExecuteMultipleRequest(requestKey);

        AddLogMessageLine("Batched request executed.");
    }

This pattern is useful when requests should be collected and executed as a batch.

---

## 🔄 Transactional operations

`DataService` also contains helper operations for selected work outside the normal transaction flow.

Relevant examples include:

- `CreateOutsideTransaction(...)`
- `UpdateOutsideTransaction(...)`

This is useful when:

- the task intentionally needs work outside the normal transactional path
- you want explicit control over a specific create or update behavior
- the solution design requires this pattern for a specific reason

> [!WARNING]
> Use outside-transaction helpers intentionally.
> They are powerful, but they should not become the default pattern for normal task implementation.

### Transactional operation example

    protected override void DoExecute()
    {
        var update = new Entity("contact")
        {
            Id = ContextEntity.Id
        };

        update["description"] = "Updated outside the main transaction flow.";

        DataServiceProvider.Admin.UpdateOutsideTransaction(update);

        AddLogMessageLine("Record updated outside transaction.");
    }

Use this pattern only when the solution design explicitly requires it.

---

## 🧭 Metadata and option sets

`DataService` also includes metadata-oriented helpers.

Relevant examples include:

- user UI language lookup
- localized option label resolution
- option set value lookup by text
- entity metadata retrieval

This is useful when:

- you need localized labels
- you work with option sets dynamically
- you need metadata-driven logic
- you need access to Dataverse metadata without rebuilding the request logic manually

This is a valuable part of the service, but it is used less frequently than query.

### Metadata example

    protected override void DoExecute()
    {
        var languageCode = DataServiceProvider.User.GetUserUiLanguageCode(TaskContext.PluginExecutionContext.UserId);

        AddLogMessageLine($"User language code: {languageCode}");
    }

This pattern is useful when task behavior depends on metadata or localization.

---

## 🗂️ Low-level record helpers

`DataService` also contains low-level record loading helpers.

These are still useful, especially in late-bound scenarios.

Typical examples include:

- loading one record by attribute
- loading multiple records by attributes
- simple direct record fetch helpers

These helpers are not the main architectural value of the framework, but they still have practical use.

This is especially true when:

- the implementation is late-bound
- the query need is simple
- a lightweight direct load is enough

> [!NOTE]
> These helpers still make sense in some solutions.
> They are not the most important part of `DataService`, but they remain useful in the right context.

### Low-level helper example

    protected override void DoExecute()
    {
        var existingContact = DataServiceProvider.User.LoadRecord(
            "contact",
            "emailaddress1",
            "john.smith@example.com");

        if (existingContact != null)
        {
            AddLogMessageLine("Existing contact found by email.");
        }
    }

This pattern is useful when a simple late-bound lookup is enough.

---

## 💻 Examples

### Query-first example

    protected override void DoExecute()
    {
        var contacts = DataServiceProvider.User
            .Query<Contact>()
            .Where(x => x.LastName == "Smith")
            .ToList();

        if (contacts.Count == 0)
        {
            AddLogMessageLine("No matching contacts found.");
            return;
        }

        AddLogDetail("Matching contacts count", contacts.Count);
        AddLogMessageLine("Query completed successfully.");
    }

### Mixed access example

    protected override void DoExecute()
    {
        var adminData = DataServiceProvider.Admin;
        var initiatingUserService = OrganizationServiceProvider.InitiatingUser;

        var contacts = adminData
            .Query<Contact>()
            .Where(x => x.LastName == "Smith")
            .ToList();

        AddLogDetail("Contacts loaded as admin", contacts.Count);
        AddLogMessageLine("Mixed access model used in task execution.");
    }

These examples show:

- prepared access through `DataServiceProvider`
- explicit context selection
- query as the primary daily-use pattern
- task-level logging around data work

---

## ✅ Design recommendations

Use these rules by default:

- treat `DataService` as the normal framework data layer
- start with query when implementing everyday task logic
- use multiple requests only when batching really adds value
- use outside-transaction helpers intentionally, not by default
- use metadata helpers when dynamic metadata-based behavior is actually needed
- keep low-level record helpers for simple or late-bound scenarios where they truly fit

When deciding how to work with Dataverse data:

- prefer the prepared framework data model over rebuilding access patterns manually
- keep the selected security context explicit
- keep query readable and close to the task responsibility
- do not mix several unrelated access patterns into one task without a good reason

> [!TIP]
> In most real tasks, the first question is not “Which helper exists?”
> The first question is “What query do I need?”
> Start there.

---

## ➡️ Related documents

Continue with:

- [Data Access](./data-access.md)
- [Task Model](./task-model.md)
- [Validation Model](./validation.md)
- [Getting Started](./getting-started.md)
- [Architecture](../architecture.md)