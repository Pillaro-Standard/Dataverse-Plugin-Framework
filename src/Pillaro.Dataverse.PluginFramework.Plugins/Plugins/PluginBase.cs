namespace Pillaro.Dataverse.PluginFramework.Plugins.Plugins
{
    public abstract class PluginBase : PluginFramework.Plugins.PluginBase
    {
        protected PluginBase(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
        }

        public override string GetVersion()
        {
            return "1.1";
        }
    }
}
