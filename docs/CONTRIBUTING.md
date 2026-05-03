# Contributing to Pillaro Dataverse Plugin Framework

Thank you for your interest in contributing! This guide will help you get started quickly.

---

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [Branching Strategy](#branching-strategy)
- [How to Contribute](#how-to-contribute)
- [Code Guidelines](#code-guidelines)
- [Pull Request Process](#pull-request-process)
- [Reporting Issues](#reporting-issues)

---

## Code of Conduct

By participating in this project, you agree to abide by our [Code of Conduct](../CODE_OF_CONDUCT.md). Please read it before contributing.

---

## Getting Started

### Prerequisites

- **Visual Studio 2022** or later
- **.NET Framework 4.6.2** SDK (for plugin assemblies)
- **.NET 8 / .NET 10** SDK (for test projects)
- **Microsoft Dataverse environment** (for integration testing)

### Repository Structure

~~~
├── src/                    # Framework source code
├── examples/               # Example implementations
├── tests/                  # Test projects
└── docs/                   # Documentation
~~~

---

## Development Setup

1. **Clone the repository**

   ~~~bash
   git clone https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework.git
   cd Dataverse-Plugin-Framework
   ~~~

2. **Open the solution in Visual Studio**

   Open `Pillaro.Dataverse.PluginFramework.sln` in Visual Studio.

3. **Build the solution**

   Use __Build > Build Solution__ (Ctrl+Shift+B) in Visual Studio.

4. **Run tests**

   Use __Test > Run All Tests__ in Visual Studio.

---

## Branching Strategy

This repository follows a structured branching workflow:

### Main Branches

- **`main`** — Production-ready code. Contains only stable releases.
- **`develop`** — Integration branch for ongoing development. **All pull requests must target this branch.**

### Branch Flow

1. Contributors create feature branches from `develop`
2. Pull requests are submitted **to `develop`** (not `main`)
3. After review and merge to `develop`, changes are tested
4. Periodic releases flow from `develop` → `main` via release process
5. NuGet packages are published automatically from `main`

> [!IMPORTANT]
> **Always create pull requests against the `develop` branch**, not `main`. Direct PRs to `main` will be redirected or closed.

### Feature Branches

Use the naming convention:

- `feature/your-feature-name` — New features
- `bugfix/issue-description` — Bug fixes
- `docs/documentation-topic` — Documentation updates

---

## How to Contribute

### Types of Contributions

We welcome:

- **Bug fixes** — Fix issues in existing functionality
- **Features** — Add new capabilities (discuss in an issue first)
- **Documentation** — Improve or add documentation
- **Tests** — Add or improve test coverage

### Contribution Workflow

1. **Check existing issues** — Avoid duplicating work
2. **Open an issue** — Discuss significant changes before implementation
3. **Fork the repository** — Create your own copy
4. **Create a feature branch from `develop`** — Use the naming convention described above
5. **Make your changes** — Follow code guidelines
6. **Write/update tests** — Maintain or improve test coverage
7. **Submit a pull request to `develop`** — Reference the related issue

---

## Code Guidelines

### General Principles

- Follow existing architecture and patterns strictly
- Keep logic simple, readable, and maintainable
- Prefer composition over complexity
- Write meaningful business logic (avoid demo-only code)

### Coding Standards

- **Target Framework**: .NET Framework 4.6.2 (sandbox compatible)
- **C# Language Version**: Use `<LangVersion>latest</LangVersion>` in all plugin projects
- **Entity Access**: Always use early-bound with `Logic.EntityName` prefix
- **Single Assembly**: Output must be ILMerge compatible

### Build Quality Gate

All contributions must build with **zero warnings** from compiler and analyzers.

Common issues to avoid:

| Code | Issue |
|------|-------|
| SYSLIB1045 | No `[GeneratedRegex]` in sandbox code |
| CA1862 | Use `string.Equals(..., StringComparison.OrdinalIgnoreCase)` |
| CA1861 | No repeated inline array allocations |
| CA1822 | Mark static when no instance access |
| IDE0005 | Remove unused usings |
| IDE0028 | Use collection expressions |

> [!TIP]
> After building in Visual Studio, check the __Output__ window for warnings. Address all warnings before submitting a PR.

### Plugin Architecture

- **Plugins**: Register tasks only, no business logic
- **Tasks**: All business logic, single responsibility, composable

---

## Pull Request Process

### Before Submitting

- ✅ Code compiles without warnings
- ✅ All existing tests pass
- ✅ New functionality includes tests (if applicable)
- ✅ Documentation updated (if needed)
- ✅ Commit messages are clear and descriptive
- ✅ **PR targets the `develop` branch**

### PR Requirements

1. **Base Branch**: Always set to `develop` (not `main`)
2. **Title**: Clear, concise description of the change
3. **Description**: Explain what and why (not how)
4. **Issue Reference**: Link to related issue using `Fixes #123` or `Closes #123`
5. **Small Scope**: Keep PRs focused on a single concern

### Review Process

1. At least one maintainer approval required
2. Address review feedback promptly
3. All checks must pass before merge
4. Changes are merged to `develop` first
5. Release to `main` and NuGet publishing happens through the release flow

---

## Reporting Issues

### Bug Reports

Include the following information:

- **Description**: Clear summary of the issue
- **Steps to Reproduce**: Minimal steps to trigger the bug
- **Expected Behavior**: What should happen
- **Actual Behavior**: What actually happens
- **Environment**: Visual Studio version, .NET version, Dataverse version

### Feature Requests

- **Problem Statement**: What problem does this solve?
- **Proposed Solution**: How should it work?
- **Alternatives Considered**: Other approaches you evaluated

---

Thank you for contributing to Pillaro Dataverse Plugin Framework!
