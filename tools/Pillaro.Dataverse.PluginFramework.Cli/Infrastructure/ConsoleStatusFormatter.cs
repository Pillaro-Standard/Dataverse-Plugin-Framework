namespace Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

/// <summary>
/// Formats bracketed status labels (e.g. [CREATE], [ERROR]) with ANSI colors for console output.
/// Colors can be disabled with the standard NO_COLOR environment variable (https://no-color.org).
/// </summary>
internal static class ConsoleStatusFormatter
{
    private const char Escape = (char)27;

    private static readonly string Reset = $"{Escape}[0m";
    private static readonly string Green = $"{Escape}[32m";
    private static readonly string Yellow = $"{Escape}[33m";
    private static readonly string Red = $"{Escape}[31m";
    private static readonly string Orange = $"{Escape}[38;2;255;140;0m";
    private static readonly string Cyan = $"{Escape}[36m";
    private static readonly string Gray = $"{Escape}[90m";

    private static readonly bool ColorsEnabled =
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR"));

    public static string Label(string status)
    {
        if (!ColorsEnabled)
        {
            return $"[{status}]";
        }

        var color = status switch
        {
            "CREATE" => Green,
            "OK" => Gray,
            "UPDATE" => Yellow,
            "CHANGE" => Orange,
            "WARN" => Orange,
            "DELETE" => Red,
            "ERROR" => Red,
            "TYPE-ONLY" => Cyan,
            _ => null,
        };

        return color == null ? $"[{status}]" : $"{color}[{status}]{Reset}";
    }
}
