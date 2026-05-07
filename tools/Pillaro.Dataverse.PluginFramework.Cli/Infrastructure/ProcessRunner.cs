using System.Diagnostics;

namespace Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

internal static class ProcessRunner
{
    public static async Task<ProcessResult> RunAsync(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            return new ProcessResult(-1, string.Empty, ex.Message);
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        return new ProcessResult(process.ExitCode, output, error);
    }
}

internal sealed record ProcessResult(int ExitCode, string Output, string Error)
{
    public bool Succeeded => ExitCode == 0;
}
