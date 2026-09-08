# Template packaging

How the Pillaro Dataverse plugin template is assembled and delivered. This is
maintainer documentation; it is not shipped in any package. The consumer-facing
text lives in `templates/Pillaro.Dataverse.PluginTemplate.DotNetNew/README.md`,
which is the file packed into the NuGet package as `PackageReadmeFile`.

## Delivery formats

The generated-project content has one source of truth and two delivery formats.

| Project | Owns | Delivered as |
| --- | --- | --- |
| `Pillaro.Dataverse.PluginTemplate.Source` | shared generated-project files | nothing (`IsPackable=false`) |
| `Pillaro.Dataverse.PluginTemplate.DotNetNew` | `dotnet new` packaging metadata and CLI overlay | NuGet package `Pillaro.Dataverse.PluginTemplate.DotNetNew` |
| `Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix` | Visual Studio packaging | VSIX on the Visual Studio Marketplace |

`dotnet new` is the recommended path for new solutions. The VSIX exists for
Visual Studio installability and Marketplace presence.

The two formats do not generate identical trees. The `dotnet new` package adds
`.slnx` and `.vscode/`; the VSIX adds `.vstemplate` files and a `.gitignore`.
Everything else comes from the shared source.

## Shared source

The single source of truth for generated-project content is:

```text
templates/Pillaro.Dataverse.PluginTemplate.Source/ProjectTemplate
```

Only files that are independent of a packaging technology belong there: sample
plugin and task code, the shared test project sources, common configuration, the
in-solution `Logic/README.md`, the template logo and `LICENSE.txt`.

Do not put `.vstemplate` files, VSIX manifests, Marketplace metadata, packaging
build scripts or generated ZIP/VSIX output there.

## dotnet new package layout

Package content is assembled from two places:

1. shared generated-project source from `Pillaro.Dataverse.PluginTemplate.Source`
2. CLI-specific overlay files from `template/ProjectTemplate` in the `DotNetNew`
   project

The overlay provides:

- `.template.config/template.json` and the template icon
- `Pillaro.Dataverse.PluginTemplate.slnx`
- the `Logic`, `Plugins` and `Tests` project files
- `.vscode/extensions.json` and `.vscode/settings.json`

### Shared-source staging during pack

The `DotNetNew` project does not pack the shared source directly. The
`PrepareDotNetNewTemplateContent` target runs before `GenerateNuspec` and `Pack`
and calls `build/Prepare-DotNetNewTemplate.ps1`, which copies the shared source
into a package-specific staging folder and applies the CLI-specific namespace and
documentation tweaks there:

```text
$(BaseIntermediateOutputPath)dotnetnew-template\ProjectTemplate
```

Staging keeps the shared source unmodified while still letting the CLI package
differ from it. The `Content` items in the csproj that point at
`$(DotNetNewStageRoot)` pack from the staged copy; the ones pointing at
`template\ProjectTemplate` pack the overlay as authored.

## VSIX build flow

1. Shared source is read from `Pillaro.Dataverse.PluginTemplate.Source/ProjectTemplate`.
2. `build/PrepareVisualStudioTemplate.ps1` copies it into
   `obj/PreparedTemplate/Pillaro.Dataverse.PluginTemplate` and overlays the
   Visual Studio-specific files from
   `template/ProjectTemplates/Pillaro.Dataverse.PluginTemplate` on top —
   the root and child `.vstemplate` files and the Visual Studio `.csproj` files.
3. `build/PackProjectTemplate.ps1` packs the prepared folder into
   `ProjectTemplates/Pillaro.Dataverse.PluginTemplate.zip`. That ZIP is the
   actual project template payload.
4. `IncludePillaroProjectTemplateZipInVsix` adds the ZIP to the VSIX under
   `ProjectTemplates/`, where `source.extension.vsixmanifest` points Visual
   Studio at it.
5. `PublishPillaroVisualStudioArtifacts` copies the ZIP and the `.vsix` into the
   artifacts directory.

## Versioning

Both packaging projects carry placeholder versions in the repository; the
`Templates - Build Template Artifacts` pipeline stamps the real ones at queue
time. The VSIX version is written into `source.extension.vsixmanifest`
(`baseVersion.buildId`); the `dotnet new` package version is passed as
`/p:PackageVersion`. For a local Visual Studio rebuild, edit the manifest
`Version` by hand.

## Build and validate locally

```powershell
dotnet pack "templates/Pillaro.Dataverse.PluginTemplate.DotNetNew/Pillaro.Dataverse.PluginTemplate.DotNetNew.csproj" -c Release

powershell -NoProfile -ExecutionPolicy Bypass `
  -File scripts/Test-DotNetTemplateArtifacts.ps1 `
  -TemplatePackagePath "templates/Pillaro.Dataverse.PluginTemplate.DotNetNew/bin/Release/Pillaro.Dataverse.PluginTemplate.DotNetNew.<version>.nupkg" `
  -SkipBuildSmoke
```

The VSIX project needs Visual Studio MSBuild (the VSSDK build tools); `dotnet
build` alone is not enough.

```powershell
dotnet build "templates/Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix/Pillaro.Dataverse.PluginTemplate.VisualStudio.Vsix.csproj" `
  /p:ArtifactsDirectory="artifacts/templates"

powershell -NoProfile -ExecutionPolicy Bypass `
  -File scripts/Test-PluginTemplateArtifacts.ps1 `
  -TemplateName "Pillaro.Dataverse.PluginTemplate" `
  -VsixVersion "<version>" `
  -ArtifactsDirectory "artifacts/templates"
```

## Maintenance rules

1. Common generated-project files go into `Pillaro.Dataverse.PluginTemplate.Source`.
2. Visual Studio-only files go into the VSIX project; CLI-only files go into the
   `DotNetNew` project. Keep both overlays minimal.
3. Never duplicate the same common file across the two packaging projects.
4. Do not treat generated ZIP or VSIX output as a source of truth.
5. After changing shared source or an overlay, rebuild both packaging projects
   and run both validation scripts.
6. When you change the packed template content, bump the package version.
7. Adding a file to the shared source does not pack it automatically — the
   `DotNetNew` csproj lists its `Content` items explicitly. Add the new file
   there as well.
