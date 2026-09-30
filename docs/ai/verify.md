# Verification — exact commands

> [!IMPORTANT]
> Read [`AGENTS.md`](../../AGENTS.md) first. This file gives literal commands, not descriptions —
> "run the build" is not enough for an agent to act on reliably, especially on the Windows/MSBuild
> stack this framework targets.

Solutions come in two project formats:

- **A solution built from the template** (`dotnet new` or the VS template) — SDK-style `net462`
  projects. `dotnet build` works directly.
- **An older solution in the legacy (non-SDK) format** — like the framework's own `/examples`.
  `dotnet build` does not reliably build these projects; use `MSBuild.exe` from a Visual Studio
  install.

If you don't know which one you're in, check the first line of the `.csproj`:
`<Project Sdk="Microsoft.NET.Sdk">` → SDK-style, `dotnet build` works.
`<Project ToolsVersion="15.0" ...>` → legacy format, use MSBuild.

---

## Fast loop — a solution built from the template

```powershell
dotnet build "path\to\YourSolution.Logic\YourSolution.Logic.csproj" -c Release
```

Zero warnings, zero errors (PF-BUILD-001). A warning is a stop, not a note — see
[`docs/ai/rules/80-build-quality.md`](./rules/80-build-quality.md).

Then the offline registration gate (no Dataverse connection needed). Build `Plugins` too, and run
the CLI bundled in the framework package — the `$cliDll` line of
`Plugins\Tools\Deployment\DeployPlugins.ps1` holds its path:

```powershell
$cliDll = (Select-String -Path "path\to\YourSolution.Plugins\Tools\Deployment\DeployPlugins.ps1" -Pattern "^\`$cliDll = '(.+)'").Matches[0].Groups[1].Value
dotnet $cliDll --help
dotnet $cliDll manifest --assembly "path\to\YourSolution.Plugins\bin\Release\YourSolution.Plugins.dll" --output artifacts/plugin-manifest.json
dotnet $cliDll validate --manifest artifacts/plugin-manifest.json
```

`manifest` and `validate` ship from the framework release after 1.2.2. If `--help` lists only
`deploy`, skip them and say so in your report; `deploy` runs the same validation before it writes
anything. Template projects put build output directly in `bin\Release\` (no `net462` subfolder).

Exit code `0` on both commands is the only acceptable result. `manifest` exits `1`/`3` on a missing
assembly or validation errors baked into the manifest; `validate` exits `2` for a missing file, `3`
for validation failures. Treat any non-zero exit code as a hard stop — read the printed errors, they
name the specific rule violated (see [`docs/ai/rules/60-registration.md`](./rules/60-registration.md)).

## Fast loop — a legacy-format solution

Locate MSBuild once per session (no need to open Visual Studio):

```powershell
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe"
```

Build the solution (quote the path if the file name has spaces):

```powershell
& $msbuild "YourSolution.sln" /t:Build /p:Configuration=Release /m
```

Build `Logic` and `Plugins` **in that order** if building a single project instead of the whole
solution — `Plugins` references `Logic`'s output.

If a `dotnet build`/`dotnet test` was ever run against legacy projects, their restore state in
`obj/` is left incompatible with MSBuild (`Your project file doesn't list 'win' as a
"RuntimeIdentifier"`). Recover with `& $msbuild "YourSolution.sln" /t:Restore`, then build again.

<!-- pillaro:framework-only -->
## Framework repository only

The framework's own solution is `Dataverse Plugin Framework.sln` (legacy format for `/examples`).
To check PF-BUILD-001 explicitly on a change (measured safe for this solution — see the F3-02 entry
in [ai-readiness-fix-plan.md](./ai-readiness-fix-plan.md)):

```powershell
& $msbuild "Dataverse Plugin Framework.sln" /t:Rebuild /p:Configuration=Release /p:TreatWarningsAsErrors=true
```

### CLI and framework unit/offline tests

```powershell
dotnet build "tests\Pillaro.Dataverse.PluginFramework.Tests\Pillaro.Dataverse.PluginFramework.Tests.csproj" -c Debug
dotnet test "tests\Pillaro.Dataverse.PluginFramework.Tests\Pillaro.Dataverse.PluginFramework.Tests.csproj" -c Debug --filter "FullyQualifiedName~PluginCommands"
```

`PluginCommands` tests (manifest/validate/router, discovery, registration diff calculation) run
offline — no live connection required. Do not widen the filter to the full suite unless you also
intend to run the slow loop below.
<!-- /pillaro:framework-only -->

## Slow loop — integration tests against a live dev environment

Only if PF-ENV-001..007 (see [`docs/ai/rules/70-testing.md`](./rules/70-testing.md)) are satisfied —
in particular, only against a dedicated dev environment, and never one you deployed to yourself.

```powershell
dotnet test "path\to\YourSolution.Tests\YourSolution.Tests.csproj" -c Debug --filter "FullyQualifiedName~YourNewTaskName"
```

- `TestFixture` verifies the connected environment's host against `ExpectedEnvironmentUrl`
  (PF-ENV-006) and throws before any test body runs if they don't match. That exception means the
  connection string points somewhere unexpected — stop and tell a human, do not work around it.
- A red test before implementation is expected and correct (TDD loop, see `AGENTS.md`). A red test
  that stays red after implementation is fixed by fixing the task, never by editing the test
  (PF-ENV-007).

## What cannot be verified this way

Deployment, plugin step registration in Dataverse, and end-to-end execution triggered by real
Dataverse events all require an actual `deploy` run — which you do not perform yourself
(PF-PROC-005). The fast loop above proves the code compiles and the registration metadata is
internally valid; it does not prove the plugin is registered or wired up in any environment. That
proof comes from the slow loop, after a human has deployed the build.

## ➡️ Related

- [AGENTS.md](../../AGENTS.md)
- [Rule catalog](./rules/)
- [CONTRIBUTING](../CONTRIBUTING.md)
