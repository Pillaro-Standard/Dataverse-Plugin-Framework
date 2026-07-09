# Example Tooling

This folder is part of a repository project.

This project does not consume these tool files from the NuGet package. The files are maintained manually in the repository so the examples and framework source projects can run against the local source tree.

That means paths in the scripts can be intentionally different from package-generated consumer projects.

For the NuGet package-managed tooling README template, see:

```text
../../../../src/Pillaro.Dataverse.PluginFramework/Tools/EarlyBoundSupport/README.md
```

Do not store Dataverse connection strings or secrets in committed files.
