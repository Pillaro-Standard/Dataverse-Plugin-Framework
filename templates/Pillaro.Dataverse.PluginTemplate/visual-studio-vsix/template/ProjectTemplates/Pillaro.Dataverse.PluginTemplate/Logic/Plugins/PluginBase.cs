namespace $safeprojectname$.Plugins
{
    public abstract class PluginBase : Pillaro.Dataverse.PluginFramework.Plugins.PluginBase
    {
        protected PluginBase(string unsecureConfig, string secureConfig)
            : base(unsecureConfig, secureConfig)
        {
        }

        public override string GetVersion()
        {
            return "1.0";
        }
    }
}

