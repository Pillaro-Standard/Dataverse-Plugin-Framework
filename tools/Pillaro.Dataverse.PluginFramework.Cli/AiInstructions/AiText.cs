using System.Text;

namespace Pillaro.Dataverse.PluginFramework.Cli.AiInstructions;

internal static class AiText
{
    public static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Line endings as LF, so a checkout with CRLF does not count as a change.</summary>
    public static string Normalize(string text)
    {
        return text.Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    public static string? ReadOrNull(string path)
    {
        return File.Exists(path) ? Normalize(File.ReadAllText(path)) : null;
    }

    public static void Write(string path, string content)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, content, Utf8NoBom);
    }
}
