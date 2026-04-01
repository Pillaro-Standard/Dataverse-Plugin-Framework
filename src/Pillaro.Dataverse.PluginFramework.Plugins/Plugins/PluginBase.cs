namespace Pillaro.Dataverse.PluginFramework.Plugins.Plugins
{
    public abstract class PluginBase : PluginFramework.Plugins.PluginBase
    {
        public PluginBase(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
        }


        public override string GetSolutionVersion()
        {
            return "1.0";
        }
    }
}