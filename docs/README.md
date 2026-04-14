# Pillaro Dataverse Plugin Framework — Documentation

> [!IMPORTANT]
> The root [README](../README.md) helps you decide whether the framework is relevant for your solution.
> This documentation section helps you understand how to use it, structure it, test it, and extend it.

> [!NOTE]
> This page is the documentation hub and working structure for the repository.
> Some documents are already available, others are planned and will be added incrementally.

---

## 📖 Table of Contents

| Section                                                    | Description                                                                 |
| ---------------------------------------------------------- | --------------------------------------------------------------------------- |
| 💬 [Questions & Discussions](#-questions--discussions)     | Ask questions, share ideas, and report issues in the repository discussions |
| 🚀 [Plugin Development](#-plugin-development)              | Build and structure Dataverse plugins with the framework                    |
| 🧪 [Test Development](#-test-development)                  | Build and run programmatic tests against Dataverse                          |
| 📦 [Release and Versioning](#-release-and-versioning)      | Release and versioning information                                          |
| ⚠️ [Known Limitations](#-known-limitations)                | Technical constraints and compatibility notes                               |
| 🤝 [Contributing](#-contributing)                          | Repository policies and contribution entry points                           |
| 🗺️ [Recommended Reading Path](#-recommended-reading-path) | Suggested reading order by goal                                             |

---

## 💬 Questions & Discussions

👉 https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/discussions

- ❓ Questions → Q&A
- 💡 Ideas → Ideas
- 🐛 Bugs → Issues

---

## 🚀 Plugin Development

Use this section when you are building Dataverse plugins with the framework.

### Start

| Document | Description | Status |
|---|---|---|
| [Getting Started](./plugins/getting-started.md) | First setup, first plugin, and first deployable assembly | ✅ |

### Core Concepts

| Document | Description | Status |
|---|---|---|
| [Architecture](./plugins/architecture.md) | High-level plugin architecture and project structure | ✅ |
| [Plugin Model](./plugins/plugin-model.md) | Plugin responsibilities and registration approach | ✅ |
| [Task Model](./plugins/task-model.md) | Task lifecycle, structure, and responsibilities | ✅ |
| [Validation Model](./plugins/validation.md) | Validation flow and validation chain design | ✅ |
| [Execution Pipeline](./plugins/execution-pipeline.md) | Plugin execution flow and task orchestration | ✅ |

### Data Access

| Document | Description | Status |
|---|---|---|
| [Data Access](./plugins/data-access.md) | Working with Dataverse data in plugin runtime | ✅ |
| [DataService](./plugins/data-service.md) | Framework data layer, query model, and related helpers | ✅ |

### Modules

| Document | Description | Status |
|---|---|---|
| [Runtime Configuration](./plugins/configuration.md) | Settings and runtime behavior configuration | ✅ |
| [Autonumbering](./plugins/autonumbering.md) | Number sequence generation and related patterns | ✅ |
| [Logging](./plugins/logging.md) | Runtime logging and diagnostics | ✅ |
| [Error Handling](./plugins/error-handling.md) | Exceptions, validation failures, and runtime behavior | ✅ |

---

## 🧪 Test Development

Use this section when you are building programmatic tests for Dataverse solutions.

### Start

| Document | Description | Status |
|---|---|---|
| [Testing Overview](./tests/testing.md) | Entry point for test setup and test usage | ✅ |

### Core Concepts

| Document | Description | Status |
|---|---|---|
| [Test Architecture](./tests/test-architecture.md) | Structure of the testing project and main building blocks | ✅ |
| [Test Execution Flow](./tests/test-execution-flow.md) | How test setup, execution, assertion, and cleanup work | ✅ |
| [Test Data Lifecycle](./tests/test-data-lifecycle.md) | Creating, using, and removing test data safely | ✅ |

### Data Access

| Document | Description | Status |
|---|---|---|
| [Test Data Access](./tests/data-access.md) | Working with Dataverse through the testing stack | ✅ |

> [!NOTE]
> The testing part of the repository is intentionally separate from plugin development.
> You should be able to understand and use the testing stack without mixing it with plugin implementation details.

---

## 📦 Release and Versioning

Use this section when you need release and versioning information for the framework-based solution.

| Document | Description | Status |
|---|---|---|
| [Versioning](./VERSIONING.md) | Versioning strategy and release model | ✅ |
| [Changelog](../CHANGELOG.md) | Release notes and change history | ✅ |

---

## ⚠️ Known Limitations

Use this section to understand current technical limits and compatibility constraints.

| Document | Description | Status |
|---|---|---|
| [Known Limitations](./limitations.md) | Framework-specific constraints and compatibility notes | ✅ |

---

## 🤝 Contributing

Repository-level policies and contribution guidance.

| Document | Description | Status |
|---|---|---|
| [Contributing](../CONTRIBUTING.md) | Basic contribution process and expectations | ✅ |
| [Security](../SECURITY.md) | Vulnerability reporting and security handling | ✅ |
| [Code of Conduct](../CODE_OF_CONDUCT.md) | Community behavior expectations | ✅ |
| [License](../LICENSE) | Repository license | ✅ |

---

## 🗺️ Recommended Reading Path

### I want to build plugins

1. [Getting Started](./plugins/getting-started.md)
2. [Architecture](./plugins/architecture.md)
3. [Plugin Model](./plugins/plugin-model.md)
4. [Task Model](./plugins/task-model.md)
5. [Validation Model](./plugins/validation.md)
6. [Execution Pipeline](./plugins/execution-pipeline.md)
7. [Data Access](./plugins/data-access.md)
8. [DataService](./plugins/data-service.md)

### I want to build tests

1. [Testing Overview](./tests/testing.md)
2. [Test Architecture](./tests/test-architecture.md)
3. [Test Execution Flow](./tests/test-execution-flow.md)
4. [Test Data Access](./tests/data-access.md)

### I want to contribute

1. [Contributing](../CONTRIBUTING.md)
2. [Code of Conduct](../CODE_OF_CONDUCT.md)
3. [Security](../SECURITY.md)
4. [License](../LICENSE)

---

**Questions?** Open a [Discussion](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/discussions) or check [Issues](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/issues)