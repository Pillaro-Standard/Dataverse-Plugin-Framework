using System.Text.Json;
using Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

namespace Pillaro.Dataverse.PluginFramework.Cli.Configuration;

internal static class PillaroSettingsLoader
{
    public const string DefaultFileName = "PillaroSettings.json";

    public static async Task<PillaroSettings> LoadAsync(CommandLineOptions options)
    {
        var path = options.Get("settings") ?? DefaultFileName;
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Pillaro settings file was not found. Expected '{path}'.", path);
        }

        await using var stream = File.OpenRead(path);
        var settings = await JsonSerializer.DeserializeAsync(stream, PillaroSettingsJsonContext.Default.PillaroSettings);
        return settings ?? throw new InvalidOperationException($"Pillaro settings file '{path}' is empty or invalid.");
    }

    public static async Task<string> WritePacModelBuilderSettingsAsync(PillaroSettings settings, IReadOnlyCollection<string> entityNames)
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

        var artifactsDirectory = Path.GetFullPath("artifacts");
        Directory.CreateDirectory(artifactsDirectory);
        var path = Path.Combine(artifactsDirectory, "pac-modelbuilder-settings.json");
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, modelBuilderSettings, PillaroSettingsJsonContext.Default.PacModelBuilderSettings);
        return path;
    }
}
