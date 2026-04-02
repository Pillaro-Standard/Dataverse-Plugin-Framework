# Versioning Strategy

> Versioning model for Pillaro Dataverse Plugin Framework packages and releases.

---

## Overview

The framework uses **Semantic Versioning (SemVer 2.0)** to ensure predictable and transparent versioning across all packages.

All published packages follow the format:

```
MAJOR.MINOR.PATCH[-prerelease]
```

Examples:

* `1.0.0`
* `1.0.1`
* `1.1.0`
* `1.1.0-preview.1`
* `1.1.0-rc.1`

---

## Table of Contents

- [Version Structure](#version-structure)
- [Branch-Based Versioning](#branch-based-versioning)
- [Package Version Alignment](#package-version-alignment)
- [Version Source](#version-source)
- [Pre-release Labels](#pre-release-labels)
- [Release Flow](#release-flow)
- [Notes](#notes)

---

## Version Structure

### MAJOR

Incremented when breaking changes are introduced.

Typical cases:

* Public API changes that require code updates
* Behavioral changes that break existing implementations

---

### MINOR

Incremented when new features are added in a backward-compatible way.

Typical cases:

* New functionality
* New extension points
* Additional helpers or APIs

---

### PATCH

Incremented for backward-compatible fixes.

Typical cases:

* Bug fixes
* Internal improvements
* Non-breaking refactoring

---

## Branch-Based Versioning

Versioning is aligned with the branching strategy.

### `main`

* Contains **stable releases only**
* No prerelease suffix

Examples:

```
1.0.0
1.0.1
1.1.0
```

---

### `dev`

* Used for active development
* Produces **preview versions**

Format:

```
{version}-preview.{build}
```

Examples:

```
1.1.0-preview.1
1.1.0-preview.25
```

---

### Release Candidate (optional)

Before merging to `main`, release candidates can be produced:

Format:

```
{version}-rc.{build}
```

Examples:

```
1.1.0-rc.1
1.1.0-rc.2
```

---

### `feature/*`

* Used for development of specific features
* Not intended for public releases

Artifacts may be produced for internal testing only:

Format:

```
{version}-ci.{build}
```

Examples:

```
1.1.0-ci.15
1.1.0-ci.42
```

---

## Package Version Alignment

All framework-related packages share the **same version**.

Examples:

* `Pillaro.Dataverse.PluginFramework` → `1.0.0`
* `Pillaro.Dataverse.PluginFramework.Testing` → `1.0.0`

This ensures:

* consistency across the ecosystem
* easier dependency management
* clearer communication to users

---

## Version Source

The base version (e.g. `1.1.0`) is defined in the repository and used by the build pipeline.

The pipeline is responsible for:

* appending prerelease suffixes (`preview`, `rc`, `ci`)
* generating build numbers

---

## Pre-release Labels

| Label     | Purpose                                    |
| --------- | ------------------------------------------ |
| `preview` | Active development, not final              |
| `rc`      | Release candidate, near final              |
| `ci`      | Internal build, not for public consumption |

---

## Release Flow

Typical release flow:

1. Development happens in `dev`
2. Preview versions are published (`preview`)
3. Optional release candidates (`rc`)
4. Merge to `main`
5. Stable version is released (no suffix)

---

## Notes

* Stable releases are always published from `main`
* Prerelease versions are intended for testing and early adoption
* Feature builds are not part of the public release cycle
