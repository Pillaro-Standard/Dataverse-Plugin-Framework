# Notes for certification

What goes into the **Notes for certification** box on the Review and submit page in Partner
Center. Partner Center warns that it does not keep these notes across resubmissions, so this
file is where they live.

The box takes **2,500 characters at most**. The text below is 2,366.

Rewrite it for each submission: the certification team reads it to know what changed since the
report they last sent, and a stale note is worse than none.

---

## Submission of 26 September 2026

Answers the certification report of 25 September 2026, which failed the offer on policy
100.5.13 (Support and Help) because the Customer support link pointed at the GitHub issue
tracker rather than a page with contact details. The report suggested
`https://pillaro.cz/kontakty`, which is what the link now uses.

```text
RESUBMISSION AFTER THE CERTIFICATION REPORT OF 25 SEPTEMBER 2026 (policy 100.5.13, Support and Help)

1. WHAT WAS FIXED
The Customer support link now points to https://pillaro.cz/kontakty, as suggested in the report. That page provides a telephone number, an email address and a contact form. It is no longer the GitHub issue tracker. The Help link remains the product documentation on GitHub, so the two differ and each leads directly to its own resource.

2. OTHER CHANGES
App version is now 1.0.0.3, matching the framework solution in the package.

The package at the same Azure Blob URL was replaced on 26 September 2026, so please validate the current content at that URL. Two changes:
(a) Pillaro Framework solution 1.0.0.2 to 1.0.0.3. Label-only: several labels on the Autonumbering table were in Czech under language code 1033 and are now English. No schema, plug-in registration or behaviour change, and the plug-in assembly and web resources are byte-identical to 1.0.0.2.
(b) The package now creates the three configuration records the examples read, in PackageImportExtension.AfterPrimaryImport: the MinimalSeverityLevel and ForbiddenWords runtime settings and the primary autonumbering configuration for Task. Each is created only when missing. Previously they had to be created by hand, so the scenarios in the functional document failed on a fresh install.

3. TESTING
No test account, licence key, external service or sign-up is required. The offer is free and Apache-2.0, with no purchase and no in-app purchase.

Install into any Dataverse environment as System Administrator. The install creates the configuration automatically. Section 5 of the end-to-end functional document lists the expected records and values; section 6 has five scenarios on the standard Contact and Task tables with steps and expected results.

Verified on 26 September 2026 on an environment with no previous Pillaro installation: both solutions install, all three records are created with the documented values, and the Autonumbering labels are English.

4. SECURITY
The package runs deployment code only in AfterPrimaryImport, and only to create those three records. It reads nothing else, opens no outbound connection, needs no S2S or CRM Secure Store access, creates no service account, and transmits no data anywhere. Section 2 of the functional document covers this.
```

## What the rest of the offer said at that point

| Field | Value |
|---|---|
| Help link | `https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/tree/main/docs` |
| Support link | `https://pillaro.cz/kontakty` |
| Privacy policy | `https://pillaro.cz/privacy-policy---dataverse-plugin-framework` |
| App version | 1.0.0.3 |
| Package URL | the Azure Blob SAS URL, expiry 31 December 2032 |
| Marketing only change | off, because the package content changed |

`Marketing only change` skips full recertification. It is only honest when nothing but listing
text moved. Any new package at the blob URL, even under the same URL, means it stays off.
