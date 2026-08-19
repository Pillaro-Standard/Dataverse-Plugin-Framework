namespace Pillaro.Dataverse.PluginFramework.Plugins.Plugins
{
    public abstract class PluginBase : PluginFramework.Plugins.PluginBase
    {
        /// <summary>
        /// Prefix for plugin step names. Keeps every step of this solution together in flat
        /// Dataverse lists and separates them from Microsoft and ISV steps.
        /// Step names follow: {StepPrefix} {entity} {Message} {Stage} {Mode},
        /// or {StepPrefix} {Message} {Stage} {Mode} for steps without a primary entity.
        /// </summary>
        protected const string StepPrefix = "Pillaro Framework";

        protected PluginBase(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {
        }

        public override string GetVersion()
        {
            return "1.1";
        }
    }
}
