# Notes for certification

What goes into the **Notes for certification** box on the Review and submit page in Partner
Center. Partner Center warns that it does not keep these notes across resubmissions, so this
file is where they live.

The box takes **2,500 characters at most**. The text below is 2,366.

Rewrite it for each submission: the certification team reads it to know what changed since the
report they last sent, and a stale note is worse than none. Newest submission first.

---

## Submission of 29 September 2026

A listing-only change, ticked as **Marketing only change** on the Properties page. That flag
skips full recertification and is only honest when nothing but listing text moved; here the
package is byte-for-byte the one certified on 28 September, at the same blob URL.

```text
LISTING-ONLY UPDATE. Marked as a marketing only change.

1. WHAT CHANGED
Offer name, offer description and one added product information link. Nothing else.

2. WHAT DID NOT CHANGE
The package is untouched. It is the same content at the same Azure Blob URL that was certified on 28 September 2026 and published the same day. No solution, no plug-in code, no configuration, no technical configuration setting, no pricing and no availability was changed.

3. WHY THE NAME CHANGED
The offer name is now "Pillaro Dataverse Plugin Framework - Demo". The package installs the framework runtime together with worked examples on the standard Contact and Task tables. The examples exist to demonstrate the framework; production use means consuming the framework as a NuGet package in the customer's own plug-in project. The previous name did not make that distinction and customers could reasonably have read the offer as the production delivery mechanism. The description now states it in the opening paragraph as well.

4. WHY THE DESCRIPTION WAS REWRITTEN
The previous text explained what the framework is but never told a customer what to do once the install finished. It now has two new sections:

"What happens when you install" - the two managed solutions, the new Pillaro Plugin Framework model-driven app that appears in the app list, the configuration created automatically, and the example steps registered on Contact and Task.

"Try it in five minutes" - the three things a customer can verify straight away: create or update a Contact and see the save blocked by a forbidden name from a runtime setting; create a Task on that Contact and see the generated number in its subject; open the Pillaro Plugin Framework app and read the Plugin Logs. These are the same scenarios as sections 5 and 6 of the end-to-end functional document already on file, shortened to listing length.

The rewrite also removed a heading that duplicated the offer name and a line that duplicated the search results summary. The description is now 4,396 of the 5,000 characters, down from 4,829.

5. ADDED LINK
A product information link named "GitHub repository" pointing at https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework, the public source repository for the framework.

6. TESTING
Unchanged from the previous submission. No test account, licence key, external service or sign-up is required. The offer is free and Apache-2.0 licensed, with no purchase and no in-app purchase.
```

That text is 2,471 characters.

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
