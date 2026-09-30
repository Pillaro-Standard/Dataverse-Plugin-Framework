namespace Pillaro.Dataverse.PluginFramework.Cli.AiInstructions;

internal enum AiTargetKind
{
    /// <summary>Owned by the framework: overwritten on every sync, removed when no longer shipped.</summary>
    Managed,

    /// <summary>Owned by the team, except a marked block that every sync replaces.</summary>
    Block,

    /// <summary>Owned by the team: created when missing, never changed afterwards.</summary>
    Seed
}

internal sealed record AiTarget(string Path, AiTargetKind Kind);

/// <summary>
/// Where each source file of the framework repository lands in a consuming solution. Paths are
/// relative, with forward slashes: source paths to the framework repository, target paths to the
/// root of the consuming solution.
/// </summary>
internal static class AiInstructionLayout
{
    public const string ManagedRoot = ".pillaro/ai/";
    public const string ManifestPath = ".pillaro/ai/manifest.json";
    public const string ProjectSetupPath = ".pillaro/project-setup.md";

    public static IReadOnlyList<AiTarget> TargetsFor(string sourcePath)
    {
        const string rules = "docs/ai/rules/";
        const string instructions = ".github/instructions/";
        const string skills = "ai/skills/";

        if (sourcePath.EndsWith('/'))
        {
            // A directory is never shipped as a file; MapLinkTarget maps links to directories.
            return [];
        }

        if (sourcePath.StartsWith(rules, StringComparison.Ordinal))
        {
            return [new(ManagedRoot + "rules/" + sourcePath[rules.Length..], AiTargetKind.Managed)];
        }

        if (sourcePath is "docs/ai/analysis-workflow.md" or "docs/ai/verify.md")
        {
            return [new(ManagedRoot + FileName(sourcePath), AiTargetKind.Managed)];
        }

        if (sourcePath.StartsWith(instructions, StringComparison.Ordinal))
        {
            // Prefixed, so the team's own instruction files never collide with the managed ones.
            return [new(instructions + "pillaro-" + FileName(sourcePath), AiTargetKind.Managed)];
        }

        if (sourcePath.StartsWith(skills, StringComparison.Ordinal))
        {
            // Claude Code reads .claude/skills, Codex reads .agents/skills; Copilot reads both.
            var skill = sourcePath[skills.Length..];
            return
            [
                new(".claude/skills/" + skill, AiTargetKind.Managed),
                new(".agents/skills/" + skill, AiTargetKind.Managed)
            ];
        }

        if (sourcePath.StartsWith("examples/", StringComparison.Ordinal)
            && sourcePath.EndsWith(".cs", StringComparison.Ordinal))
        {
            // .txt, so no project that globs **/*.cs ever compiles the reference copies.
            return [new(ManagedRoot + "examples/" + FileName(sourcePath) + ".txt", AiTargetKind.Managed)];
        }

        return sourcePath switch
        {
            "ai/entry/AGENTS.md" => [new("AGENTS.md", AiTargetKind.Block)],
            "ai/entry/CLAUDE.md" => [new("CLAUDE.md", AiTargetKind.Block)],
            "ai/entry/copilot-instructions.md" => [new(".github/copilot-instructions.md", AiTargetKind.Block)],
            "ai/project-setup.md" => [new(ProjectSetupPath, AiTargetKind.Seed)],
            _ => []
        };
    }

    /// <summary>
    /// Where a link to <paramref name="sourcePath"/> (a file, or a directory ending in '/') points in
    /// the consuming solution, or null when the linked file is not shipped.
    /// </summary>
    public static string? MapLinkTarget(string sourcePath)
    {
        var targets = TargetsFor(sourcePath);
        if (targets.Count > 0)
        {
            return targets[0].Path;
        }

        return sourcePath switch
        {
            "AGENTS.md" => "AGENTS.md",
            ".github/copilot-instructions.md" => ".github/copilot-instructions.md",
            ".github/project-setup.md" => ProjectSetupPath,
            "docs/ai/rules/" => ManagedRoot + "rules/",
            ".github/instructions/" => ".github/instructions/",
            _ => null
        };
    }

    private static string FileName(string path)
    {
        return path[(path.LastIndexOf('/') + 1)..];
    }
}
