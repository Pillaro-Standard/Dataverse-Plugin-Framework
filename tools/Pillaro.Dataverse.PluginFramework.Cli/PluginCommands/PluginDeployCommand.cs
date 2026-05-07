using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;
using Pillaro.Dataverse.PluginFramework.Cli.PluginCommands.RegistrationState;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal static class PluginDeployCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            var options = CommandLineOptions.Parse(args);
            var manifestPath = options.Require("manifest");
            var assemblyPath = options.Require("assembly");
            var connectionOptions = DataverseConnectionOptions.From(options);
            var pacPushOptions = PacPluginPushOptions.From(options);
            var allowConfirmationRequired = options.HasFlag("confirm");
            var includeUnchanged = options.HasFlag("include-unchanged");
            var allowSdkMetadataUpsert = options.HasFlag("allow-sdk-metadata-upsert");

            if (!File.Exists(manifestPath))
            {
                Console.Error.WriteLine($"Manifest was not found: {manifestPath}");
                return 2;
            }

            if (!File.Exists(assemblyPath))
            {
                Console.Error.WriteLine($"Assembly was not found: {assemblyPath}");
                return 2;
            }

            var connectionErrors = connectionOptions.Validate();
            if (connectionErrors.Count > 0)
            {
                Console.Error.WriteLine("Dataverse connection options are invalid:");
                foreach (var error in connectionErrors)
                {
                    Console.Error.WriteLine($"- {error}");
                }

                return 3;
            }

            if (!connectionOptions.UsesPacCli && !allowSdkMetadataUpsert)
            {
                Console.Error.WriteLine("Direct SDK metadata upsert is disabled by default. Use --auth-type PacCli for PAC-first deployment, or pass --allow-sdk-metadata-upsert explicitly.");
                return 3;
            }

            if (!connectionOptions.UsesPacCli && !pacPushOptions.SkipPacPush)
            {
                Console.Error.WriteLine("PAC plugin push requires --auth-type PacCli. Use --skip-pac-push to skip PAC upload when using SDK metadata mode.");
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

            var pacAuthResult = await PacCliAuthService.EnsureSelectedAsync(connectionOptions);
            if (pacAuthResult != 0)
            {
                return pacAuthResult;
            }

            var manifest = await PluginManifestSerializer.LoadAsync(manifestPath);
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
            Console.WriteLine($"Target environment: {GetTargetLabel(connectionOptions)}");
            Console.WriteLine($"Assembly: {Path.GetFullPath(assemblyPath)}");
            Console.WriteLine($"Solution: {connectionOptions.SolutionName ?? "<not specified>"}");
            Console.WriteLine($"Plugins: {manifest.Plugins.Count}");
            Console.WriteLine($"Steps: {manifest.Plugins.Sum(plugin => plugin.Steps.Count)}");
            Console.WriteLine($"Images: {manifest.Plugins.SelectMany(plugin => plugin.Steps).Sum(step => step.Images.Count)}");
            Console.WriteLine();

            var pacPushResult = await PacPluginPushService.PushAsync(connectionOptions, pacPushOptions, assemblyPath);
            if (pacPushResult != 0)
            {
                return pacPushResult;
            }

            if (connectionOptions.UsesPacCli && !allowSdkMetadataUpsert)
            {
                Console.WriteLine();
                Console.WriteLine("PAC-first deployment completed for assembly/package upload.");
                Console.WriteLine("Step/image metadata from the manifest was validated but not applied because PAC CLI does not currently expose a dedicated plugin step/image registration command in this CLI wrapper.");
                Console.WriteLine("Use --allow-sdk-metadata-upsert with SDK credentials only if the team explicitly accepts the SDK fallback for metadata operations.");
                return manifest.Plugins.SelectMany(plugin => plugin.Steps).Any() ? 20 : 0;
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
            Console.WriteLine("Applying step/image metadata changes using explicit SDK fallback...");
            await DataverseRegistrationUpserter.ApplyAsync(service, manifest, diff);
            Console.WriteLine("Step/image metadata deploy completed.");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static string GetTargetLabel(DataverseConnectionOptions options)
    {
        if (options.UsesPacCli)
        {
            return string.IsNullOrWhiteSpace(options.PacAuthProfile)
                ? "PAC CLI active profile"
                : $"PAC CLI profile '{options.PacAuthProfile}'";
        }

        return options.EnvironmentUrl ?? "<connection-string>";
    }
}
