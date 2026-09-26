# Process and Security

> [!IMPORTANT]
> Read [`AGENTS.md`](../../../AGENTS.md) first. This file is one entry in its rule catalog.

| ID | Rule | Source |
|---|---|---|
| PF-PROC-001 | PRs target `develop`, never `main`. Branch naming: `feature/` \| `bugfix/` \| `docs/`. | [CONTRIBUTING.md](../../CONTRIBUTING.md) |
| PF-PROC-002 | A behavior change gets an entry in `CHANGELOG.md`. | [CHANGELOG.md](../../../CHANGELOG.md), [versioning.md](../../versioning.md) |
| PF-PROC-003 | A public API change needs a versioning-impact check. | [versioning.md](../../versioning.md) |
| PF-PROC-004 | NEVER commit a connection string, a secret, or `key.snk` content. Use user-secrets locally. | [testing.md](../../tests/testing.md), [SECURITY.md](../../SECURITY.md) |
| PF-PROC-005 | AI NEVER runs deployment to Dataverse. That is a human action, run through the team's own pipeline. | [deployment-plugins.md](../../plugins/deployment-plugins.md) |
| PF-PROC-006 | Do not discuss vulnerabilities in a public issue. | [SECURITY.md](../../SECURITY.md) |

## Why PF-PROC-005 is a hard boundary, not a style preference

Deployment writes plugin assemblies and registration metadata into a real Dataverse environment,
where other people's work and other running solutions are live. The CLI's `manifest` and `validate`
commands give you a real, enforceable offline gate on registration correctness (see
[`docs/ai/verify.md`](../verify.md)) — use those to check your own work before asking a human to run
`deploy`. Never treat "I could technically run the deploy command" as license to do so.

## ➡️ Related

- [CONTRIBUTING](../../CONTRIBUTING.md)
- [Deployment Plugins](../../plugins/deployment-plugins.md)
- [SECURITY](../../SECURITY.md)
