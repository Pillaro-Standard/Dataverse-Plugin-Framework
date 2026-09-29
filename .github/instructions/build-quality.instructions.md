---
name: Build quality
description: Zero-warning build, analyzer rules and sandbox constraints for all C# code.
applyTo: "**/*.cs"
---

# Build quality

Full rules and reasons: [`80-build-quality.md`](../../docs/ai/rules/80-build-quality.md). Exact build
and validation commands: [`verify.md`](../../docs/ai/verify.md).

## Rules

- Zero warnings from the compiler and analyzers (PF-BUILD-001). A warning is fixed at its cause,
  never suppressed with `#pragma` or `NoWarn` (PF-BUILD-005).
- Plugin code targets `net462` and must stay ILMerge-compatible: no dependency that is invalid in the
  plugin sandbox (PF-BUILD-003/004).
- The merged `Plugins` DLL must carry `ProxyTypesAssemblyAttribute`. If early-bound calls fail in
  Dataverse with "not a known entity type", fix the assembly — never rewrite the code to late-bound
  (PF-BUILD-006).
- A new `.cs` file is added to its project file. The template's `Logic` and `Tests` projects and the
  `/examples` projects list their compile items explicitly; an unlisted file is silently not built
  (PF-BUILD-007).

## Analyzer messages to avoid

| Id | Instead |
|---|---|
| `SYSLIB1045` | no `[GeneratedRegex]` in the sandbox — prefer string APIs to regex |
| `CA1862`, `CA1307`, `CA1309` | `string.Equals(a, b, StringComparison.OrdinalIgnoreCase)` and an explicit `StringComparison` everywhere — never `ToLower()`/`ToUpper()` to compare |
| `CA1861` | a constant array used repeatedly goes to a `static readonly` field |
| `CA1822` | a member that uses no instance state is `static` |
| `IDE0005` | no unused `using` directives |
| `IDE0028` | collection expressions: `["Create", "Update"]` |
| `IDE0059`, `IDE0060` | no unused assignments or parameters |

Also: no unused locals or private members, and no `TODO` placeholders in finished code. Match the
style of the reference files in [`project-setup.md`](../project-setup.md).
