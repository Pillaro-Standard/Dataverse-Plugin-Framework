# Pillaro Dataverse Plugin Template DotNetNew

This project builds the `dotnet new` template package for the Pillaro Dataverse plugin solution.

## Purpose

This package is the CLI-friendly delivery format for the shared template source stored in:

```text
templates/Pillaro.Dataverse.PluginTemplate.Source/ProjectTemplate
```

It reuses the same shared files as the Visual Studio VSIX template, but adds the `dotnet new` packaging metadata and the CLI-oriented project overlays.

## What this package does

The package produces a NuGet template package that can be installed locally with `dotnet new install`.

After installation, create a new solution with:

```powershell
dotnet new pillaro-dataverse-plugin -n MySolution
```

The generated solution is intended for Visual Studio Code and other CLI-based workflows.

## Package layout

The package content is assembled from two places:

1. Shared generated-project source from `Pillaro.Dataverse.PluginTemplate.Source`
2. Dotnet-specific overlay files from `template/ProjectTemplate`

The overlay provides:

- `.template.config/template.json`
- `Pillaro.Dataverse.PluginTemplate.slnx`
- `Logic`, `Plugins`, and `Tests` project files
- optional VS Code workspace hints

## Relationship to other template formats

This project owns only the `dotnet new` delivery format.

The shared generated-project files remain in `Pillaro.Dataverse.PluginTemplate.Source`, and the Visual Studio VSIX template stays in `Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix`.
