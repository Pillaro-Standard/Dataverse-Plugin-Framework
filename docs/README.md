# Pillaro Dataverse Plugin Framework — Documentation

[![Docs: In Progress](https://img.shields.io/badge/Docs-In%20Progress-orange.svg)](#-table-of-contents)
[![License: PCL v1.0](https://img.shields.io/badge/License-PCL%20v1.0-blue.svg)](../LICENSE)

> Structured documentation for understanding, using, and extending the Pillaro Dataverse Plugin Framework.

> [!NOTE]
> Documentation is in progress. Some sections may be incomplete or subject to change.

---

## 📖 Table of Contents

| Section | Description |
|---|---|
| [Getting Started](#-getting-started) | Setup, prerequisites, and first plugin |
| [Core Concepts](#-core-concepts) | Architecture, plugins, tasks, and validation |
| [Execution & Runtime](#-execution--runtime) | Pipeline and configuration |
| [Autonumbering](#-autonumbering) | Sequence generation and concurrency handling |
| [Observability](#-observability) | Logging and error handling |
| [Testing](#-testing) | Testing principles and integration testing |
| [Packaging & Deployment](#-packaging--deployment) | Signing, assembly structure, and deployment |
| [Examples](#-examples) | Sample implementations |
| [Contributing](#-contributing) | Guidelines for contributors |

---

## 🚀 Getting Started

Start here if you are new to the framework.

| Document | Description |
|---|---|
| [Getting Started](./getting-started.md) | Setup, prerequisites, and first plugin implementation |

---

## 🧱 Core Concepts

Understanding how the framework is designed — from architecture to validation.

| Document | Description |
|---|---|
| [Architecture](./architecture.md) | High-level execution model and structural overview |
| [Plugin Model](./plugin-model.md) | Role and responsibilities of plugins |
| [Task Model](./task-model.md) | Task lifecycle, structure, and single-responsibility pattern |
| [Validation Model](./validation.md) | Fluent validation API and execution flow |

---

## ⚙️ Execution & Runtime

How the framework behaves during plugin execution in Dataverse.

| Document | Description |
|---|---|
| [Execution Pipeline](./execution-pipeline.md) | Task orchestration, ordering, and execution flow |
| [Runtime Configuration](./configuration.md) | Configuration stored in Dataverse, loaded at runtime |

---

## 🔢 Autonumbering

Configurable sequence generation built into the framework.

| Document | Description |
|---|---|
| [Autonumbering](./autonumbering.md) | Sequence configuration, parent-based numbering, and concurrency handling |

---

## 🔍 Observability

Monitoring, debugging, and diagnosing execution behavior.

| Document | Description |
|---|---|
| [Logging](./logging.md) | Diagnostic logging and execution tracing |
| [Error Handling](./error-handling.md) | Validation failures, warnings, and exception handling |

---

## 🧪 Testing

How to verify behavior against real Dataverse environments.

| Document | Description |
|---|---|
| [Testing Overview](./testing.md) | Testing principles and approach |
| [Integration Testing](./integration-testing.md) | Working with Dataverse environments |

---

## 📦 Packaging & Deployment

How to prepare, sign, and deploy plugin assemblies.

| Document | Description |
|---|---|
| [Packaging and Deployment](./packaging-and-deployment.md) | Signing, assembly structure, and deployment process |
| [Versioning](./VERSIONING.md) | Versioning strategy and release model | 
| [Changelog](../CHANGELOG.md) | Release notes and changes per version |

---

## 💡 Examples

Practical usage of the framework with sample implementations.

| Document | Description |
|---|---|
| [Examples Overview](./examples.md) | Available sample implementations and patterns |

---

## 🤝 Contributing

| Document | Description |
|---|---|
| [Contributing](./contributing.md) | Guidelines for contributing to the project |

---

## 🗺️ Recommended Reading Path

If you are new to the framework:

1. Start with **Getting Started**
2. Continue with **Architecture** and **Task Model**
3. Explore **Examples**
4. Dive into advanced topics as needed

If you are implementing a solution:

1. Review **Packaging & Deployment**
2. Set up **Testing**
3. Use **Logging** for diagnostics
