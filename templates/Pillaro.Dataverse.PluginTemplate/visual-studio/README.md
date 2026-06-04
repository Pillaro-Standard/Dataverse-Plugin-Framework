# Visual Studio Template

This folder contains the Visual Studio multi-project template for `Pillaro Dataverse Plugin Template`.

The template creates three projects:

- `YourProject.Logic`
- `YourProject.Plugins`
- `YourProject.Tests`

## Build the template package

From the repository root:

```powershell
.\templates\Pillaro.Dataverse.PluginTemplate\visual-studio\build-template.ps1
```

The script creates:

```text
artifacts\templates\Pillaro.Dataverse.PluginTemplate.zip
```

## Install into Visual Studio

Copy the generated `.zip` file to the Visual Studio project templates folder:

```text
%USERPROFILE%\Documents\Visual Studio 2022\Templates\ProjectTemplates
```

Restart Visual Studio and create a project named `Pillaro Dataverse Plugin Template`.

The Visual Studio template intentionally uses fixed defaults:

- Pillaro framework package version: `1.0.2`
- test target framework: `net8.0`

Adjust package versions or the test target framework after project creation if the target environment needs different values.
