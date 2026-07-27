using System.Diagnostics;
using System.Security;
using System.Text;

namespace Pillaro.Dataverse.PluginFramework.Tests.Tests.Deployment;

public class DeploymentScaffoldingTests
{
    [Fact]
    public async Task ScaffoldDeployment_WhenPathsContainDiacritics_GeneratesExecutableBatchWrapper()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var cancellationToken = TestContext.Current.CancellationToken;
        var testRoot = Path.Combine(Path.GetTempPath(), $"Pillaro-Deployment-{Guid.NewGuid():N}");

        try
        {
            var packageRoot = Path.Combine(
                testRoot,
                "JánMucha",
                ".nuget",
                "packages",
                "pillaro.dataverse.pluginframework",
                "1.0.0");
            CopyScaffoldingFiles(packageRoot);

            var cliDll = Path.Combine(packageRoot, "tools", "Deployment", "pillaro-dv", "pillaro-dv.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(cliDll)!);
            await File.WriteAllBytesAsync(cliDll, [], cancellationToken);

            var projectRoot = Path.Combine(testRoot, "Řešení", "Plugin Project");
            Directory.CreateDirectory(projectRoot);

            var targetPath = Path.Combine(packageRoot, "build", "Pillaro.Dataverse.PluginFramework.targets");
            var projectPath = Path.Combine(projectRoot, "Consumer.proj");
            var escapedTargetPath = SecurityElement.Escape(targetPath);
            var projectContent = $$"""
                <?xml version="1.0" encoding="utf-8"?>
                <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
                  <PropertyGroup>
                    <AssemblyName>Consumer.Plugins</AssemblyName>
                    <RootNamespace>Consumer.Plugins</RootNamespace>
                  </PropertyGroup>
                  <Import Project="{{escapedTargetPath}}" />
                </Project>
                """;
            await File.WriteAllTextAsync(projectPath, projectContent, new UTF8Encoding(false), cancellationToken);

            var generation = await RunProcessAsync(
                "dotnet",
                ["msbuild", projectPath, "/nologo", "/t:PillaroScaffoldDeployment", "/p:BuildingProject=true"],
                cancellationToken);

            Assert.True(generation.ExitCode == 0, generation.Output);

            var deploymentDirectory = Path.Combine(projectRoot, "Tools", "Deployment");
            var batchPath = Path.Combine(deploymentDirectory, "DeployPlugins.bat");
            var powerShellPath = Path.Combine(deploymentDirectory, "DeployPlugins.ps1");
            var settingsPath = Path.Combine(projectRoot, "PillaroSettings.json");

            Assert.True(File.Exists(batchPath), $"Generated batch wrapper was not found: {batchPath}");
            Assert.True(File.Exists(powerShellPath), $"Generated PowerShell wrapper was not found: {powerShellPath}");
            Assert.True(File.Exists(settingsPath), $"Generated settings file was not found: {settingsPath}");

            var batchBytes = await File.ReadAllBytesAsync(batchPath, cancellationToken);
            Assert.All(batchBytes, value => Assert.InRange(value, (byte)0, (byte)127));

            var powerShellBytes = await File.ReadAllBytesAsync(powerShellPath, cancellationToken);
            Assert.True(powerShellBytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
            Assert.Contains("JánMucha", Encoding.UTF8.GetString(powerShellBytes));

            var fakeToolsDirectory = Path.Combine(testRoot, "fake-tools");
            Directory.CreateDirectory(fakeToolsDirectory);

            var fakeDotnetBatch = Path.Combine(fakeToolsDirectory, "dotnet.cmd");
            await File.WriteAllTextAsync(
                fakeDotnetBatch,
                "@echo off\r\npowershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File \"%~dp0fake-dotnet.ps1\" %*\r\nexit /b %ERRORLEVEL%\r\n",
                Encoding.ASCII,
                cancellationToken);

            var fakeDotnetPowerShell = Path.Combine(fakeToolsDirectory, "fake-dotnet.ps1");
            await File.WriteAllTextAsync(
                fakeDotnetPowerShell,
                "Set-Content -LiteralPath $env:PILLARO_TEST_ARGUMENTS -Value $args -Encoding UTF8\r\n" +
                "Set-Content -LiteralPath $env:PILLARO_TEST_WORKING_DIRECTORY -Value (Get-Location).Path -Encoding UTF8\r\n" +
                "exit ([int]$env:PILLARO_TEST_EXIT_CODE)\r\n",
                Encoding.ASCII,
                cancellationToken);

            var argumentsCapturePath = Path.Combine(testRoot, "arguments.txt");
            var workingDirectoryCapturePath = Path.Combine(testRoot, "working-directory.txt");
            var environment = new Dictionary<string, string?>
            {
                ["PATH"] = fakeToolsDirectory + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH"),
                ["PILLARO_TEST_ARGUMENTS"] = argumentsCapturePath,
                ["PILLARO_TEST_WORKING_DIRECTORY"] = workingDirectoryCapturePath,
                ["PILLARO_TEST_EXIT_CODE"] = "37"
            };

            var execution = await RunProcessAsync(
                Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
                ["/d", "/c", batchPath, "rélease"],
                cancellationToken,
                environment);

            Assert.True(execution.ExitCode == 37, execution.Output);

            var arguments = await File.ReadAllLinesAsync(argumentsCapturePath, Encoding.UTF8, cancellationToken);
            Assert.Collection(
                arguments,
                value => Assert.Equal(Path.GetFullPath(cliDll), Path.GetFullPath(value)),
                value => Assert.Equal("deploy", value),
                value => Assert.Equal("--settings", value),
                value => Assert.Equal(Path.GetFullPath(settingsPath), Path.GetFullPath(value)),
                value => Assert.Equal("--profile", value),
                value => Assert.Equal("rélease", value));

            var workingDirectory = (await File.ReadAllTextAsync(
                workingDirectoryCapturePath,
                Encoding.UTF8,
                cancellationToken)).Trim();
            Assert.Equal(Path.GetFullPath(projectRoot), Path.GetFullPath(workingDirectory));

            var defaultProfileExecution = await RunProcessAsync(
                Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe",
                ["/d", "/c", batchPath],
                cancellationToken,
                environment);

            Assert.True(defaultProfileExecution.ExitCode == 37, defaultProfileExecution.Output);

            var defaultProfileArguments = await File.ReadAllLinesAsync(
                argumentsCapturePath,
                Encoding.UTF8,
                cancellationToken);
            Assert.Equal("debug", defaultProfileArguments[^1]);
        }
        finally
        {
            if (Directory.Exists(testRoot))
            {
                Directory.Delete(testRoot, true);
            }
        }
    }

    private static void CopyScaffoldingFiles(string packageRoot)
    {
        var sourceRoot = Path.Combine(AppContext.BaseDirectory, "DeploymentScaffolding");

        foreach (var sourcePath in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceRoot, sourcePath);
            var destinationPath = Path.Combine(packageRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            File.Copy(sourcePath, destinationPath);
        }
    }

    private static async Task<ProcessResult> RunProcessAsync(
        string fileName,
        IReadOnlyCollection<string> arguments,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, string?>? environment = null)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environment is not null)
        {
            foreach (var (name, value) in environment)
            {
                startInfo.Environment[name] = value;
            }
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Unable to start process '{fileName}'.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(1));
        var standardOutput = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var standardError = process.StandardError.ReadToEndAsync(timeout.Token);
        await process.WaitForExitAsync(timeout.Token);

        var output = (await standardOutput) + Environment.NewLine + (await standardError);
        return new ProcessResult(process.ExitCode, output.Trim());
    }

    private sealed record ProcessResult(int ExitCode, string Output);
}
