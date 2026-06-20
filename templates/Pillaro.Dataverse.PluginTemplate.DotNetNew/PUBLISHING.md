# Publishing Pillaro Dataverse Plugin Template DotNetNew

This project packages the shared Dataverse plugin template into a NuGet-based `dotnet new` template.

## Build

```powershell
dotnet pack "templates/Pillaro.Dataverse.PluginTemplate.DotNetNew/Pillaro.Dataverse.PluginTemplate.DotNetNew.csproj" -c Release
```

## Validate

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File scripts/Test-DotNetTemplateArtifacts.ps1 `
  -TemplatePackagePath "templates/Pillaro.Dataverse.PluginTemplate.DotNetNew/bin/Release/Pillaro.Dataverse.PluginTemplate.DotNetNew.1.0.12.nupkg" `
  -SkipBuildSmoke
```

## Publish checklist

1. Confirm the generated template still creates `Logic`, `Plugins`, and `Tests`.
2. Confirm the generated `Tests` project includes the prepared `Data` folders.
3. Bump the package version when the packaged template content changes.
4. Publish the resulting `.nupkg` to the NuGet feed used by the team.
