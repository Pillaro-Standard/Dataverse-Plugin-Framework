# ILMerge Tooling

This folder contains ILMerge tooling and prepared post-build action templates copied by the `Pillaro.Dataverse.PluginFramework` NuGet package into consuming plugin projects.

The purpose of this tooling is to help produce a single deployable Dataverse plugin assembly.

## Why ILMerge Is Used

Dataverse plugin deployment requires a single plugin assembly.

When plugin logic is split across multiple projects or uses additional runtime dependencies, those assemblies must be merged into one final plugin DLL before deployment.

Typical structure:

- `Plugin.dll` — Dataverse plugin entry point
- `Logic.dll` — business logic shared with tests
- framework/runtime dependencies
- final merged plugin assembly

The final output should be one deployable DLL registered in Dataverse.

---

## Files

This folder contains:

- `ILMerge.exe` — merge tool used during build
- `PostBuildAction-logic_plugin-projects.txt` — post-build template for solutions with separate Plugin and Logic projects
- `PostBuildAction-single-project.txt` — post-build template for solutions where plugin entry points and business logic are in one project
- `README.md` — this documentation

---

## Post-Build Action Templates

Choose the template that matches your project structure.

### `PostBuildAction-logic_plugin-projects.txt`

Use this template when:

- the Dataverse plugin entry point is in a Plugin project
- the business logic is in a separate Logic project
- the final plugin assembly must include the Logic assembly

Before using the template, replace:

```text
{LOGIC_ASSEMBLY}
```

with the actual Logic project output DLL name.

Example:

```text
MySolution.Logic.dll
```

Important rules:

- the Logic assembly must exist in `$(TargetDir)`
- the Logic assembly must be the last item in the merge list
- update the merge list when adding or removing dependencies

### `PostBuildAction-single-project.txt`

Use this template when:

- plugin entry points and business logic are in one plugin project
- there is no separate Logic assembly to merge
- only the plugin assembly and required dependencies need to be merged

Important rules:

- all assemblies in the merge list must exist in `$(TargetDir)`
- update the merge list when adding or removing dependencies
- this variant does not use `{LOGIC_ASSEMBLY}`

---

## Cleanup Behavior

After the merge completes, the post-build script removes temporary or duplicated files from the output directory.

The exact cleanup depends on the selected template.

For the Logic + Plugin project template:

- the temporary renamed assembly is removed
- the separate Logic assembly is removed after it has been merged

For the single-project template:

- the temporary renamed assembly is removed

This keeps the build output focused on the final merged plugin DLL.

---

## Notes

- Adjust paths if your project structure differs.
- Keep the dependency list explicit and under control.
- Include only assemblies that must be part of the final Dataverse plugin DLL.
- Make sure the final merged assembly is strong-name signed if required by your deployment process.
- Rebuild the plugin project after changing the post-build action.

---

## Design Decision

ILMerge is used because of Dataverse plugin runtime constraints:

- plugin deployment expects a single assembly
- plugin runtime does not load arbitrary local dependency DLLs
- plugin assemblies target `.NET Framework 4.6.2`
- deterministic deployment is easier when the final artifact is one DLL

This approach keeps deployment predictable and reduces missing-dependency issues in Dataverse environments.