# Pillaro Dataverse Plugin Framework — Documentation

> [!IMPORTANT]
> The root [README](../README.md) helps you decide whether the framework is relevant for your solution.
> This documentation section helps you understand how to use it, structure it, test it, and extend it.

> [!NOTE]
> This page is the documentation hub and working structure for the repository.
> Some documents are already available, others are planned and will be added incrementally.

---

## 📖 Table of Contents

| Section | Description |
|---|---|
| 🚀 [Plugin Development](#-plugin-development) | Build and structure Dataverse plugins with the framework |
| 🧪 [Test Development](#-test-development) | Build and run programmatic tests against Dataverse |
| 🧱 [Shared Concepts](#-shared-concepts) | Repository-wide concepts shared across plugins and tests |
| 📦 [Packaging & Deployment](#-packaging--deployment) | Build, package, version, and deploy the solution |
| ⚠️ [Known Limitations](#-known-limitations) | Technical constraints and compatibility notes |
| 🤝 [Contributing](#-contributing) | Basic repository policies and contribution entry points |
| 🗺️ [Recommended Reading Path](#️-recommended-reading-path) | Suggested reading order by goal |

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

### Modules

| Document | Description | Status |
|---|---|---|
| [Runtime Configuration](./configuration.md) | Settings and runtime behavior configuration | 🚧 |
| [Autonumbering](./autonumbering.md) | Number sequence generation and related patterns | 🚧 |
| [Logging](./logging.md) | Runtime logging and diagnostics | 🚧 |
| [Error Handling](./error-handling.md) | Exceptions, validation failures, and runtime behavior | 🚧 |

> [!TIP]
> Example implementations should be used mainly as support material while working through Getting Started and the plugin-focused documents above.

---

## 🧪 Test Development

Use this section when you are building programmatic tests for Dataverse solutions.

### Start

| Document | Description | Status |
|---|---|---|
| [Testing Overview](./testing.md) | Entry point for test setup and test usage | 🚧 |
| [Integration Testing](./integration-testing.md) | Running tests against Dataverse environments | 🚧 |

### Core Concepts

| Document | Description | Status |
|---|---|---|
| [Test Architecture](./tests/test-architecture.md) | Structure of the testing project and main building blocks | 🚧 |
| [Test Execution Flow](./tests/test-execution-flow.md) | How test setup, execution, assertion, and cleanup work | 🚧 |
| [Test Data Lifecycle](./tests/test-data-lifecycle.md) | Creating, using, and removing test data safely | 🚧 |
| [Cleanup Strategy](./tests/cleanup-strategy.md) | Cleanup responsibilities and isolation rules | 🚧 |

### Data Access

| Document | Description | Status |
|---|---|---|
| [Test Data Access](./tests/data-access.md) | Working with Dataverse through the testing stack | 🚧 |
| [Test Security Contexts](./tests/security-contexts.md) | Access patterns, context selection, and test execution behavior | 🚧 |

> [!NOTE]
> The testing part of the repository is intentionally separate from plugin development.
> You should be able to understand and use the testing stack without mixing it with plugin implementation details.

---

## 🧱 Shared Concepts

Use this section for concepts shared across the repository.

| Document | Description | Status |
|---|---|---|
| [Repository Structure](./repository-structure.md) | How the repository is organized across plugins, tests, and shared parts | 🚧 |
| [Shared Terminology](./terminology.md) | Common terms used across framework documentation | 🚧 |
| [Design Principles](./design-principles.md) | Core technical principles used across the framework | 🚧 |

> [!NOTE]
> Shared Concepts should stay focused on repository-wide topics.
> Plugin-specific and test-specific concepts should stay in their respective sections.

---

## 📦 Packaging & Deployment

Use this section when you need to package, version, or deploy the framework-based solution.

| Document | Description | Status |
|---|---|---|
| [Packaging and Deployment](./packaging-and-deployment.md) | Packaging model, deployment flow, and assembly preparation | 🚧 |
| [Versioning](./VERSIONING.md) | Versioning strategy and release model | ✅ |
| [Changelog](../CHANGELOG.md) | Release notes and change history | ✅ |

---

## ⚠️ Known Limitations

Use this section to understand current technical limits and compatibility constraints.

| Document | Description | Status |
|---|---|---|
| [Known Limitations](./limitations.md) | Framework-specific constraints and compatibility notes | ✅ |

Current important areas include:

- package and tooling compatibility constraints
- framework-specific runtime assumptions
- supported setup expectations for plugins and tests

---

## 🤝 Contributing

Repository-level policies and contribution guidance are kept at the repository root.

| Document | Description |
|---|---|
| [Contributing](./CONTRIBUTING.md) | Basic contribution process and expectations |
| [Security](./SECURITY.md) | Vulnerability reporting and security handling |
| [Code of Conduct](./CODE_OF_CONDUCT.md) | Community behavior expectations |
| [License](../LICENSE) | Repository license |

---

## 🗺️ Recommended Reading Path

### I want to build plugins

1. [Getting Started](./getting-started.md)
2. [Architecture](./architecture.md)
3. [Plugin Model](./plugin-model.md)
4. [Task Model](./task-model.md)
5. [Validation Model](./validation.md)
6. [Execution Pipeline](./execution-pipeline.md)
7. [Logging](./logging.md)

### I want to build tests

1. [Testing Overview](./testing.md)
2. [Integration Testing](./integration-testing.md)
3. [Test Architecture](./tests/test-architecture.md)
4. [Test Execution Flow](./tests/test-execution-flow.md)
5. [Test Data Access](./tests/data-access.md)

### I want to understand the repository structure

1. [Repository Structure](./repository-structure.md)
2. [Design Principles](./design-principles.md)
3. [Architecture](./architecture.md)
4. [Versioning](./VERSIONING.md)

### I want to contribute

1. [Contributing](../CONTRIBUTING.md)
2. [Security](../SECURITY.md)
3. [Code of Conduct](../CODE_OF_CONDUCT.md)
4. [License](../LICENSE)