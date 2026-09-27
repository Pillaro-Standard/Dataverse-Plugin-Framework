# Build Quality Gate

> [!IMPORTANT]
> Read [`AGENTS.md`](../../../AGENTS.md) first. This file is one entry in its rule catalog.

| ID | Rule | Source |
|---|---|---|
| PF-BUILD-001 | Zero warnings from the compiler and analyzers. | [CONTRIBUTING.md](../../CONTRIBUTING.md) |
| PF-BUILD-002 | Forbidden patterns: `SYSLIB1045` (no `[GeneratedRegex]` in the sandbox), `CA1862`, `CA1861`, `CA1822`, `IDE0005`, `IDE0028`. | [CONTRIBUTING.md](../../CONTRIBUTING.md) |
| PF-BUILD-003 | Plugin projects target `net462`, `<LangVersion>latest</LangVersion>`. | [getting-started.md](../../plugins/getting-started.md) |
| PF-BUILD-004 | Output must stay ILMerge-compatible — do not add a dependency that is invalid in the plugin sandbox. | [CONTRIBUTING.md](../../CONTRIBUTING.md), [architecture.md](../../plugins/architecture.md) |
| PF-BUILD-005 | Never suppress a warning with `#pragma` / `NoWarn` as the fix — fix the cause. | — |
| PF-BUILD-006 | The merged `Plugins` DLL MUST carry `[assembly: ProxyTypesAssemblyAttribute]` (declared in the `Plugins` project, because ILMerge drops the one `pac modelbuilder` puts into `Logic`). Without it every early-bound read and write fails at runtime, never at build time. | live-verified, see [40-data-access.md](./40-data-access.md) |

## Run this before calling a change done

```powershell
dotnet build "<path-to-Logic-or-Plugins>.csproj" -c Release
```

Zero warnings, zero errors is the bar — see [`docs/ai/verify.md`](../verify.md) for the exact
commands for this repository's own projects, including the plugin-project build via MSBuild.

## What "fix the cause" looks like

```csharp
// ❌ Wrong — suppresses IDE0028 instead of fixing it
#pragma warning disable IDE0028
private static readonly string[] Attributes = new[] { "firstname", "lastname" };
#pragma warning restore IDE0028
```

```csharp
// ✅ Correct — the collection expression IDE0028 is asking for
private static readonly string[] Attributes = ["firstname", "lastname"];
```

## ➡️ Related

- [CONTRIBUTING](../../CONTRIBUTING.md)
- [`docs/ai/verify.md`](../verify.md)
