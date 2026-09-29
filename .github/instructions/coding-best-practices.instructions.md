---
applyTo: "**/*.cs"
---

# Coding Best Practices

Use repository-specific values from `.github/project-setup.md`.

## Quality Goal

All generated C# code MUST compile with zero warnings and zero analyzer messages.

Treat compiler, IDE, CA, and SYSLIB diagnostics as blocking quality gates.

This repository targets the runtime and isolation mode defined in `.github/project-setup.md`.
Favor sandbox-compatible code over satisfying analyzer suggestions mechanically.

---

## Mandatory Analyzer Rules

Generated code MUST avoid at minimum:

- SYSLIB1045: do not respond by introducing [GeneratedRegex(...)] in plugin/task code; instead avoid regex where possible and prefer non-regex string logic
- IDE0057: avoid manual Substring patterns when range/index expressions are clearer
- CA1862: use explicit string comparison APIs (for example string.Equals(..., StringComparison.OrdinalIgnoreCase)) instead of ToLower()/ToUpper() normalization
- CA1861: avoid repeated constant array allocations in call sites; move to static readonly field when reused
- CA1822: mark members as static when they do not access instance state
- IDE0028: prefer collection expressions/initializers over verbose collection setup
- CA1307/CA1309: always use explicit and appropriate StringComparison for string operations
- IDE0005: remove unnecessary using directives
- IDE0059/IDE0060: avoid unnecessary assignments and unused parameters

---

## String and Comparison Rules

- Never compare against lower-cased/upper-cased transformed strings.
- Always use explicit StringComparison.
- Use OrdinalIgnoreCase unless assignment explicitly requires culture-aware behavior.
- Prefer StartsWith/EndsWith/Contains/IndexOf/Split/Replace over regex when solving text problems in plugin/task code.

---

## Allocation and Readability Rules

- Avoid unnecessary allocations in hot or repeated paths.
- Prefer collection expressions for static message/enum lists.
- Extract repeatedly reused constant argument arrays into static readonly fields.
- Keep code concise, but not at the expense of clarity.
- Do not introduce modern attributes or APIs that are unsuitable for Dataverse Sandbox plug-ins.

---

## Hygiene Rules

- No unused usings.
- No unused local variables.
- No dead private members.
- No TODO placeholders in final generated code.

---

## Generator Self-Check (Required)

Before finalizing generated output, verify:

1. No rule above is violated.
2. No validation logic leaks into execution path where validators are required.
3. String and logical name comparisons use framework constants and explicit StringComparison.
4. Output is ready to pass build/analyzers without additional cleanup.
5. No sandbox-incompatible feature was introduced just to satisfy an analyzer.
