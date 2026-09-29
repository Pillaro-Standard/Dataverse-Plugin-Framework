# Copilot Instructions

> [!IMPORTANT]
> Canonical rules and rationale live in `AGENTS.md` and `docs/ai/rules/` in this repository.
> The files under `.github/` are the GitHub Copilot-native form of the same rules. If a rule here
> ever disagrees with `docs/ai/rules/`, `docs/ai/rules/` wins — fix this file to match, don't fix
> the rules to match this file.

## Purpose

This repository contains a Dataverse Plugin Framework.

Repository-specific paths, namespaces, runtime constraints, and reference implementations are defined in:

- .github/project-setup.md

Instruction files are scoped by responsibility:

- Project setup → .github/project-setup.md
- Requirement → plan (plugins/tasks/tests) breakdown, for consultants without C# → .github/prompts/analyze-and-plan.prompt.md
- Plugin generation → .github/instructions/plugins.instructions.md
- Task generation → .github/instructions/task.instructions.md
- Test generation → .github/instructions/tests.instructions.md
- Coding best practices → .github/instructions/coding-best-practices.instructions.md

Always follow the instruction file that matches the requested output.
Always apply coding best practices for every generated C# output.
Always resolve concrete project-specific values from .github/project-setup.md.

---

## General Principles

- Follow existing architecture and patterns strictly
- Keep logic simple, readable, and maintainable
- Prefer composition over complexity
- Generate only meaningful business logic
- Avoid artificial, demo-only, or placeholder behavior

---

## Plugins
- Register tasks only
- No business logic
- One pattern per plugin: entity-based OR functionality-based

### Tasks
- All business logic
- Single responsibility
- Composable and deterministic

---

## Entity Access

- Always use early-bound: `Logic.<EntityName>`
- Validate entity existence in `{EntityConfigPath}` → key `{EntitiesConfigKey}`
- If entity missing in early-bound file → stop and require regeneration

---

## Build Quality Gate

Zero warnings from compiler and analyzers.

Must avoid:
- `SYSLIB1045`: No `[GeneratedRegex]` in sandbox code
- `CA1862`: Use `string.Equals(..., StringComparison.OrdinalIgnoreCase)`
- `CA1861`: No repeated inline array allocations
- `CA1822`: Mark static when no instance access
- `IDE0005`: Remove unused usings
- `IDE0028`: Use collection expressions

---

## Constraints

- Target: .NET Framework 4.6.2 (sandbox)
- Single assembly output (ILMerge)
- No dependency injection in tasks

---

## Markdown Formatting Guidelines

When generating Markdown files (e.g. README.md, CONTRIBUTING.md), follow these rules strictly:

- Always output a **single complete Markdown document**
- Do not split the output into multiple parts or messages
- Do not include any explanations, comments, or text outside the Markdown content
- Ensure the document is **valid and well-structured Markdown**

#### Structure
- Use correct heading hierarchy (`#`, `##`, `###`, …)
- Ensure lists are properly formatted and indented
- Keep consistent spacing between sections

#### Code Blocks
- Always use `~~~` for code fences (never use triple backticks)
- Always specify language when applicable (e.g. `~~~csharp`)
- Ensure every code block is properly closed
- Never nest unescaped code blocks

#### Output Quality
- The result must be directly usable as a `.md` file without any modification
- Validate that:
  - all blocks are closed
  - no broken formatting exists
  - the document renders correctly in standard Markdown viewers

#### Behavior
- Act as if you are generating a file that will be committed directly to the repository
- Do not explain what you are doing
- Do not ask questions
- Only output the final Markdown content

When generating markdown files that contain code blocks, always use tildes (~~~) for outer code fences and backticks (```) for inner code examples to prevent markdown parsing conflicts. Never nest identical fence characters.