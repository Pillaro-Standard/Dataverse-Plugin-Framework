# Pillaro Dataverse Plugin Template Source

This project contains the shared source files for the Pillaro Dataverse plugin project template.

## Purpose

`Pillaro.Dataverse.PluginTemplate.Source` is the common template source project.

It contains files that are independent of a specific template delivery format. These files are expected to be reused by multiple template types, for example:

- Visual Studio VSIX project template
- CLI-oriented `dotnet new` template
- future internal or CI-generated template packages

The goal is to keep the actual plugin solution skeleton in one shared place and avoid duplicating common files across multiple template packaging projects.

## What belongs here

Only template files that are not specific to one packaging technology should be placed here.

Typical examples:

- sample plugin logic
- sample task implementation
- shared test project files
- common configuration files
- common README files used inside the generated project
- common assets such as template logo or license file
- files that should appear in every generated Pillaro Dataverse plugin solution

Current shared template root:

```text
templates/
└── Pillaro.Dataverse.PluginTemplate.Source/
    └── ProjectTemplate/
        ├── Logic/
        ├── Plugins/
        ├── Tests/
        ├── PillaroLogo128.png
        └── LICENSE.txt
```

The project file includes the shared template files from:

```text
ProjectTemplate/**/*.*
```

## What does not belong here

Do not put template-package-specific files into this project.

Avoid placing these files here:

- Visual Studio `.vstemplate` files
- VSIX manifests
- VSIX build scripts
- Visual Studio-specific project overlays
- Marketplace metadata
- generated ZIP files
- generated VSIX files
- build output
- files that are needed only by one template packaging format

Those files belong to the packaging-specific project, for example `Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix`.

## Relationship to the VSIX project

The Visual Studio VSIX project reads this shared source and combines it with Visual Studio-specific overlay files.

The shared source is copied into a prepared temporary template folder. Then Visual Studio-specific files from the VSIX project are overlaid on top of it.

This means:

1. common files live here,
2. Visual Studio-specific files live in the VSIX project,
3. CLI-specific packaging files live in the `dotnet new` project,
4. the packaging projects are responsible for producing the final installable template packages.

## Maintenance rules

When changing the generated project structure:

1. Put common generated-project files into this project.
2. Put Visual Studio-only template files into the VSIX project.
3. Do not duplicate the same common file in both projects.
4. Do not commit generated ZIP/VSIX outputs unless there is a clearly documented reason.
5. After changing shared files, rebuild the VSIX project and run the template artifact validation script.

## Recommended validation

After changing files in this project, validate the generated Visual Studio template through the VSIX project:

```powershell
dotnet build "templates/Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix/Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix.csproj" `
  /p:ArtifactsDirectory="artifacts/templates"

powershell -NoProfile -ExecutionPolicy Bypass `
  -File scripts/Test-PluginTemplateArtifacts.ps1 `
  -TemplateName "Pillaro.Dataverse.PluginTemplate" `
  -VsixVersion "1.0.8" `
  -ArtifactsDirectory "artifacts/templates"
```

The VSIX project stores its version directly in `source.extension.vsixmanifest`. For local Visual Studio rebuilds, edit the `Version` value in that manifest and rebuild the project. The pipeline overwrites that manifest version before building.

## Short summary

This project owns the shared template content.

It should answer the question:

> What should every generated Pillaro Dataverse plugin solution contain, regardless of how the template is packaged or installed?
