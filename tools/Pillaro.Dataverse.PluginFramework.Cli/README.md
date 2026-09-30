# Pillaro Dataverse CLI (`pillaro-dv`)

Deploys a Microsoft Dataverse plugin assembly and synchronizes its plugin step
and image registrations from the registration metadata declared in code.

Most users do not need to install this tool. It is bundled inside the
[Pillaro.Dataverse.PluginFramework](https://www.nuget.org/packages/Pillaro.Dataverse.PluginFramework)
package, which scaffolds `Tools/Deployment/DeployPlugins.bat` and
`DeployPlugins.ps1` wrappers into your plugin project on build. Install this
package only if you want `pillaro-dv` on your `PATH` — for example in a CI job
that has no plugin project checked out.

## Install

```powershell
dotnet tool install --global Pillaro.Dataverse.PluginFramework.Cli
```

## Usage

```text
pillaro-dv deploy [options]

  --settings <path>   Path to PillaroSettings.json. Defaults to the nearest
                      PillaroSettings.json found from the current directory upwards.
  --profile <name>    Deployment profile from PillaroSettings.json. Defaults to
                      the file's defaultProfile.
  --just-assembly     Deploy only the plugin assembly and skip step and image
                      synchronization.
```

```text
pillaro-dv manifest --assembly <dll> --output <json>   registration manifest from an assembly (offline)
pillaro-dv validate --manifest <json>                  validate a manifest (offline)
pillaro-dv diff --manifest <json> [options]            compare a manifest with an environment
pillaro-dv ai-sync [--root <folder>] [--check]         AI instructions into the solution (offline)
```

`ai-sync` writes the framework's AI instructions for Claude Code, GitHub Copilot and Codex into the
solution root, in the version of this CLI: `AGENTS.md`, `CLAUDE.md` and
`.github/copilot-instructions.md` (only the block between `pillaro:begin` and `pillaro:end` is
replaced), managed files in `.pillaro/ai/`, `.github/instructions/pillaro-*`,
`.claude/skills/pillaro-*` and `.agents/skills/pillaro-*`, and `.pillaro/project-setup.md` once.
`--check` changes nothing and exits with 3 when the instructions are out of date. The framework
package also generates `Tools/AI/SyncAiInstructions.bat`, which runs it. `pillaro-dv ai-sync --help`
lists its options.

## Configuration

Everything comes from `PillaroSettings.json`, which the framework package
generates next to your plugin project:

- `solution` — unique name of the target Dataverse solution
- `profiles.<profile>.pluginAssemblyPath` — path to the merged plugin assembly
- `dataverse.connectionStringEnvironmentVariable` — name of the environment
  variable holding the Dataverse connection string (`DV_CONN` by default)

The connection string itself is never stored in the settings file.

## Requirements

- .NET 8.0 runtime or later
- A Dataverse environment and an application user with permission to write
  plugin assemblies and SDK message processing steps

## Links

- [Framework repository](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework)
- [Deploying plugins](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/deployment-plugins.md)
- [Plugin registration API](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/docs/plugins/plugin-registration-api.md)
- License: Apache-2.0 — [LICENSE](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/LICENSE)
