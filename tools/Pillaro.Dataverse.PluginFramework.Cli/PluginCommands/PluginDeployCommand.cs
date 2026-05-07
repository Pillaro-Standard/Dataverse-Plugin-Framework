using Pillaro.Dataverse.PluginFramework.Cli.Configuration;
using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;
using Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.RegistrationState;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal static class PluginDeployCommand
{
    private const string InternalManifestPath = "artifacts/plugin-manifest.json";

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var options = CommandLineOptions.Parse(args);
            var settings = await PillaroSettingsLoader.LoadAsync(options);
            var configuredAssemblyPath = options.Get("assembly") ?? settings.Plugins.Assembly;
            var assemblyPath = ResolveAssemblyPath(options, configuredAssemblyPath);
            var solutionName = ResolveSolutionName(options, settings);
            var connectionOptions = await DataverseConnectionOptions.ResolveAsync(options, settings);
            var pacPushOptions = PacPluginPushOptions.From(options);
            var allowConfirmationRequired = options.HasFlag("confirm");
            var includeUnchanged = options.HasFlag("include-unchanged");

            CliLog.WriteHeader("plugin deploy", options, settings, connectionOptions);
            Console.WriteLine($"Resolved solution: {solutionName ?? "<empty>"}");
            Console.WriteLine($"Configured plugin assembly: {configuredAssemblyPath ?? "<empty>"}");
            Console.WriteLine($"Resolved plugin assembly: {assemblyPath ?? "<empty>"}");
            Console.WriteLine();

            if (string.IsNullOrWhiteSpace(solutionName))
            {
                Console.Error.WriteLine("Missing solution. Set top-level 'solution' in PillaroSettings.json or pass --solution.");
                Console.Error.WriteLine("Note: earlyBound.solution is used only as fallback for now. Preferred config is top-level 'solution'.");
                return 3;
            }

            if (string.IsNullOrWhiteSpace(settings.Solution) && !string.IsNullOrWhiteSpace(settings.EarlyBound.Solution))
            {
                Console.WriteLine("Warning: using earlyBound.solution as plugin deployment solution fallback. Move it to top-level 'solution'.");
                Console.WriteLine();
            }

            if (string.IsNullOrWhiteSpace(assemblyPath) || !File.Exists(assemblyPath))
            {
                Console.Error.WriteLine($"Assembly was not found: {assemblyPath}");
                Console.Error.WriteLine("Relative paths from PillaroSettings.json are resolved from the settings file directory.");
                Console.Error.WriteLine("Set plugins.assembly in PillaroSettings.json or pass --assembly.");
                return 2;
            }

            var sdkConnectionErrors = connectionOptions.ValidateSdk();
            if (sdkConnectionErrors.Count > 0)
            {
                Console.Error.WriteLine("Dataverse SDK connection options are invalid:");
                foreach (var error in sdkConnectionErrors)
                {
                    Console.Error.WriteLine($"- {error}");
                }

                return 3;
            }

            var pacConnectionErrors = connectionOptions.ValidatePac(required: !pacPushOptions.SkipPacPush);
            if (pacConnectionErrors.Count > 0)
            {
                Console.Error.WriteLine("PAC CLI options are invalid:");
                foreach (var error in pacConnectionErrors)
                {
                    Console.Error.WriteLine($"- {error}");
                }

                return 3;
            }

            var pacPushErrors = pacPushOptions.ValidateForPacPush();
            if (pacPushErrors.Count > 0)
            {
                Console.Error.WriteLine("PAC plugin push options are invalid:");
                foreach (var error in pacPushErrors)
                {
                    Console.Error.WriteLine($"- {error}");
                }

                return 3;
            }

            var pacAuthResult = await PacCliAuthService.EnsureSelectedAsync(connectionOptions, force: !pacPushOptions.SkipPacPush);
            if (pacAuthResult != 0)
            {
                return pacAuthResult;
            }

            var manifest = PluginManifestFactory.CreateFromAssembly(assemblyPath);
            var manifestErrors = PluginManifestValidator.Validate(manifest);
            if (manifestErrors.Count > 0)
            {
                Console.Error.WriteLine("Plugin manifest validation failed:");
                foreach (var error in manifestErrors)
                {
                    Console.Error.WriteLine($"- {error}");
                }

                return 4;
            }

            var manifestPath = PillaroSettingsLoader.ResolvePathFromSettings(options, InternalManifestPath);
            Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
            await PluginManifestSerializer.SaveAsync(manifest, manifestPath);

            var confirmationRequiredSteps = manifest.Plugins
                .SelectMany(plugin => plugin.Steps)
                .Where(step => step.DeploymentPolicy?.RequiresConfirmation == true)
                .ToList();

            if (confirmationRequiredSteps.Count > 0 && !allowConfirmationRequired)
            {
                Console.Error.WriteLine("Deployment contains steps requiring confirmation. Re-run with --confirm after reviewing the diff.");
                foreach (var step in confirmationRequiredSteps)
                {
                    Console.Error.WriteLine($"- {step.StepId} {step.MessageName} {step.EntityName ?? "<none>"} {step.StageName} {step.ModeName}: {step.DeploymentPolicy?.Risk} - {step.DeploymentPolicy?.Reason}");
                }

                return 6;
            }

            Console.WriteLine("Plugin manifest is valid.");
            Console.WriteLine("SDK target environment: <from profile/connection string>");
            Console.WriteLine($"Assembly: {Path.GetFullPath(assemblyPath)}");
            Console.WriteLine($"Solution: {solutionName}");
            Console.WriteLine($"Manifest: {manifestPath}");
            Console.WriteLine($"Plugins: {manifest.Plugins.Count}");
            Console.WriteLine($"Steps: {manifest.Plugins.Sum(plugin => plugin.Steps.Count)}");
            Console.WriteLine($"Images: {manifest.Plugins.SelectMany(plugin => plugin.Steps).Sum(step => step.Images.Count)}");
            Console.WriteLine();

            var pacPushResult = await PacPluginPushService.PushAsync(connectionOptions, pacPushOptions, assemblyPath);
            if (pacPushResult != 0)
            {
                return pacPushResult;
            }

            var service = DataverseSdkConnectionFactory.Create(connectionOptions);
            var currentState = await DataverseRegistrationStateReader.ReadAsync(service, manifest);
            var diff = PluginRegistrationDiffCalculator.Calculate(manifest, currentState);
            PluginRegistrationDiffWriter.Write(diff, includeUnchanged);

            if (!diff.HasChanges)
            {
                Console.WriteLine();
                Console.WriteLine("No step/image metadata changes to apply.");
                return 0;
            }

            Console.WriteLine();
            Console.WriteLine("Applying step/image metadata changes using Pillaro registration layer...");
            await DataverseRegistrationUpserter.ApplyAsync(service, manifest, diff, solutionName);
            Console.WriteLine("Step/image metadata deploy completed.");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static string? ResolveAssemblyPath(CommandLineOptions options, string? configuredAssemblyPath)
    {
        return string.IsNullOrWhiteSpace(configuredAssemblyPath)
            ? null
            : PillaroSettingsLoader.ResolvePathFromSettings(options, configuredAssemblyPath);
    }

    private static string? ResolveSolutionName(CommandLineOptions options, PillaroSettings settings)
    {
        return options.Get("solution")
            ?? NullIfWhiteSpace(settings.Solution)
            ?? NullIfWhiteSpace(settings.EarlyBound.Solution);
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
