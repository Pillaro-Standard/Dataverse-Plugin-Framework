<!-- Managed by pillaro-dv ai-sync (Pillaro Dataverse Plugin Framework). Do not edit: the next sync overwrites this file. Your own rules belong in AGENTS.md, outside the managed block. -->

# Process and Security

> [!IMPORTANT]
> Read [`AGENTS.md`](../../../AGENTS.md) first. This file is one entry in its rule catalog.

| ID | Rule | Source |
|---|---|---|
| PF-PROC-001 | PRs target `develop`, never `main`. Branch naming: `feature/` \| `bugfix/` \| `docs/`. | [CONTRIBUTING.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/CONTRIBUTING.md) |
| PF-PROC-002 | A behavior change gets an entry in `CHANGELOG.md`. | [CHANGELOG.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/CHANGELOG.md), [versioning.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/versioning.md) |
| PF-PROC-003 | A public API change needs a versioning-impact check. | [versioning.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/versioning.md) |
| PF-PROC-004 | NEVER commit a connection string, a secret, or `key.snk` content. Use user-secrets locally. | [testing.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/tests/testing.md), [SECURITY.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/SECURITY.md) |
| PF-PROC-005 | AI NEVER runs deployment to Dataverse. That is a human action, run through the team's own pipeline. | [deployment-plugins.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/deployment-plugins.md) |
| PF-PROC-006 | Do not discuss vulnerabilities in a public issue. | [SECURITY.md](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/SECURITY.md) |

## Why PF-PROC-005 is a hard boundary, not a style preference

Deployment writes plugin assemblies and registration metadata into a real Dataverse environment,
where other people's work and other running solutions are live. The CLI's `manifest` and `validate`
commands give you a real, enforceable offline gate on registration correctness (see
[`.pillaro/ai/verify.md`](../verify.md)) — use those to check your own work before asking a human to run
`deploy`. Never treat "I could technically run the deploy command" as license to do so.

## ➡️ Related

- [CONTRIBUTING](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/CONTRIBUTING.md)
- [Deployment Plugins](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/deployment-plugins.md)
- [SECURITY](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/SECURITY.md)
