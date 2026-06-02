# Pillaro.Dataverse.PluginFramework.Plugins

This project contains Dataverse plugins that are part of the Pillaro Dataverse Plugin Framework model-driven application solution.

It is not a template project for customer plugins and it is not the main onboarding project for the NuGet package.

## Purpose

The project contains framework-owned plugin logic required by the model-driven application solution.

At the moment, the only supported functionality in this project is:

- autonumbering support
- `GetAutoNumber` plugin/task used by the framework autonumbering feature

No customer-specific business plugins should be added to this project.

## Current Functionality

### Autonumbering

The project provides the plugin logic required for the framework autonumbering functionality.

The autonumbering feature is used by the model-driven application solution to generate configured numbers for Dataverse records.

Current implementation:

- plugin area: `Autonumbering`
- task: `GetAutoNumber`
- purpose: generate autonumbering values based on framework configuration stored in Dataverse

## Relationship to the Framework Solution

This project belongs to the deployable framework solution.

It is part of the internal Pillaro Dataverse Plugin Framework application/runtime package and should be maintained together with the framework solution components.

It should stay focused only on plugin behavior required by the framework solution itself.

## Deployment

The compiled plugin assembly is deployed as part of the framework solution deployment process.

Plugin registration metadata should stay aligned with the actual framework solution components that depend on this plugin behavior.

## Versioning

The plugin project uses the framework plugin versioning model.

Plugin execution logs use the version returned from `PluginBase.GetVersion()`.

Keep the version aligned with the framework release when publishing or deploying a new version of the framework solution.
