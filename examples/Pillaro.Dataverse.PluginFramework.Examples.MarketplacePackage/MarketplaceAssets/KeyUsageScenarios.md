# Pillaro Dataverse Plugin Framework — key usage scenarios

**Publisher:** Pillaro Labs s.r.o.
**Offer:** Pillaro Dataverse Plugin Framework

The scenarios below are the ones the certification team can verify in a test environment.
Full steps, expected results and the required configuration are in the end-to-end functional
document submitted with this offer.

| # | Scenario | Where it runs | Verifiable outcome |
|---|---|---|---|
| 1 | Validation driven by a runtime setting | Contact create or update | Saving a Contact whose first or last name matches a word in the `ForbiddenWords` runtime setting is blocked with a validation error. Changing the setting changes the behaviour with no redeployment. |
| 2 | Derived value written on update | Contact create or update | **Address 1: Name** is generated from the Contact address fields when the record is saved. |
| 3 | Autonumbering | Task create | The Task subject is prefixed with a generated number in the format configured in the Autonumberings record, for example `26-09-21-001000`. Numbers increment and are never reused. |
| 4 | Related record synchronisation | Task create, update or completion | The parent Contact description is updated with the latest planned or completed activity date from the related Task. |
| 5 | Diagnostic logging | Any of the above | Plugin Log records in the Pillaro Plugin Framework app show the plug-in and tasks that ran, their messages, input context, execution time and execution depth, including the reason a validation failed. |
| 6 | Administration | Pillaro Plugin Framework model-driven app | Runtime Settings, Autonumberings and Plugin Logs are created, edited and read from the installed app. The four Pillaro security roles grant read-only or full access to those records. |

**Prerequisites for all scenarios:** the package installed into a Dataverse environment and a
System Administrator user. The install creates the configuration the scenarios need — the
`MinimalSeverityLevel` and `ForbiddenWords` runtime settings and the Autonumbering record for
the Task table — so there is nothing to set up by hand. No external service, licence key or
sign-up is required.
