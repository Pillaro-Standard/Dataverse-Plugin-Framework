# AI instructions for solutions built on the framework

`pillaro-dv ai-sync` writes the framework's AI instructions into a consuming solution, for Claude
Code, GitHub Copilot and Codex. The instructions are embedded in the CLI, so a solution always gets
the version that matches its framework package. This folder holds the sources that exist only for
consuming solutions; the rules themselves are shared with this repository.

| Source in this repository | In a consuming solution | Owner there |
|---|---|---|
| `ai/entry/AGENTS.md` | `AGENTS.md` | block replaced on sync, rest is the team's |
| `ai/entry/CLAUDE.md` | `CLAUDE.md` (imports `AGENTS.md`) | block replaced on sync, rest is the team's |
| `ai/entry/copilot-instructions.md` | `.github/copilot-instructions.md` | block replaced on sync, rest is the team's |
| `ai/skills/<skill>/SKILL.md` | `.claude/skills/<skill>/` and `.agents/skills/<skill>/` | managed |
| `ai/project-setup.md` | `.pillaro/project-setup.md`, filled with the solution's project paths | created once, then the team's |
| `docs/ai/rules/*.md`, `docs/ai/analysis-workflow.md`, `docs/ai/verify.md` | `.pillaro/ai/` | managed |
| `.github/instructions/*.instructions.md` | `.github/instructions/pillaro-*.instructions.md` | managed |
| selected files from `/examples` | `.pillaro/ai/examples/*.cs.txt` (reference copies, never compiled) | managed |

The mapping is `AiInstructionLayout` in `tools/Pillaro.Dataverse.PluginFramework.Cli/AiInstructions/`,
the list of embedded files is in the CLI's `.csproj`.

When writing these sources:

- Link with relative paths as usual. `ai-sync` points each link at the shipped copy, or at this
  repository on GitHub (the release tag) when the linked file is not shipped.
- Wrap text that applies only to this repository in `<!-- pillaro:framework-only -->` …
  `<!-- /pillaro:framework-only -->`; consuming solutions do not get it.
- After changing anything under `docs/ai/`, `.github/instructions/` or `ai/`, run
  `scripts/Update-TemplateAiInstructions.ps1` and commit the result. The project template carries a
  snapshot of the instructions, and the PR build fails when it is out of date.
