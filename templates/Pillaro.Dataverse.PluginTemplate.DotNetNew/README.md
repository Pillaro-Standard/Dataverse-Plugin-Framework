# Pillaro Dataverse Plugin Template DotNetNew

This project builds the CLI-oriented [`dotnet new`](https://www.nuget.org/packages/Pillaro.Dataverse.PluginTemplate.DotNetNew) template package for the Pillaro Dataverse plugin solution.

This is the recommended starting point for new solutions.

## Install

This template is published on NuGet.org:

- [Pillaro.Dataverse.PluginTemplate.DotNetNew](https://www.nuget.org/packages/Pillaro.Dataverse.PluginTemplate.DotNetNew)

Install it with:

```powershell
dotnet new install Pillaro.Dataverse.PluginTemplate.DotNetNew
```

The generated solution works with both Visual Studio Code and Visual Studio, so the same template fits CLI-first and IDE-first workflows.

## Purpose

This package is the CLI-friendly delivery format for the shared template source stored in:

```text
templates/Pillaro.Dataverse.PluginTemplate.Source/ProjectTemplate
```

It reuses the same shared files as the [Visual Studio VSIX template](https://marketplace.visualstudio.com/items?itemName=Pillaro.PillaroDataversePluginVisualStudioTemplate), but adds the `dotnet new` packaging metadata, a package icon, and the CLI-oriented project overlays.

## What this package does

The package produces a NuGet template package that can be installed locally with `dotnet new install`.

After installation, create a new solution with:

```powershell
dotnet new pillaro-dataverse-plugin-dotnet -n MySolution
```

The generated solution is intended for Visual Studio Code and other CLI-based workflows.
It is the default template we recommend for starting new projects quickly.

## Package layout

The package content is assembled from two places:

1. Shared generated-project source from `Pillaro.Dataverse.PluginTemplate.Source`
2. Dotnet-specific overlay files from `template/ProjectTemplate`

The shared source remains the single source of truth for all common files. During pack, the `dotnet new` project stages a package-specific copy of the shared template source and applies the CLI-specific namespace and documentation tweaks there.

The overlay provides:

- `.template.config/template.json`
- package and template icon metadata
- `Pillaro.Dataverse.PluginTemplate.slnx`
- `Logic`, `Plugins`, and `Tests` project files
- optional VS Code workspace hints

## Relationship to other template formats

This project owns only the `dotnet new` delivery format.

The shared generated-project files remain in `Pillaro.Dataverse.PluginTemplate.Source`, and the [Visual Studio VSIX template](https://marketplace.visualstudio.com/items?itemName=Pillaro.PillaroDataversePluginVisualStudioTemplate) stays in `Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix`.

The [VSIX template](https://marketplace.visualstudio.com/items?itemName=Pillaro.PillaroDataversePluginVisualStudioTemplate) exists primarily for Visual Studio installability and Marketplace presence. If you are starting a new solution, use [`dotnet new`](https://www.nuget.org/packages/Pillaro.Dataverse.PluginTemplate.DotNetNew) unless you specifically need the VSIX distribution path.
