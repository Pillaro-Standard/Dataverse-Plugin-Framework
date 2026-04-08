# Contributing to Pillaro Dataverse Plugin Framework

Thank you for your interest in contributing! This document provides guidelines and instructions for contributing to the project.

---

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Setup](#development-setup)
- [How to Contribute](#how-to-contribute)
- [Code Guidelines](#code-guidelines)
- [Pull Request Process](#pull-request-process)
- [Reporting Issues](#reporting-issues)

---

## Code of Conduct

By participating in this project, you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md). Please read it before contributing.

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

   ~~~bash
   start Pillaro.Dataverse.PluginFramework.sln
   ~~~

3. **Restore NuGet packages**

   Visual Studio will restore packages automatically, or run:

   ~~~bash
   dotnet restore
   ~~~

4. **Build the solution**

   Press Ctrl+Shift+B or use **Build > Build Solution**.

5. **Run tests**

   Use **Test > Run All Tests** or:

   ~~~bash
   dotnet test
   ~~~

---

## How to Contribute

### Types of Contributions

| Type | Description |
|------|-------------|
| Bug fixes | Fix issues in existing functionality |
| Features | Add new capabilities (discuss first) |
| Documentation | Improve or add documentation |
| Tests | Add or improve test coverage |
| Refactoring | Improve code quality without changing behavior |

### Contribution Workflow

1. **Check existing issues** - Avoid duplicating work
2. **Open an issue** - Discuss significant changes before implementation
3. **Fork the repository** - Create your own copy
4. **Create a feature branch** - Branch from main
5. **Make your changes** - Follow code guidelines
6. **Write/update tests** - Maintain test coverage
7. **Submit a pull request** - Reference the related issue

---

## Code Guidelines

### General Principles

- Follow existing architecture and patterns strictly
- Keep logic simple, readable, and maintainable
- Prefer composition over complexity
- Generate only meaningful business logic

### Coding Standards

- **Target Framework**: .NET Framework 4.6.2 (sandbox compatible)
- **Entity Access**: Always use early-bound with Logic.EntityName prefix
- **Single Assembly**: Output must be ILMerge compatible

### Build Quality Gate

All contributions must pass with **zero warnings** from compiler and analyzers.

Must avoid:

| Code | Issue |
|------|-------|
| SYSLIB1045 | No [GeneratedRegex] in sandbox code |
| CA1862 | Use string.Equals(..., StringComparison.OrdinalIgnoreCase) |
| CA1861 | No repeated inline array allocations |
| CA1822 | Mark static when no instance access |
| IDE0005 | Remove unused usings |
| IDE0028 | Use collection expressions |

### Plugin Architecture

- **Plugins**: Register tasks only, no business logic
- **Tasks**: All business logic, single responsibility, composable

---

## Pull Request Process

### Before Submitting

- Code compiles without warnings
- All existing tests pass
- New functionality includes tests
- Documentation updated if needed
- Commit messages are clear and descriptive

### PR Requirements

1. **Title**: Clear, concise description of the change
2. **Description**: Explain what and why (not how)
3. **Issue Reference**: Link to related issue using Fixes #123 or Closes #123
4. **Small Scope**: Keep PRs focused on a single concern

### Review Process

1. Automated checks must pass
2. At least one maintainer approval required
3. Address review feedback promptly
4. Squash commits before merge (if requested)

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
- **Additional Context**: Screenshots, examples, references

---

## Questions?

If you have questions about contributing, feel free to:

- Open a [Discussion](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/discussions)
- Check existing [Issues](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/issues)

---

Thank you for contributing to Pillaro Dataverse Plugin Framework!
