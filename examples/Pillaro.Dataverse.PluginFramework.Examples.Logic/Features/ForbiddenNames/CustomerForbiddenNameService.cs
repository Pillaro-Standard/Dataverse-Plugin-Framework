using Newtonsoft.Json;
using System.Collections.Generic;

namespace Pillaro.Dataverse.PluginFramework.Examples.Logic.Features.ForbiddenNames
{
    internal class CustomerForbiddenNameService
    {
        private readonly SettingsService _settingService;
        public CustomerForbiddenNameService(SettingsService settingService)
        {
            this._settingService = settingService;
        }

        public List<string> GetForbiddenNames()
        {
            var listJson = _settingService.GetJsonValue(SettingsKeys.ForbiddenWords);

            var list = JsonConvert.DeserializeObject<List<string>>(listJson);

            return list;
        }
    }
}
