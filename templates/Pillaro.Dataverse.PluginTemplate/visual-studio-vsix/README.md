# Visual Studio VSIX Package

This folder packages the Visual Studio project template into an installable VSIXv3 by using the official `Microsoft.VSSDK.BuildTools` package.

## Build

From the repository root:

```bash
dotnet build .\templates\Pillaro.Dataverse.PluginTemplate\visual-studio-vsix\Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix.csproj
```

The project creates:

```text
artifacts\templates\Pillaro.Dataverse.PluginTemplate.zip
artifacts\templates\Pillaro.Dataverse.PluginTemplate.VisualStudio.vsix
```

To override the VSIX version:

```bash
dotnet build .\templates\Pillaro.Dataverse.PluginTemplate\visual-studio-vsix\Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix.csproj /p:VsixVersion=1.0.8
```

Install the VSIX locally by opening it in Windows or from Visual Studio through **Extensions > Manage Extensions**.

## Marketplace

The VSIX contains a `Microsoft.VisualStudio.ProjectTemplate` asset. During build, VSSDK BuildTools expands the generated template ZIP into the VSIX and creates the Visual Studio template manifest.

Before publishing, update:

- publisher identity
- extension id
- version
- repository and more-info URL
- icon and license assets
- marketplace overview content
