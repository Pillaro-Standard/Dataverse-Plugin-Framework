namespace $safeprojectname$.Plugins;

public abstract class PluginBase(string unsecureConfig, string secureConfig) : PluginFramework.Plugins.PluginBase(unsecureConfig, secureConfig)
{
     public override string GetVersion()
     {
         return "1.0";
     }
}