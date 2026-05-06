# Pillaro Dataverse Plugin Framework

[![NuGet - Framework](https://img.shields.io/badge/NuGet-Pillaro.Dataverse.PluginFramework-blue?logo=nuget)](https://www.nuget.org/packages/Pillaro.Dataverse.PluginFramework)
[![NuGet - Testing](https://img.shields.io/badge/NuGet-Pillaro.Dataverse.PluginFramework.Testing-blue?logo=nuget)](https://www.nuget.org/packages/Pillaro.Dataverse.PluginFramework.Testing)

Source-open, AI-ready standard for building scalable Dynamics 365 and Power Platform plugins.


## Overview

Pillaro Framework provides a structured and production-proven approach to developing Dataverse plugins using C#.

It introduces a task-based execution model with explicit validation and execution phases, enabling:

- predictable and deterministic behavior
- strict separation of concerns
- fully testable plugin logic
- long-term maintainability at scale

The framework is designed for real-world production environments and long-term solution sustainability.


## Proven in Production

✅ **Production Ready**

The framework is production-proven, actively used in enterprise environments.

**First production adopter:**

<p align="left">
  <a href="https://www.seyfor.com/?utm_source=pillaro_github&utm_medium=partner_referral&utm_campaign=dataverse_plugin_framework&utm_content=seyfor_logo">
    <img src=".github/assets/seyfor-logo.png" alt="Seyfor" height="60"/>
  </a>
</p>

Seyfor has successfully implemented and deployed the framework in their enterprise Dynamics 365 and Power Platform solutions, validating its real-world readiness, reliability, and enterprise-grade quality.

---

## What Problem It Solves

Standard plugin development often leads to:

- large, hard-to-maintain classes
- mixed validation and execution logic
- multiple responsibilities in a single class
- lack of testability
- duplicated patterns across projects

This framework introduces a consistent structure:

- each functionality = single task
- validation is separated from execution
- behavior is deterministic and explicit
- built-in testing support
- built-in diagnostic logging

---

## Key Features

### Task-Based Architecture

Each plugin is composed of independent tasks:

- single responsibility
- explicit validation
- isolated execution
- explicitly testable units (one task = one testable component)

---

### Validation Model

- fluent validation defined per task with clear and explicit rules
- fail-fast validation that prevents execution of invalid tasks while allowing the pipeline to continue
- consistent handling of validation and execution errors with clear and traceable outcomes
- automatic logging of validation failures with detailed reasons

---

### Runtime Configuration

- configuration stored in Dataverse entity
- dynamically affects plugin behavior
- no redeployment required
- cached for performance optimization

---

### Autonumbering

- configurable sequences
- parent-based numbering
- concurrency-safe generation

---

### Diagnostic Logging

Comprehensive logging integrated throughout the execution pipeline:

- tracks full execution flow across all plugins and tasks
- records detailed diagnostic information with context data
- measures execution time and performance metrics
- tracks execution depth to detect excessive nesting
- provides a logging console for tasks to write structured runtime messages

This significantly simplifies debugging and troubleshooting:

- understand what code was executed and why
- trace step-by-step execution with input and context data
- identify performance bottlenecks
- detect excessive plugin nesting
- in most cases eliminates the need for complex plugin debugging

---

### Testing Support

The framework includes a built-in testing foundation designed for functional testing.

- tests are executed against a real Dataverse environment
- verifies full plugin behavior, not just isolated units
- ensures plugins work together correctly and do not interfere with each other

The testing infrastructure:

- automatically creates required test data
- ensures isolation between test runs
- cleans up all data after execution

This allows reliable validation of real-world behavior in a controlled environment.

---

## Architecture (Simplified)

The framework extends the standard Dataverse plugin SDK with a structured execution pipeline.

Each plugin is composed of independent tasks that follow a consistent lifecycle:

~~~
Plugin
  ↓
Task
  ├─ Validation
  └─ Execution
~~~

---

### Plugin Layer

The plugin acts as an entry point and orchestrates execution.

Responsibilities:

- receives Dataverse execution context
- initializes framework services
- executes registered tasks in a defined order
- manages execution pipeline and flow
- handles logging and diagnostics

---

### Task Layer

Tasks represent isolated units of functionality.

Each task:

- has a single responsibility
- is independently testable
- follows a consistent lifecycle (validation → execution)
- operates within a controlled execution context

---

### Validation

Validation is executed before any business logic.

- implemented using a fluent validation model
- ensures all required conditions are met
- prevents invalid state from reaching execution
- prevents execution of the current task if validation fails, while allowing the pipeline to continue processing remaining tasks

---

### Execution

Execution contains the actual business logic.

- runs only if validation passes
- operates on validated and prepared data
- produces deterministic and predictable results
- does not contain validation logic

---

### Logging and Diagnostics

The framework implements a multi-layer logging architecture:

**Pipeline-level logging:**
- automatic tracking of plugin and task execution flow
- performance metrics collection (execution time, depth)
- correlation of operations across the entire pipeline
- detection of circular dependencies and excessive nesting

**Task-level logging console:**
- tasks can write structured diagnostic messages
- messages include execution context and input data
- enables step-by-step tracing of business logic
- supports debugging without attaching to plugin process

**Benefits:**
- understand exactly what was executed and why
- identify performance bottlenecks with precise metrics
- troubleshoot issues using log analysis instead of complex debugging sessions

---

### Runtime Configuration

Configuration is loaded dynamically from Dataverse.

- affects behavior without redeployment
- shared across plugins and tasks
- enables environment-specific behavior
- cached to ensure minimal performance impact
- allows safe runtime adjustments of logic

---

## Getting Started

> 🚧 TODO  
> Detailed setup and usage guide will be added soon.

---

## Repository Structure

~~~
/src        → framework source code
/tests      → test projects
/examples   → sample implementations
/docs       → documentation
~~~

---

## Key Principles

- clear separation between validating conditions and executing business logic
- small, focused units of functionality with a single responsibility
- predictable and deterministic behavior in every execution
- explicit and transparent execution flow
- consistent structure across all plugins and tasks
- design focused on long-term maintainability and scalability

---

## AI-Ready Standard

The framework is part of the Pillaro delivery standards, designed to support the entire development lifecycle.

The architecture is intentionally structured to be understandable by both developers and AI-driven tools and agents.

Key principles:

- functionality is decomposed into clearly defined tasks
- tasks can be identified already during the analysis phase
- each task represents a predictable and isolated unit of behavior

This enables:

- direct mapping between analysis and implementation
- scalable development using AI-assisted workflows
- consistent structure across projects

In future scenarios, tasks can be implemented by specialized AI agents based on analysis outputs.

---

### AI Feedback Loop

Diagnostic logging and testing provide structured feedback for automated systems.

- logs contain detailed execution flow and context data
- programmatic tests validate real behavior in the environment
- failures include enough information to identify root causes

This allows:

- more accurate issue detection
- better automated code suggestions
- iterative improvement of solutions

The long-term goal is to support semi-autonomous or autonomous development workflows, where:

- AI systems generate and refine implementations
- developers focus on architecture, validation, and review

---

## Support & Partnership

Need help with implementation, extended support, or business collaboration?

Visit [www.pillaro.cz](https://www.pillaro.cz?utm_source=github&utm_medium=referral&utm_campaign=dataverse_plugin_framework&utm_content=repository_link) to get in touch with us.

We offer:

- implementation consulting and guidance
- extended technical support
- business partnership opportunities
- custom development and integration services


## License

See `LICENSE` file.
