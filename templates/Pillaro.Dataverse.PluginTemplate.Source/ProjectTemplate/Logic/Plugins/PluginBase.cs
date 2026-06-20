namespace $safeprojectname$.Logic.Plugins;

public abstract class PluginBase(string unsecureConfig, string secureConfig) : Pillaro.Dataverse.PluginFramework.Plugins.PluginBase(unsecureConfig, secureConfig)
{
    public override string GetVersion()
    {
        return "1.0";
    }
}
