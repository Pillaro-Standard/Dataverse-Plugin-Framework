# Pillaro Dataverse Visual Studio VSIX Template

This project builds the Visual Studio project template package for Pillaro Dataverse Plugin Framework.

## Purpose

`Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix` is responsible for producing an installable Visual Studio VSIX package.

The VSIX package installs a Visual Studio project template that creates a standard Pillaro Dataverse plugin solution structure.

The generated solution currently contains:

- `Logic` project
- `Plugins` project
- `Tests` project

The VSIX project does not own the common generated-project content. Common files are stored in:

```text
templates/Pillaro.Dataverse.PluginTemplate.Source/ProjectTemplate
```

This project owns only the Visual Studio-specific template packaging.

## High-level build flow

The Visual Studio template is built in several steps.

### 1. Shared source is used as the base

The shared template source is read from:

```text
templates/Pillaro.Dataverse.PluginTemplate.Source/ProjectTemplate
```

This folder contains the files that should be common for all template types.

### 2. Visual Studio overlay is applied

The Visual Studio-specific overlay is read from:

```text
templates/Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix/template/ProjectTemplates/Pillaro.Dataverse.PluginTemplate
```

This overlay contains Visual Studio-specific files, mainly:

- root `.vstemplate`
- child project `.vstemplate` files
- Visual Studio-specific `.csproj` template files

The build prepares a temporary template folder by copying the shared source first and then applying the Visual Studio overlay on top of it.

Prepared output location:

```text
templates/Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix/obj/PreparedTemplate/Pillaro.Dataverse.PluginTemplate
```

This is handled by:

```text
build/PrepareVisualStudioTemplate.ps1
```

### 3. Prepared template is packed into ZIP

The prepared template folder is packed into a Visual Studio project template ZIP.

Generated ZIP path:

```text
templates/Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix/ProjectTemplates/Pillaro.Dataverse.PluginTemplate.zip
```

This is handled by:

```text
build/PackProjectTemplate.ps1
```

The ZIP is the actual Visual Studio project template payload.

### 4. ZIP is included in the VSIX package

The generated ZIP is added into the VSIX under:

```text
ProjectTemplates/
```

The VSIX manifest points Visual Studio to that project template content.

### 5. Rebuild creates the VSIX output

When the VSIX project is rebuilt, the final `.vsix` package is produced in the project output folder.

The package can then be installed into Visual Studio 2022.

## Important folders

```text
templates/
└── Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix/
    ├── build/
    │   ├── PrepareVisualStudioTemplate.ps1
    │   ├── PackProjectTemplate.ps1
    │   └── SyncTemplateSource.ps1
    ├── template/
    │   └── ProjectTemplates/
    │       └── Pillaro.Dataverse.PluginTemplate/
    ├── ProjectTemplates/
    │   └── Pillaro.Dataverse.PluginTemplate.zip
    ├── source.extension.vsixmanifest
    └── Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix.csproj
```

## What belongs here

This project should contain files required specifically for Visual Studio template packaging.

Typical examples:

- `.vstemplate` files
- VSIX manifest
- Visual Studio-specific project template `.csproj` files
- VSIX build scripts
- template overlay files
- Visual Studio template metadata
- packaging validation logic related to VSIX output

## What does not belong here

Do not put common generated-project source files here unless they are Visual Studio-specific.

Avoid placing these files here:

- common plugin sample code
- common task sample code
- common test base classes
- shared generated-project README files
- shared assets that should be reused by other template types
- generated build output that can be recreated

Those files should live in `Pillaro.Dataverse.PluginTemplate.Source`.

## Build command

From the repository root:

```powershell
dotnet build "templates/Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix/Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix.csproj" `
  /p:VsixVersion=1.0.8 `
  /p:ArtifactsDirectory="artifacts/templates"
```

## Validation command

After build, validate the generated template artifacts:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File scripts/Test-PluginTemplateArtifacts.ps1 `
  -TemplateName "Pillaro.Dataverse.PluginTemplate" `
  -VsixVersion "1.0.8" `
  -ArtifactsDirectory "artifacts/templates"
```

The validation should check at minimum:

- project template ZIP exists
- VSIX package exists
- required `.vstemplate` files exist
- files referenced from `.vstemplate` files exist
- generated project can be smoke-built
- VSIX manifest has expected metadata and version

## Maintenance rules

When changing the Visual Studio template:

1. Put common generated-project files into `Pillaro.Dataverse.PluginTemplate.Source`.
2. Put Visual Studio-specific files into this project.
3. Keep the overlay minimal.
4. Do not manually edit generated ZIP output as the source of truth.
5. Rebuild the VSIX project after changing shared source or overlay files.
6. Run the template artifact validation script before merging.
7. Keep documentation paths aligned with the actual project structure.

## Short summary

This project owns the Visual Studio packaging process.

It should answer the question:

> How do we turn the shared Pillaro Dataverse plugin template source into an installable Visual Studio VSIX template?
