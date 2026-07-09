# Limitations

> [!IMPORTANT]
> This document lists the current framework-specific limitations.

---

## 📑 Navigation

- [🧪 Merged assembly reference limitation](#-merged-assembly-reference-limitation)
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

## ➡️ Related documents

- [Getting Started](./plugins/getting-started.md)
- [Architecture](./plugins/architecture.md)