using System;
using System.Collections.Generic;
using System.Net.Http;


using Microsoft.Extensions.Configuration;
using IVCE.DAI.Adapters.Extensions;
using System.Linq;
using IVCE.DAI.Adapters.Config;

namespace MultilexHostingDesktopUi.Modules
{
    public interface IModuleSettingsFactory
    {
        IModuleCountryContext CreateModuleSetting(string AppTag, IConfiguration configuration);
    }
    public class ModuleSettingsFactory : IModuleSettingsFactory
    {
        public IModuleCountryContext CreateModuleSetting(string AppTag, IConfiguration configuration)
        {

            var moduleSettingsListTask = configuration.DeserialiseForAsync<ModuleCountryContext>("ModuleSettings").GetAwaiter();
            var moduleSettingsList = moduleSettingsListTask.GetResult();

            return moduleSettingsList.FirstOrDefault(m => m.AppTag == AppTag);

        }
    }
}
