using System.Text.RegularExpressions;

namespace Pillaro.Dataverse.PluginFramework.Cli.AiInstructions;

/// <summary>Finds the solution's Logic and test projects for the first project-setup.md.</summary>
internal static class AiProjectDiscovery
{
    private static readonly string[] SkippedDirectories = ["bin", "obj", ".git", ".vs", "node_modules", ".pillaro"];

    private static readonly Regex FrameworkReference = new(
        @"Include=""(Pillaro\.Dataverse\.PluginFramework|[^""]*[\\/]Pillaro\.Dataverse\.PluginFramework\.csproj)""",
        RegexOptions.CultureInvariant);

    private static readonly Regex TestingReference = new(
        @"Include=""(Pillaro\.Dataverse\.PluginFramework\.Testing|[^""]*[\\/]Pillaro\.Dataverse\.PluginFramework\.Testing\.csproj)""",
        RegexOptions.CultureInvariant);

    /// <summary>Folders of the Logic and the test project, relative to the root, or null when not found.</summary>
    public static (string? Logic, string? Tests) Find(string root)
    {
        var projects = EnumerateProjects(root).ToList();

        var logic = projects
            .Where(project => FrameworkReference.IsMatch(File.ReadAllText(project)))
            .OrderByDescending(project => project.EndsWith(".Logic.csproj", StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        var tests = projects.FirstOrDefault(project => TestingReference.IsMatch(File.ReadAllText(project)));

        return (FolderOf(root, logic), FolderOf(root, tests));
    }

    private static IEnumerable<string> EnumerateProjects(string directory)
    {
        foreach (var project in Directory.EnumerateFiles(directory, "*.csproj"))
        {
            yield return project;
        }

        foreach (var child in Directory.EnumerateDirectories(directory))
        {
            if (SkippedDirectories.Contains(Path.GetFileName(child), StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var project in EnumerateProjects(child))
            {
                yield return project;
            }
        }
    }

    private static string? FolderOf(string root, string? project)
    {
        if (project is null)
        {
            return null;
        }

        var folder = Path.GetRelativePath(root, Path.GetDirectoryName(project)!).Replace('\\', '/');
        return folder.Length == 0 ? "." : folder;
    }
}
