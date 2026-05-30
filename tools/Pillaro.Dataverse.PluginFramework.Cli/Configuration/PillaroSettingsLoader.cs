using System.Text.Json;
using System.Text.RegularExpressions;
using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

namespace Pillaro.Dataverse.PluginFramework.Cli.Configuration;

internal static class PillaroSettingsLoader
{
    public const string DefaultFileName = "PillaroSettings.json";
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static async Task<PillaroSettings> LoadAsync(CommandLineOptions options)
    {
        var path = GetResolvedSettingsPath(options);
        if (path == null)
        {
            var configuredPath = options.Get("settings") ?? DefaultFileName;
            throw new FileNotFoundException($"Pillaro settings file was not found. Expected '{configuredPath}'.");
        }

        await using var stream = File.OpenRead(path);
        var settings = await JsonSerializer.DeserializeAsync<PillaroSettings>(stream, ReadOptions);
        return settings ?? throw new InvalidOperationException($"Pillaro settings file '{path}' is empty or invalid.");
    }

    public static string? GetResolvedSettingsPath(CommandLineOptions options)
    {
        var configuredPath = options.Get("settings") ?? DefaultFileName;
        return ResolveSettingsPath(configuredPath);
    }

    /// <summary>
    /// Resolves the active profile name. --profile CLI flag takes priority, then settings.DefaultProfile.
    /// Throws <see cref="InvalidOperationException"/> when neither is set.
    /// </summary>
    public static string ResolveActiveProfileName(CommandLineOptions options, PillaroSettings settings)
    {
        var profileName = options.Get("profile") ?? NullIfWhiteSpace(settings.DefaultProfile);

        if (profileName == null)
        {
            throw new InvalidOperationException(
                "No profile selected. Set 'defaultProfile' in PillaroSettings.json or pass --profile <name>.");
        }

        return profileName;
    }

    /// <summary>
    /// Resolves the active profile. Throws when the profile name does not exist in settings.
    /// </summary>
    public static PillaroSettingsProfile ResolveActiveProfile(CommandLineOptions options, PillaroSettings settings)
    {
        var profileName = ResolveActiveProfileName(options, settings);

        if (!settings.Profiles.TryGetValue(profileName, out var profile))
        {
            var available = settings.Profiles.Count > 0
                ? string.Join(", ", settings.Profiles.Keys)
                : "<none defined>";
            throw new InvalidOperationException(
                $"Profile '{profileName}' not found in PillaroSettings.json. Available profiles: {available}.");
        }

        return profile;
    }

    /// <summary>
    /// Resolves the plugin assembly path.
    /// Priority: --assembly CLI flag → active profile pluginAssemblyPath.
    /// </summary>
    public static string ResolveAssembly(CommandLineOptions options, PillaroSettings settings)
    {
        if (options.Get("assembly") is { } cliAssembly && !string.IsNullOrWhiteSpace(cliAssembly))
        {
            return cliAssembly;
        }

        var profile = ResolveActiveProfile(options, settings);
        return profile.PluginAssemblyPath;
    }

    /// <summary>
    /// Resolves the connection string environment variable name from settings.
    /// Falls back to DV_CONN when not configured.
    /// </summary>
    public static string ResolveConnectionStringEnvironmentVariable(PillaroSettings settings)
    {
        var value = settings.Dataverse.ConnectionStringEnvironmentVariable;
        return string.IsNullOrWhiteSpace(value) ? "DV_CONN" : value;
    }

    /// <summary>
    /// Resolves a path value read from PillaroSettings.json.
    /// Expands environment variable tokens first, then resolves relative paths against
    /// the directory that contains the settings file, not against the current working directory.
    /// Supports tokens: $(NAME), ${NAME}, %NAME%, $NAME.
    /// Unknown tokens are left unchanged so the downstream file-existence check reports the unresolved path.
    /// </summary>
    public static string ResolveConfiguredPath(string configuredPath, string settingsDirectory)
    {
        var expanded = ExpandEnvironmentTokens(configuredPath);
        return Path.IsPathRooted(expanded)
            ? Path.GetFullPath(expanded)
            : Path.GetFullPath(Path.Combine(settingsDirectory, expanded));
    }

    /// <summary>
    /// Resolves a path supplied directly on the command line (e.g. --assembly).
    /// Relative paths are resolved against the current working directory.
    /// </summary>
    public static string ResolveCommandLinePath(string path)
    {
        var expanded = ExpandEnvironmentTokens(path);
        return Path.GetFullPath(expanded);
    }

    private static string ExpandEnvironmentTokens(string value)
    {
        // $(NAME) and ${NAME} – MSBuild / Azure DevOps style
        value = Regex.Replace(value, @"\$\(([^)]+)\)", m => ResolveToken(m.Groups[1].Value, m.Value));
        value = Regex.Replace(value, @"\$\{([^}]+)\}", m => ResolveToken(m.Groups[1].Value, m.Value));

        // %NAME% – Windows cmd style
        value = Regex.Replace(value, @"%([^%]+)%", m => ResolveToken(m.Groups[1].Value, m.Value));

        // $NAME – Unix style (word boundary, uppercase letters/digits/underscores)
        value = Regex.Replace(value, @"\$([A-Z_][A-Z0-9_]*)", m => ResolveToken(m.Groups[1].Value, m.Value));

        return value;
    }

    private static string ResolveToken(string name, string original)
    {
        return Environment.GetEnvironmentVariable(name) ?? original;
    }

    private static string? ResolveSettingsPath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
        {
            return File.Exists(configuredPath) ? configuredPath : null;
        }

        var directPath = Path.GetFullPath(configuredPath);
        if (File.Exists(directPath))
        {
            return directPath;
        }

        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, configuredPath);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
