using System.Text.RegularExpressions;
using Pillaro.Dataverse.PluginFramework.Cli.AiInstructions;
using Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

namespace Pillaro.Dataverse.PluginFramework.Tests.Tests.PluginCommands;

public class AiSyncCommandTests
{
    [Theory]
    // A rule linking to a sibling rule and to the verify page, both shipped
    [InlineData("docs/ai/rules/20-task.md", ".pillaro/ai/rules/20-task.md", "./30-validation.md", "30-validation.md")]
    [InlineData("docs/ai/rules/20-task.md", ".pillaro/ai/rules/20-task.md", "../verify.md#fast-loop", "../verify.md#fast-loop")]
    // A rule linking to framework docs that are not shipped: the release on GitHub
    [InlineData("docs/ai/rules/20-task.md", ".pillaro/ai/rules/20-task.md", "../../plugins/task-model.md",
        "https://github.com/Pillaro-Standard/Dataverse-Plugin-Framework/blob/1.3.0/docs/plugins/task-model.md")]
    // A skill linking to a Copilot instruction file, which is shipped with a prefix
    [InlineData("ai/skills/pillaro-tests/SKILL.md", ".claude/skills/pillaro-tests/SKILL.md", "../../../.github/instructions/tests.instructions.md",
        "../../../.github/instructions/pillaro-tests.instructions.md")]
    // The framework's project-setup.md stands for the solution's own
    [InlineData(".github/instructions/tasks.instructions.md", ".github/instructions/pillaro-tasks.instructions.md", "../project-setup.md",
        "../../.pillaro/project-setup.md")]
    // Entry files land in the solution root
    [InlineData("ai/entry/copilot-instructions.md", ".github/copilot-instructions.md", "./AGENTS.md", "../AGENTS.md")]
    [InlineData("ai/entry/AGENTS.md", "AGENTS.md", "../../docs/ai/rules/", ".pillaro/ai/rules/")]
    [InlineData("docs/ai/rules/20-task.md", ".pillaro/ai/rules/20-task.md", "https://example.com/x", "https://example.com/x")]
    [InlineData("docs/ai/rules/20-task.md", ".pillaro/ai/rules/20-task.md", "#naming", "#naming")]
    public void RewriteLink_PointsAtTheShippedCopyOrAtTheRelease(string sourcePath, string targetPath, string url, string expected)
    {
        var rewritten = AiInstructionRenderer.RewriteLink(sourcePath, targetPath, url, "1.3.0");

        Assert.Equal(expected, rewritten);
    }

    [Theory]
    [InlineData("1.3.0", "1.3.0")]
    [InlineData("1.3.0-rc.1", "main")]
    [InlineData("1.0.0", "main")]
    public void GitRefFor_UsesTheReleaseTagOnlyForARelease(string version, string expected)
    {
        Assert.Equal(expected, AiInstructionRenderer.GitRefFor(version));
    }

    [Fact]
    public async Task AiSync_IntoAnEmptySolution_WritesInstructionsWhoseLinksAllResolve()
    {
        using var root = new TempRoot();

        var exitCode = await PluginCommandRouter.RunAsync(["ai-sync", "--root", root.Path]);

        Assert.Equal(0, exitCode);
        foreach (var expected in new[]
                 {
                     "AGENTS.md", "CLAUDE.md", ".github/copilot-instructions.md", ".pillaro/project-setup.md",
                     ".pillaro/ai/rules/20-task.md", ".pillaro/ai/verify.md", ".github/instructions/pillaro-tasks.instructions.md",
                     ".claude/skills/pillaro-plan/SKILL.md", ".agents/skills/pillaro-plan/SKILL.md",
                     ".pillaro/ai/examples/ValidateNames.cs.txt", AiInstructionLayout.ManifestPath
                 })
        {
            Assert.True(File.Exists(root.File(expected)), $"Missing {expected}");
        }

        var broken = BrokenRelativeLinks(root.Path);
        Assert.True(broken.Count == 0, "Broken links: " + string.Join(", ", broken));
    }

