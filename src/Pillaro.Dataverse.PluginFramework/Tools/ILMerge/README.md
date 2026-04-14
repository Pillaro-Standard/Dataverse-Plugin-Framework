# ILMerge Tooling

This folder contains build tooling used to merge Dataverse plugin assemblies into a single deployable DLL.

## Why ILMerge is used

Dataverse plugin deployment requires a single assembly output for the plugin runtime.

Because plugin logic is typically split into multiple projects (for testability and reuse), the final plugin assembly must be merged during post-build.

Typical structure:

- `Plugin.dll` – plugin entry point (shell)
- `Framework.dll` – shared framework
- `Logic.dll` – business logic (shared with tests)

The final output is a single merged plugin assembly.

---

## Files

- `ILMerge.exe` – merge tool used during build
- `PostBuildAction.txt` – template for post-build merge command
- `README.md` – this documentation

---

## PostBuildAction.txt

This file contains the template used to merge plugin assemblies into a single deployable DLL.

### Placeholder

- `{LOGIC_ASSEMBLY}` must be replaced with the actual logic project output  
- Example:  
  `Pillaro.Plugins.Examples.Logic.dll`

### Important rules

- The logic assembly **must be the last item** in the merge list  
- All assemblies must exist in the build output directory (`bin`)  
- Update the merge list when adding or removing dependencies  

### Cleanup behavior

After the merge completes:

- the temporary renamed assembly (`ForMerge*.dll`) is removed  
- the logic assembly (`{LOGIC_ASSEMBLY}`) is also removed from the output directory  

This ensures:
- only the final merged plugin DLL remains  
- no duplicate or unnecessary assemblies are left in the build output  

---

## Notes

- Adjust paths if your project structure differs  
- Keep dependency list explicit and under control  
- Only include assemblies that must be part of the final plugin DLL  

---

## Design decision

ILMerge is used intentionally due to Dataverse plugin runtime constraints:

- single assembly deployment requirement  
- no runtime dependency loading  
- compatibility with .NET Framework 4.6.2  

This approach ensures predictable deployment and consistent behavior across environments.