using System.Text.Json;
using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

namespace Pillaro.Dataverse.PluginFramework.Cli.Configuration;

internal static class PillaroSettingsLoader
{
    public const string DefaultFileName = "PillaroSettings.json";
    public const string DefaultProfilesFileName = "dataverse-profiles.json";

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

    public static string ResolveConfiguredPath(string configuredPath)
    {
        return Path.GetFullPath(configuredPath);
    }

    public static async Task<DataverseProfile?> TryLoadProfileAsync(CommandLineOptions options, PillaroSettings? settings = null)
    {
        var explicitConnectionString = options.Get("conn") ?? options.Get("sdk-connection-string") ?? options.Get("connection-string");
        if (!string.IsNullOrWhiteSpace(explicitConnectionString))
        {
            return new DataverseProfile
            {
                ConnectionString = explicitConnectionString,
                PacProfile = options.Get("pac-profile") ?? options.Get("pac-auth-profile") ?? string.Empty,
            };
        }

        var profilesPath = GetProfilesPath(options);
        if (!File.Exists(profilesPath))
        {
            return null;
        }

        await using var stream = File.OpenRead(profilesPath);
        var profilesDocument = await JsonSerializer.DeserializeAsync<DataverseProfilesDocument>(stream, ReadOptions);
        if (profilesDocument == null)
        {
            return null;
        }

        var profileName = options.Get("profile")
            ?? settings?.Profile
            ?? profilesDocument.DefaultProfile;

        if (string.IsNullOrWhiteSpace(profileName))
        {
            return null;
        }

        return profilesDocument.Profiles.TryGetValue(profileName, out var profile)
            ? profile
            : null;
    }

    public static string GetProfilesPath(CommandLineOptions options)
    {
        var configuredPath = options.Get("profiles");
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            return Path.GetFullPath(configuredPath);
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(userProfile, ".pillaro", DefaultProfilesFileName);
    }

    public static async Task<string> WritePacModelBuilderSettingsAsync(PillaroSettings settings, CommandLineOptions options, IReadOnlyCollection<string> entityNames)
    {
        var earlyBound = settings.EarlyBound;
        var modelBuilderSettings = new PacModelBuilderSettings
        {
            SuppressINotifyPattern = earlyBound.SuppressINotifyPattern,
            SuppressGeneratedCodeAttribute = earlyBound.SuppressGeneratedCodeAttribute,
            Namespace = earlyBound.Namespace,
            ServiceContextName = earlyBound.ServiceContextName,
            GenerateSdkMessages = earlyBound.GenerateSdkMessages,
            EmitFieldsClasses = earlyBound.EmitFieldsClasses,
            EntityTypesFolder = earlyBound.EntityTypesFolder,
            MessagesTypesFolder = earlyBound.MessagesTypesFolder,
            OptionSetsTypesFolder = earlyBound.OptionSetsTypesFolder,
            EntityNamesFilter = entityNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList(),
            MessageNamesFilter = earlyBound.Messages.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToList(),
            EmitVirtualAttributes = earlyBound.EmitVirtualAttributes,
        };

        var artifactsDirectory = ResolveConfiguredPath("artifacts");
        Directory.CreateDirectory(artifactsDirectory);
        var path = Path.Combine(artifactsDirectory, "pac-modelbuilder-settings.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, modelBuilderSettings, PillaroSettingsJsonContext.Default.PacModelBuilderSettings);
        return path;
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
}
