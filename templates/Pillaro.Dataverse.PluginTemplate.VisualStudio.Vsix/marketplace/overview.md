# Pillaro Dataverse Plugin Template

A Visual Studio project template that creates a Microsoft Dataverse plugin solution with
three projects:

- **Logic** — plugin registrations and task classes, where the business logic lives.
- **Plugins** — the signed assembly that Dataverse loads, built against the framework.
- **Tests** — an xUnit project wired to a Dataverse environment through a connection string.

The generated solution builds on the [Pillaro Dataverse Plugin
Framework](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework), which provides
plugin registration from code, runtime settings, autonumbering and plugin logging.

## Getting started

1. In Visual Studio, choose **File → New → Project** and search for *Pillaro Dataverse
   Plugin*.
2. Build the solution once. The first build scaffolds the deployment tooling into `Tools/`.
3. Import the Pillaro framework solution into your Dataverse environment. The runtime
   features do not work without it.
4. Set the connection string environment variable named in `PillaroSettings.json` and
   deploy with `pillaro-dv`.

Full documentation, including the deployment steps, is in the
[repository](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework).

## Requirements

- Visual Studio 2022 or 2026
- .NET Framework 4.6.2 developer pack, for the plugin assembly
- A Dataverse environment, to deploy into

## License

Apache-2.0. Commercial use is free of charge. See
[LICENSE](https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/main/LICENSE).
