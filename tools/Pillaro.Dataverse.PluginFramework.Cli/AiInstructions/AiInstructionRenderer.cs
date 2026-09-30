using System.Text.RegularExpressions;

namespace Pillaro.Dataverse.PluginFramework.Cli.AiInstructions;

/// <summary>
/// Turns a source file of the framework repository into the file a consuming solution gets:
/// sections marked framework-only are dropped, relative links are pointed at the shipped copies
/// (or at the framework repository on GitHub for files that are not shipped), and managed files get
/// a header saying so.
/// </summary>
internal static class AiInstructionRenderer
{
    public const string RepositoryUrl = "https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework";

    public const string ManagedHeader =
        "<!-- Managed by pillaro-dv ai-sync (Pillaro Dataverse Plugin Framework). Do not edit: the next sync overwrites this file. " +
        "Your own rules belong in AGENTS.md, outside the managed block. -->";

    public const string CodeHeader =
        "// Reference copy from the Pillaro Dataverse Plugin Framework examples, managed by pillaro-dv ai-sync.\n" +
        "// It is not compiled. Copy the code shape, never its step or image GUIDs (PF-REG-003).\n\n";

    private static readonly Regex FrameworkOnlySection = new(
        @"<!-- pillaro:framework-only -->.*?<!-- /pillaro:framework-only -->\n?",
        RegexOptions.Singleline | RegexOptions.CultureInvariant);

    private static readonly Regex MarkdownLink = new(@"\]\((?<url>[^)\s]+)\)", RegexOptions.CultureInvariant);

    // Paths written as text rather than as links.
    private static readonly (string From, string To)[] Mentions =
    [
        ("`docs/ai/rules/", "`" + AiInstructionLayout.ManagedRoot + "rules/"),
        ("`docs/ai/analysis-workflow.md`", "`" + AiInstructionLayout.ManagedRoot + "analysis-workflow.md`"),
        ("`docs/ai/verify.md`", "`" + AiInstructionLayout.ManagedRoot + "verify.md`"),
        ("`.github/project-setup.md`", "`" + AiInstructionLayout.ProjectSetupPath + "`")
    ];

    public static string Render(string sourcePath, string content, AiTarget target, string gitRef)
    {
        if (!sourcePath.EndsWith(".md", StringComparison.Ordinal))
        {
            return CodeHeader + content;
        }

        var text = FrameworkOnlySection.Replace(content, string.Empty);
        text = MarkdownLink.Replace(text, match =>
            "](" + RewriteLink(sourcePath, target.Path, match.Groups["url"].Value, gitRef) + ")");

        foreach (var (from, to) in Mentions)
        {
            text = text.Replace(from, to, StringComparison.Ordinal);
        }

        return target.Kind == AiTargetKind.Managed ? InsertHeader(text) : text;
    }

    public static string RewriteLink(string sourcePath, string targetPath, string url, string gitRef)
    {
        if (url.StartsWith('#')
            || url.StartsWith("http:", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("https:", StringComparison.OrdinalIgnoreCase)
            || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        var hash = url.IndexOf('#', StringComparison.Ordinal);
        var path = hash >= 0 ? url[..hash] : url;
        var anchor = hash >= 0 ? url[hash..] : string.Empty;

        var resolved = Resolve(DirectoryOf(sourcePath), path);
        if (resolved is null)
        {
            return url;
        }

        var mapped = AiInstructionLayout.MapLinkTarget(resolved);
        if (mapped is null)
        {
            var kind = resolved.EndsWith('/') ? "tree" : "blob";
            return $"{RepositoryUrl}/{kind}/{gitRef}/{resolved.TrimEnd('/')}{anchor}";
        }

        return Relative(DirectoryOf(targetPath), mapped) + anchor;
    }

    /// <summary>
    /// The git ref for links into the framework repository: the release tag, or main for a
    /// pre-release and for a local build without a version (the SDK default 1.0.0 is not a tag).
    /// </summary>
    public static string GitRefFor(string version)
    {
        var isRelease = Regex.IsMatch(version, @"^\d+\.\d+\.\d+$", RegexOptions.CultureInvariant);
        return isRelease && version != "1.0.0" ? version : "main";
    }

    private static string InsertHeader(string text)
    {
        // Skills and Copilot instructions start with YAML front matter, which must stay first.
        if (text.StartsWith("---\n", StringComparison.Ordinal))
        {
            var end = text.IndexOf("\n---\n", 4, StringComparison.Ordinal);
            if (end >= 0)
            {
                var split = end + "\n---\n".Length;
                return text[..split] + "\n" + ManagedHeader + "\n" + text[split..];
            }
        }

        return ManagedHeader + "\n\n" + text;
    }

    internal static string DirectoryOf(string path)
    {
        var slash = path.LastIndexOf('/');
        return slash >= 0 ? path[..slash] : string.Empty;
    }

    /// <summary>Resolves a relative link against a directory; null when it leaves the repository.</summary>
    internal static string? Resolve(string directory, string relative)
    {
        var segments = new List<string>();
        if (directory.Length > 0)
        {
            segments.AddRange(directory.Split('/'));
        }

        foreach (var segment in relative.Split('/'))
        {
            if (segment is "" or ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (segments.Count == 0)
                {
                    return null;
                }

                segments.RemoveAt(segments.Count - 1);
                continue;
            }

            segments.Add(segment);
        }

        var resolved = string.Join('/', segments);
        return relative.EndsWith('/') && resolved.Length > 0 ? resolved + "/" : resolved;
    }

    /// <summary>The relative path from a directory to a file or directory (trailing '/').</summary>
    internal static string Relative(string fromDirectory, string to)
    {
        var from = fromDirectory.Length > 0 ? fromDirectory.Split('/') : [];
        var isDirectory = to.EndsWith('/');
        var target = to.TrimEnd('/').Split('/');

        var common = 0;
        while (common < from.Length && common < target.Length - (isDirectory ? 0 : 1)
               && string.Equals(from[common], target[common], StringComparison.Ordinal))
        {
            common++;
        }

        var parts = Enumerable.Repeat("..", from.Length - common).Concat(target.Skip(common)).ToList();
        var relative = string.Join('/', parts);

        if (relative.Length == 0)
        {
            return "./";
        }

        return isDirectory ? relative + "/" : relative;
    }
}
