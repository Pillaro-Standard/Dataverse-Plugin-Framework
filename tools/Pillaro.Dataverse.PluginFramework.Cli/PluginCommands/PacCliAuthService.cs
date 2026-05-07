using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

internal static class PacCliAuthService
{
    public static Task<int> EnsureSelectedAsync(DataverseConnectionOptions options)
    {
        return EnsureSelectedAsync(options, force: false);
    }

    public static async Task<int> EnsureSelectedAsync(DataverseConnectionOptions options, bool force)
    {
        if (!force && !options.UsesPacCli)
        {
            return 0;
        }

        if (!string.IsNullOrWhiteSpace(options.PacAuthProfile))
        {
            var selectResult = await ProcessRunner.RunAsync(options.PacCliPath ?? "pac", $"auth select --name \"{options.PacAuthProfile}\"");
            if (!selectResult.Succeeded)
            {
                Console.Error.WriteLine($"PAC auth profile '{options.PacAuthProfile}' could not be selected.");
                WritePacOutput(selectResult);
                return 30;
            }
        }

        var whoResult = await ProcessRunner.RunAsync(options.PacCliPath ?? "pac", "auth who");
        if (!whoResult.Succeeded)
        {
            Console.Error.WriteLine("PAC CLI authentication is not ready. Run 'pac auth create' locally or select an existing profile.");
            WritePacOutput(whoResult);
            return 31;
        }

        Console.WriteLine("PAC CLI authentication profile is active.");
        if (!string.IsNullOrWhiteSpace(whoResult.Output))
        {
            Console.WriteLine(whoResult.Output.Trim());
        }

        return 0;
    }

    private static void WritePacOutput(ProcessResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.Output))
        {
            Console.Error.WriteLine(result.Output.Trim());
        }

        if (!string.IsNullOrWhiteSpace(result.Error))
        {
            Console.Error.WriteLine(result.Error.Trim());
        }
    }
}
