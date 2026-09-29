## Data Access (CRITICAL)

Use repository-specific values from `.github/project-setup.md`.

Use ONLY:
- DataService
- QueryService

Primary access point for Dataverse MUST be:

DataService.OrganizationService

---

### REQUIRED USAGE

All direct Dataverse operations MUST use:

DataService.OrganizationService

Example:

- Create
- Update
- Retrieve

---

### FORBIDDEN

Do NOT:

- resolve IOrganizationService from DI container
  (e.g. testFixture.Container.Resolve<IOrganizationService>())

- inject IOrganizationService manually

- instantiate OrganizationService directly

- bypass DataService

---

### ENTITY CREATION

Entity creation MUST be done via:

DataService.CreateTestEntity(...)

This ensures automatic cleanup via TestBase.

---

### CLEANUP

Test data cleanup is handled automatically.

Do NOT implement manual cleanup.

---

### INTEGRATION TESTS ONLY

- Do NOT mock anything
- Do NOT fake services
- Always use real Dataverse via DataService

---

## Analyzer Compliance (CRITICAL)

Generated test code MUST compile with zero warnings/messages.

Avoid at minimum:
- SYSLIB1045: follow `.github/project-setup.md` sandbox compatibility rules; do not introduce `[GeneratedRegex(...)]` into plugin/task-oriented code
- IDE0057: simplify `Substring` patterns
- CA1862: use `string.Equals(..., StringComparison...)`
- CA1861: avoid repeated inline constant array allocations
- CA1822: mark members `static` when possible
- IDE0028: prefer collection expressions/initializers
- CA1307/CA1309: always specify correct `StringComparison`
- IDE0005: remove unnecessary `using` directives
- IDE0059/IDE0060: avoid unnecessary assignments and unused parameters

Also ensure:
- no unused usings
- no unused locals/private members

---