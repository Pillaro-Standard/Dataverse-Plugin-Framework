namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Plugins
{
    public abstract class PluginBase : PluginFramework.Plugins.PluginBase
    {
        protected PluginBase(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
        }

        public override string GetVersion()
        {
            return "1.0";
        }
    }
}
