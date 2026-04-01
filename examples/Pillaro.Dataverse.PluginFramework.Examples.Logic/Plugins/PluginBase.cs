namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Plugins
{
    public class PluginBase : PluginFramework.Plugins.PluginBase
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