    [Fact]
    public async Task AiSync_ManagedFiles_HaveTheHeaderAndNoFrameworkOnlySections()
    {
        using var root = new TempRoot();

        await PluginCommandRouter.RunAsync(["ai-sync", "--root", root.Path]);

        var verify = File.ReadAllText(root.File(".pillaro/ai/verify.md"));
        Assert.StartsWith(AiInstructionRenderer.ManagedHeader, verify, StringComparison.Ordinal);
        Assert.DoesNotContain("Dataverse Plugin Framework.sln", verify, StringComparison.Ordinal);
        Assert.DoesNotContain("pillaro:framework-only", verify, StringComparison.Ordinal);

        // The front matter stays first, the header follows it.
        var skill = File.ReadAllText(root.File(".claude/skills/pillaro-plan/SKILL.md"));
        Assert.StartsWith("---\nname: pillaro-plan\n", skill, StringComparison.Ordinal);
        Assert.Contains(AiInstructionRenderer.ManagedHeader, skill, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AiSync_Again_KeepsTheTeamsContentOutsideTheBlockAndRestoresTheBlock()
    {
        using var root = new TempRoot();
        await PluginCommandRouter.RunAsync(["ai-sync", "--root", root.Path]);

        var agents = root.File("AGENTS.md");
        var original = File.ReadAllText(agents);
        var edited = original
            .Replace("Hard boundaries — read first", "Edited inside the block", StringComparison.Ordinal)
            + "\n- Our own rule: invoices are never deleted.\n";
        File.WriteAllText(agents, edited);

        var exitCode = await PluginCommandRouter.RunAsync(["ai-sync", "--root", root.Path]);

        var synced = File.ReadAllText(agents);
        Assert.Equal(0, exitCode);
        Assert.Contains("Hard boundaries — read first", synced, StringComparison.Ordinal);
        Assert.DoesNotContain("Edited inside the block", synced, StringComparison.Ordinal);
        Assert.Contains("Our own rule: invoices are never deleted.", synced, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AiSync_ExistingFileWithoutTheBlock_PutsTheBlockFirstAndKeepsTheFile()
    {
        using var root = new TempRoot();
        File.WriteAllText(root.File("CLAUDE.md"), "Always answer in Czech.\n");

        await PluginCommandRouter.RunAsync(["ai-sync", "--root", root.Path]);

        var claude = File.ReadAllText(root.File("CLAUDE.md"));
        Assert.StartsWith(AiInstructionSync.BlockBegin, claude, StringComparison.Ordinal);
        Assert.Contains("@AGENTS.md", claude, StringComparison.Ordinal);
        Assert.EndsWith("Always answer in Czech.\n", claude, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AiSync_ProjectSetup_IsFilledFromTheProjectsOnceAndNeverOverwritten()
    {
        using var root = new TempRoot();
        root.WriteProject("src/Contoso.Logic/Contoso.Logic.csproj", """<PackageReference Include="Pillaro.Dataverse.PluginFramework" Version="1.3.0" />""");
        root.WriteProject("src/Contoso.Tests/Contoso.Tests.csproj", """<PackageReference Include="Pillaro.Dataverse.PluginFramework.Testing" Version="1.3.0" />""");

        await PluginCommandRouter.RunAsync(["ai-sync", "--root", root.Path]);

        var setupPath = root.File(AiInstructionLayout.ProjectSetupPath);
        var setup = File.ReadAllText(setupPath);
        Assert.Contains("`src/Contoso.Logic`", setup, StringComparison.Ordinal);
        Assert.Contains("`src/Contoso.Tests`", setup, StringComparison.Ordinal);

        File.WriteAllText(setupPath, "our own setup\n");
        await PluginCommandRouter.RunAsync(["ai-sync", "--root", root.Path]);

        Assert.Equal("our own setup\n", File.ReadAllText(setupPath));
    }

    [Fact]
    public async Task AiSync_ManagedFileNoLongerShipped_IsRemoved()
    {
        using var root = new TempRoot();
        await PluginCommandRouter.RunAsync(["ai-sync", "--root", root.Path]);

        // A file an older version shipped: listed in the manifest, not shipped any more.
        var stale = root.File(".pillaro/ai/rules/99-retired.md");
        File.WriteAllText(stale, "old rule\n");
        var manifestPath = root.File(AiInstructionLayout.ManifestPath);
        File.WriteAllText(manifestPath, File.ReadAllText(manifestPath).Replace("\"Files\": [", "\"Files\": [\n    \".pillaro/ai/rules/99-retired.md\",", StringComparison.Ordinal));

        await PluginCommandRouter.RunAsync(["ai-sync", "--root", root.Path]);

        Assert.False(File.Exists(stale));
    }

    [Fact]
    public async Task AiSync_Check_ReturnsExitCode3WhenOutOfDateAnd0AfterASync()
    {
        using var root = new TempRoot();

        var before = await PluginCommandRouter.RunAsync(["ai-sync", "--root", root.Path, "--check"]);
        Assert.Equal(3, before);
        Assert.False(File.Exists(root.File("AGENTS.md")), "--check must not write anything");

        await PluginCommandRouter.RunAsync(["ai-sync", "--root", root.Path]);
        var after = await PluginCommandRouter.RunAsync(["ai-sync", "--root", root.Path, "--check"]);

        Assert.Equal(0, after);
    }

    [Theory]
    [InlineData("--frobnicate")]
    [InlineData("--root")]
    public async Task AiSync_InvalidOptions_ReturnExitCode2(string option)
    {
        var args = option == "--root"
            ? new[] { "ai-sync", "--root", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()) }
            : ["ai-sync", option];

        var exitCode = await PluginCommandRouter.RunAsync(args);

        Assert.Equal(2, exitCode);
    }

    private static List<string> BrokenRelativeLinks(string root)
    {
        var broken = new List<string>();
        var link = new Regex(@"\]\((?<url>[^)\s]+)\)", RegexOptions.CultureInvariant);

        foreach (var file in Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories))
        {
            foreach (Match match in link.Matches(File.ReadAllText(file)))
            {
                var url = match.Groups["url"].Value;
                if (url.StartsWith('#') || url.Contains(':', StringComparison.Ordinal))
                {
                    continue;
                }

                var path = url.Split('#')[0];
                var target = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, path));
                if (!File.Exists(target) && !Directory.Exists(target))
                {
                    broken.Add($"{Path.GetRelativePath(root, file)} -> {url}");
                }
            }
        }

        return broken;
    }

    private sealed class TempRoot : IDisposable
    {
        public TempRoot()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "ai-sync-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string File(string relativePath)
        {
            return System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
        }

        public void WriteProject(string relativePath, string itemGroupContent)
        {
            var file = File(relativePath);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(file)!);
            System.IO.File.WriteAllText(file, $"<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup>{itemGroupContent}</ItemGroup></Project>");
        }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
