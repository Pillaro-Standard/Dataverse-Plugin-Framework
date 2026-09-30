using Pillaro.Dataverse.PluginFramework.Cli.AiInstructions;
using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

namespace Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

/// <summary>
/// ai-sync: writes the framework's AI instructions (Claude Code, GitHub Copilot, Codex) into the
/// solution, in the version of this CLI. Offline, no connection.
/// </summary>
internal static class AiSyncCommand
{
    private static readonly string[] SupportedOptions = ["root", "logic-project", "tests-project", "check", "help"];

    public static Task<int> RunAsync(string[] args)
    {
        try
        {
            var options = CommandLineOptions.Parse(args);
            if (options.HasFlag("help"))
            {
                PrintHelp();
                return Task.FromResult(0);
            }

            var unsupported = options.Names.Where(name => !SupportedOptions.Contains(name, StringComparer.OrdinalIgnoreCase)).ToList();
            if (unsupported.Count > 0)
            {
                Console.Error.WriteLine($"Unsupported option(s): {string.Join(", ", unsupported.Select(name => "--" + name))}.");
                return Task.FromResult(2);
            }

            var rootOption = options.Get("root");
            if (rootOption is not null && !Directory.Exists(rootOption))
            {
                Console.Error.WriteLine($"Root folder was not found: {rootOption}");
                return Task.FromResult(2);
            }

            var root = rootOption is not null
                ? Path.GetFullPath(rootOption)
                : AiInstructionSync.FindRoot(Directory.GetCurrentDirectory());

            var check = options.HasFlag("check");
            var version = AiInstructionSource.Version();
            var changes = AiInstructionSync.Run(
                AiInstructionSource.Load(),
                new AiSyncOptions(root, version, check, options.Get("logic-project"), options.Get("tests-project")));

            foreach (var change in changes.Where(change => change.Action != AiSyncAction.Unchanged))
            {
                Console.WriteLine($"  {change.Action.ToString().ToLowerInvariant(),-9} {change.Path}");
            }

            var outdated = changes.Count(change => change.Action != AiSyncAction.Unchanged);
            Console.WriteLine(
                $"AI instructions {version} in {root}: " +
                $"{Count(changes, AiSyncAction.Created)} created, {Count(changes, AiSyncAction.Updated)} updated, " +
                $"{Count(changes, AiSyncAction.Removed)} removed, {Count(changes, AiSyncAction.Unchanged)} unchanged.");

            if (check && outdated > 0)
            {
                Console.Error.WriteLine("AI instructions are out of date. Run 'pillaro-dv ai-sync' to update them.");
                return Task.FromResult(3);
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return Task.FromResult(1);
        }
    }

    private static int Count(IReadOnlyList<AiSyncChange> changes, AiSyncAction action)
    {
        return changes.Count(change => change.Action == action);
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  pillaro-dv ai-sync [--root <folder>] [--check] [--logic-project <folder>] [--tests-project <folder>]");
        Console.WriteLine();
        Console.WriteLine("Writes the AI instructions for Claude Code, GitHub Copilot and Codex into the solution:");
        Console.WriteLine("  AGENTS.md, CLAUDE.md, .github/copilot-instructions.md   only the marked block is replaced");
        Console.WriteLine("  .pillaro/ai/, .github/instructions/pillaro-*,");
        Console.WriteLine("  .claude/skills/pillaro-*, .agents/skills/pillaro-*        managed, overwritten");
        Console.WriteLine("  .pillaro/project-setup.md                                  created once, then yours");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --root <folder>            Solution root. Default: the nearest folder with .git or a solution file.");
        Console.WriteLine("  --check                    Change nothing; exit 3 when the instructions are out of date.");
        Console.WriteLine("  --logic-project <folder>   Logic project folder for a new project-setup.md. Default: discovered.");
        Console.WriteLine("  --tests-project <folder>   Test project folder for a new project-setup.md. Default: discovered.");
        Console.WriteLine("  -h, --help                 Show help.");
    }
}
