namespace $safeprojectname$.Logic.Plugins;

public abstract class PluginBase(string unsecureConfig, string secureConfig) : Pillaro.Dataverse.PluginFramework.Plugins.PluginBase(unsecureConfig, secureConfig)
{
    /// <summary>
    /// Prefix for plugin step names. Keeps every step of this solution together in flat
    /// Dataverse lists and separates them from Microsoft and ISV steps.
    /// Step names follow: {StepPrefix} {entity} {Message} {Stage} {Mode}
    /// </summary>
    protected const string StepPrefix = "$safeprojectname$";

    public override string GetVersion()
    {
        return "1.0";
    }
}
