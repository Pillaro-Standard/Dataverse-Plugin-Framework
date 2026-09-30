using System.Text.Json;

namespace Pillaro.Dataverse.PluginFramework.Cli.AiInstructions;

internal enum AiSyncAction
{
    Created,
    Updated,
    Unchanged,
    Removed
}

internal sealed record AiSyncChange(string Path, AiSyncAction Action);

internal sealed record AiSyncOptions(
    string RootDirectory,
    string Version,
    bool CheckOnly,
    string? LogicProject = null,
    string? TestsProject = null);

/// <summary>
/// Writes the AI instructions into a consuming solution: managed files are overwritten, the marked
/// block in AGENTS.md, CLAUDE.md and copilot-instructions.md is replaced and the rest of those files
/// kept, project-setup.md is created only once, and managed files a newer version no longer ships
/// are removed (tracked in <see cref="AiInstructionLayout.ManifestPath"/>).
/// </summary>
internal static class AiInstructionSync
{
    public const string BlockBegin =
        "<!-- pillaro:begin — managed by pillaro-dv ai-sync: this block is replaced on every sync; write your own content outside it -->";

    public const string BlockEnd = "<!-- pillaro:end -->";

    private const string BlockBeginPrefix = "<!-- pillaro:begin";

    private static readonly Dictionary<string, string> BlockSeedTails = new(StringComparer.Ordinal)
    {
        ["AGENTS.md"] = "\n## Rules of this solution\n\nYour team's own rules go here, outside the managed block — `ai-sync` keeps them.\n"
    };

    private static readonly JsonSerializerOptions ManifestJson = new() { WriteIndented = true };

    public static IReadOnlyList<AiSyncChange> Run(IReadOnlyDictionary<string, string> sources, AiSyncOptions options)
    {
        var gitRef = AiInstructionRenderer.GitRefFor(options.Version);
        var changes = new List<AiSyncChange>();
        var managed = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var (sourcePath, content) in sources)
        {
            foreach (var target in AiInstructionLayout.TargetsFor(sourcePath))
            {
                var rendered = AiInstructionRenderer.Render(sourcePath, content, target, gitRef);
                var fullPath = FullPath(options.RootDirectory, target.Path);

                switch (target.Kind)
                {
                    case AiTargetKind.Managed:
                        managed[target.Path] = rendered;
                        break;

                    case AiTargetKind.Block:
                        var existing = AiText.ReadOrNull(fullPath);
                        BlockSeedTails.TryGetValue(target.Path, out var seedTail);
                        changes.Add(Apply(fullPath, target.Path, existing, ApplyBlock(existing, rendered, seedTail), options.CheckOnly));
                        break;

                    case AiTargetKind.Seed:
                        if (File.Exists(fullPath))
                        {
                            changes.Add(new(target.Path, AiSyncAction.Unchanged));
                            break;
                        }

                        changes.Add(Apply(fullPath, target.Path, null, FillProjectSetup(rendered, options), options.CheckOnly));
                        break;
                }
            }
        }

        foreach (var (path, content) in managed)
        {
            var fullPath = FullPath(options.RootDirectory, path);
            changes.Add(Apply(fullPath, path, AiText.ReadOrNull(fullPath), content, options.CheckOnly));
        }

        foreach (var stale in ReadManifest(options.RootDirectory).Except(managed.Keys, StringComparer.Ordinal))
        {
            var fullPath = FullPath(options.RootDirectory, stale);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            changes.Add(new(stale, AiSyncAction.Removed));
            if (!options.CheckOnly)
            {
                File.Delete(fullPath);
            }
        }

        if (!options.CheckOnly)
        {
            WriteManifest(options.RootDirectory, managed.Keys);
        }

        return changes.OrderBy(change => change.Path, StringComparer.Ordinal).ToList();
    }

    /// <summary>The solution root: the nearest folder with .git, else with a solution file, else the start folder.</summary>
    public static string FindRoot(string startDirectory)
    {
        for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")) || File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }
        }

        for (var directory = new DirectoryInfo(startDirectory); directory is not null; directory = directory.Parent)
        {
            if (directory.EnumerateFiles("*.sln").Any() || directory.EnumerateFiles("*.slnx").Any())
            {
                return directory.FullName;
            }
        }

        return Path.GetFullPath(startDirectory);
    }

    internal static string ApplyBlock(string? existing, string blockContent, string? seedTail)
    {
        var block = BlockBegin + "\n" + blockContent.Trim('\n') + "\n" + BlockEnd + "\n";
        if (existing is null)
        {
            return block + (seedTail ?? string.Empty);
        }

        var begin = existing.IndexOf(BlockBeginPrefix, StringComparison.Ordinal);
        var end = begin >= 0 ? existing.IndexOf(BlockEnd, begin, StringComparison.Ordinal) : -1;
        if (begin < 0 || end < 0)
        {
            // An existing file without the block: the block goes first, the team's content stays below.
            return block + "\n" + existing;
        }

        var after = end + BlockEnd.Length;
        if (after < existing.Length && existing[after] == '\n')
        {
            after++;
        }

        return existing[..begin] + block + existing[after..];
    }

    private static AiSyncChange Apply(string fullPath, string path, string? existing, string content, bool checkOnly)
    {
        if (existing is not null && string.Equals(existing, content, StringComparison.Ordinal))
        {
            return new(path, AiSyncAction.Unchanged);
        }

        if (!checkOnly)
        {
            AiText.Write(fullPath, content);
        }

        return new(path, existing is null ? AiSyncAction.Created : AiSyncAction.Updated);
    }

    private static string FillProjectSetup(string content, AiSyncOptions options)
    {
        var (logic, tests) = AiProjectDiscovery.Find(options.RootDirectory);

        return content
            .Replace("{{LogicProject}}", options.LogicProject ?? logic ?? "<path to the Logic project>", StringComparison.Ordinal)
            .Replace("{{TestsProject}}", options.TestsProject ?? tests ?? "<path to the test project>", StringComparison.Ordinal);
    }

    private static string FullPath(string root, string relativePath)
    {
        return Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    private static IReadOnlyList<string> ReadManifest(string root)
    {
        var text = AiText.ReadOrNull(FullPath(root, AiInstructionLayout.ManifestPath));
        if (text is null)
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<AiManifest>(text)?.Files ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static void WriteManifest(string root, IEnumerable<string> files)
    {
        var manifest = new AiManifest("pillaro-dv ai-sync", files.ToList());
        AiText.Write(FullPath(root, AiInstructionLayout.ManifestPath), JsonSerializer.Serialize(manifest, ManifestJson) + "\n");
    }

    // No version: the files are the same in every solution synced from one package version, so the
    // manifest stays unchanged in a template snapshot and in a solution.
    private sealed record AiManifest(string Generator, List<string> Files);
}
