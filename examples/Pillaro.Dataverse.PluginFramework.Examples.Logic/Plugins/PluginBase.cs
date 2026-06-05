namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Plugins
{
    public abstract class PluginBase(string unsecureConfig, string secureConfig) : PluginFramework.Plugins.PluginBase(unsecureConfig, secureConfig)
    {
        protected const string SolutionName = "PillaroPluginFrameworkExamples";

        public override string GetVersion()
        {
            return "1.0";
        }
    }
}
