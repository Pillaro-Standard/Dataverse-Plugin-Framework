using $safeprojectname$.Tasks.Example;
using Pillaro.Dataverse.PluginFramework.Plugins;

namespace $safeprojectname$.Plugins
{
    public class ExamplePlugin : PluginBase
    {
        public ExamplePlugin(string unsecureConfig, string secureConfig)
            : base(unsecureConfig, secureConfig)
        {
            RegisterTask<ExampleTask>(
                PluginStage.Prevalidation,
                new[] { "Create" },
                string.Empty,
                PluginMode.Synchronous);
        }
    }
}

