# Limitations

> [!IMPORTANT]
> This document lists the current framework-specific limitations.

---

## 📑 Navigation

- [🧪 Merged assembly reference limitation](#-merged-assembly-reference-limitation)
- [🧰 SPKL early-bound generation limitation](#-spkl-early-bound-generation-limitation)
- [➡️ Related documents](#️-related-documents)

---

## 🧪 Merged assembly reference limitation

The final `Plugins` assembly is a merged deployment output.

It must not be referenced from test projects, because it introduces duplicate references and type conflicts.

Because of that:

- implementation must stay in `Logic`
- `Plugins` must remain deployment-only
- tests must reference `Logic`, not the merged plugin assembly

> [!IMPORTANT]
> The `Logic` and `Plugins` split is required not only for structure, but also to avoid reference conflicts caused by the merged deployment assembly.

---

## 🧰 SPKL early-bound generation limitation

If SPKL is used for early-bound generation, `Microsoft.CrmSdk.CoreTools` must not be upgraded beyond:

    9.1.0.92

This limitation applies only to SPKL early-bound generation usage.

---

## ➡️ Related documents

- [Getting Started](./plugins/getting-started.md)
- [Architecture](./plugins/architecture.md)