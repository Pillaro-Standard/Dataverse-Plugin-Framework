# ILMerge Tooling

This folder contains the ILMerge binaries and prepared post-build script templates that the `Pillaro.Dataverse.PluginFramework` NuGet package copies into consuming plugin projects.

The scripts run from the project-local `Tools\ILMerge` folder and derive paths explicitly from:

- `$(MSBuildProjectDirectory)`
- `$(Configuration)`
- `$(AssemblyName)`

They no longer depend on:

- `$(TargetDir)`
- `$(TargetFileName)`
- `$(ProjectDir)`

## Files

- `ILMerge.exe` - merge tool used during build
- `PostBuildAction-logic_plugin-projects.txt` - post-build script for separate Plugin and Logic projects
- `PostBuildAction-single-project.txt` - post-build script for a single plugin project
- `README.md` - this documentation

## Copy behavior

The package target copies the ILMerge payload from:

```text
$(MSBuildThisFileDirectory)..\tools\ILMerge\
```

into the consuming project path:

```text
$(MSBuildProjectDirectory)\Tools\ILMerge\
```

The copy runs `BeforeTargets="PostBuildEvent"` so the merge tool is available before the post-build merge executes.

The target is imported only into projects that directly reference the `Pillaro.Dataverse.PluginFramework` NuGet package.

## Logic + Plugins projects

Use `PostBuildAction-logic_plugin-projects.txt` when plugin code and business logic are split across two projects.

Replace `{LOGIC_ASSEMBLY}` with the actual logic project output DLL name in reusable text files and documentation.

The generated Visual Studio template may use:

```bat
set "LOGIC_DLL=$ext_safeprojectname$.Logic.dll"
```

for the Logic project assembly name.

## Single-project plugins

Use `PostBuildAction-single-project.txt` when plugin code and business logic live in one project.

This variant does not use `{LOGIC_ASSEMBLY}`.

## Runtime path

The post-build scripts always execute ILMerge from the project-local tool folder:

```text
%PROJECT_DIR%\Tools\ILMerge\ILMerge.exe
```

That path is the one the consuming project should rely on at build time.

## Notes

- Keep the dependency list explicit and under control.
- Include only assemblies that must be part of the final Dataverse plugin DLL.
- Keep the Logic assembly at the end of the merge list in the split-project variant.
- Rebuild the plugin project after changing the post-build action.
