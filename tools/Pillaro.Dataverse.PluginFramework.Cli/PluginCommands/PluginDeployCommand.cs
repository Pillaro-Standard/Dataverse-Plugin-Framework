using System.Reflection;
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
            var settingsPath = PillaroSettingsLoader.GetResolvedSettingsPath(options);
            if (settingsPath == null)
            {
                Console.Error.WriteLine("Error: PillaroSettings.json not found.");
                Console.Error.WriteLine("Action: Create PillaroSettings.json in the current project folder, or pass --settings with an existing file path.");
                return 2;
            }

            var settings = await PillaroSettingsLoader.LoadAsync(options);
            var settingsDirectory = Path.GetDirectoryName(Path.GetFullPath(settingsPath))!;
            var activeProfileName = PillaroSettingsLoader.ResolveActiveProfileName(options, settings);
            var configuredAssemblyPath = PillaroSettingsLoader.ResolveAssembly(options, settings);
            var assemblyPath = ResolveAssemblyPath(options, configuredAssemblyPath, settingsDirectory);
            var solutionName = ResolveSolutionName(options, settings);
            var connectionStringEnvironmentVariableName = PillaroSettingsLoader.ResolveConnectionStringEnvironmentVariable(settings);
            var connectionString = Environment.GetEnvironmentVariable(connectionStringEnvironmentVariableName);
            var justAssembly = options.HasFlag("just-assembly");

            var preflightErrors = ValidatePreflight(activeProfileName, solutionName, configuredAssemblyPath, assemblyPath, connectionStringEnvironmentVariableName, connectionString);
            if (preflightErrors.Count > 0)
            {
                Console.Error.WriteLine("Prerequisites failed:");
                foreach (var error in preflightErrors)
                {
                    Console.Error.WriteLine($"  {error}");
                }
                return 3;
            }

            var manifest = PluginManifestFactory.CreateFromAssembly(assemblyPath!);
            var manifestErrors = PluginManifestValidator.Validate(manifest);
            if (manifestErrors.Count > 0)
            {
                Console.Error.WriteLine("Plugin manifest validation failed:");
                foreach (var error in manifestErrors)
                {
                    Console.Error.WriteLine($"  {error}");
                }
                return 4;
            }

            var manifestPath = PillaroSettingsLoader.ResolveConfiguredPath(InternalManifestPath, settingsDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
            await PluginManifestSerializer.SaveAsync(manifest, manifestPath);

            const string Purple = "\u001b[38;2;99;102;241m";
            const string Reset = "\u001b[0m";

            Console.WriteLine(Purple + @"
             ____  _   ─────                 
            |  _ \(_)  ─────   __ _ _ __ ___  
            | |_) | |   | |   / _` | '__/ _ \ 
            |  __/| |   | |  | (_| | | | (_) |
            |_|   |_|  _|_|_  \__,_|_|  \___/ 
            " + Reset);

            Console.WriteLine($"Dataverse Plugin Registration v{GetCliVersion()}");
            Console.WriteLine();
            Console.WriteLine($"[OK] Profile   : {activeProfileName}");
            Console.WriteLine($"[OK] Solution  : {solutionName}");
            Console.WriteLine($"[OK] Connection: {connectionStringEnvironmentVariableName}");
            Console.WriteLine($"[OK] Assembly  : {assemblyPath}");
            Console.WriteLine($"[OK] Manifest  : {Path.GetFileName(manifestPath)}");
            Console.WriteLine();
            Console.WriteLine("Summary");
            Console.WriteLine($"  Plugins  : {manifest.Plugins.Count}");
            Console.WriteLine($"  Steps    : {manifest.Plugins.Sum(plugin => plugin.Steps.Count)}");
            Console.WriteLine($"  Images   : {manifest.Plugins.SelectMany(plugin => plugin.Steps).Sum(step => step.Images.Count)}");

            var connectionOptions = DataverseConnectionOptions.FromSdkConnectionString(connectionString!);
            var sdkConnectionErrors = connectionOptions.ValidateSdk();
            if (sdkConnectionErrors.Count > 0)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Dataverse SDK connection options are invalid:");
                foreach (var error in sdkConnectionErrors)
                {
                    Console.Error.WriteLine($"  {error}");
                }
                return 3;
            }

            var service = DataverseSdkConnectionFactory.Create(connectionOptions);
            var assemblyDeployment = await DataversePluginAssemblyDeployer.DeployAsync(service, assemblyPath!, manifest, solutionName!);

            if (justAssembly)
            {
                Console.WriteLine();
                Console.WriteLine("[OK] Assembly deployed (--just-assembly mode)");
                return 0;
            }

            var currentState = await DataverseRegistrationStateReader.ReadAsync(service, manifest, assemblyDeployment.PluginTypeIdsByName);
            var diff = PluginRegistrationDiffCalculator.Calculate(manifest, currentState);

            PluginRegistrationDiffWriter.Write(diff, manifest);

            if (diff.HasChanges)
            {
                Console.WriteLine();
                await DataverseRegistrationUpserter.ApplyAsync(service, manifest, diff, solutionName!, assemblyDeployment.PluginTypeIdsByName);
            }

            await DataverseRegistrationUpserter.EnsureDesiredSolutionMembershipAsync(service, manifest, solutionName!);

            Console.WriteLine();
            Console.WriteLine("[OK] Registration completed");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine($"[ERROR] {ex.Message}");
            return 1;
        }
    }

    private static IReadOnlyCollection<string> ValidatePreflight(
        string activeProfileName,
        string? solutionName,
        string? configuredAssemblyPath,
        string? resolvedAssemblyPath,
        string connectionStringEnvironmentVariableName,
        string? connectionString)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(solutionName))
        {
            errors.Add("Set top-level 'solution' in PillaroSettings.json.");
        }

        if (string.IsNullOrWhiteSpace(configuredAssemblyPath))
        {
            errors.Add($"Set profiles.{activeProfileName}.pluginAssemblyPath in PillaroSettings.json.");
        }
        else if (string.IsNullOrWhiteSpace(resolvedAssemblyPath) || !File.Exists(resolvedAssemblyPath))
        {
            errors.Add($"Assembly not found: {resolvedAssemblyPath}");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            errors.Add($"Set environment variable {connectionStringEnvironmentVariableName}. In Azure DevOps map the secret variable to env:{connectionStringEnvironmentVariableName}.");
        }

        return errors;
    }

    private static string? ResolveAssemblyPath(CommandLineOptions options, string? configuredAssemblyPath, string settingsDirectory)
    {
        if (string.IsNullOrWhiteSpace(configuredAssemblyPath))
        {
            return null;
        }

        // --assembly on the command line is resolved against CWD, not the settings file directory.
        if (options.Get("assembly") is { } cliAssembly && !string.IsNullOrWhiteSpace(cliAssembly))
        {
            return PillaroSettingsLoader.ResolveCommandLinePath(configuredAssemblyPath);
        }

        // Values from PillaroSettings.json profiles are resolved relative to the settings file directory.
        return PillaroSettingsLoader.ResolveConfiguredPath(configuredAssemblyPath, settingsDirectory);
    }

    private static string? ResolveSolutionName(CommandLineOptions options, PillaroSettings settings)
    {
        return options.Get("solution")
            ?? NullIfWhiteSpace(settings.Solution);
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string GetCliVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version == null ? "dev" : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
