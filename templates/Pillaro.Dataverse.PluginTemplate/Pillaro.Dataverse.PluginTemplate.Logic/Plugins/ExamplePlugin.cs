using Pillaro.Dataverse.PluginTemplate.Logic.Tasks.Example;
using Pillaro.Dataverse.PluginFramework.Plugins;

namespace Pillaro.Dataverse.PluginTemplate.Logic.Plugins
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
