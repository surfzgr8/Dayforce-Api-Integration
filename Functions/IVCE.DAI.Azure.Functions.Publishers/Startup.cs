using System;
using System.IO;
using System.Linq;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Azure.Cosmos;
using Microsoft.FeatureManagement;
using IVCE.DAI.Adapters;
using IVCE.DAI.Adapters.Extensions;
using Microsoft.Identity.Client;
using Microsoft.Graph;
using IVCE.DAI.Adapters.Config;

[assembly: FunctionsStartup(typeof(IVCE.DAI.Azure.Functions.Publishers.Startup))]

namespace IVCE.DAI.Azure.Functions.Publishers
{
    public class Startup : FunctionsStartup
    {
      
        private IConfigurationRoot _azureConfiguration;
        private IConfigurationRefresher _refresher;
        private IModuleCountryContext _moduleCountryContextSettings;

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();


            var settings= builder.ConfigurationBuilder
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, "appsettings.json"), optional: true, reloadOnChange: false)
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"), optional: true, reloadOnChange: false)
                    .AddEnvironmentVariables()
                        .Build();


            var o = new DefaultAzureCredentialOptions();
            o.VisualStudioTenantId = settings["AzureAd:TenantId"];

             _azureConfiguration = new ConfigurationBuilder() 
               .AddEnvironmentVariables()
               .AddAzureAppConfiguration(options =>
               {
                   options.Connect(settings["ConnectionStrings:AzureAppConfig"])
                           // .Select(KeyFilter.Any, settings["DOTNET_ENVIRONMENT"])
                            .ConfigureKeyVault(kv =>
                            {
                                kv.SetCredential(new DefaultAzureCredential(o));
                            })
                           .UseFeatureFlags()
                           .ConfigureRefresh(refresh =>
                           {
                               refresh.Register(".appconfig.featureflag/IBERIA_AAD_FEATURE", settings["DOTNET_ENVIRONMENT"], true)
                                      .SetCacheExpiration(TimeSpan.FromSeconds(1));
                           });
                   _refresher = options.GetRefresher();
               })
               .Build();


        }


        public override void Configure(IFunctionsHostBuilder builder)
        {

            var moduleSettingsListTask = _azureConfiguration.DeserialiseForAsync<ModuleCountryContext>("ModuleSettings").GetAwaiter();
            var moduleSettingsList = moduleSettingsListTask.GetResult();

            // TODO: need to find way to inject App Tag. Need setting for CosmosDb, although this is not country specific. Maybe hold in Func App settings
            _moduleCountryContextSettings = moduleSettingsList.FirstOrDefault(m => m.AppTag == "ESP_AAD");

            var httpClientBuilder = builder.Services.AddHttpClient("IBERIA_AAD", client =>
            {
               // client.BaseAddress = new Uri(_moduleSettings.ApiBaseUri.ToString());
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
            //.AddHttpMessageHandler(h => new AuthenticationDelegatingHandler(configuration));

            builder.Services.AddScoped<GraphServiceClient>(sp => {
                return new GraphServiceClient(AquireToken(_moduleCountryContextSettings, _azureConfiguration));
            });

            builder.Services.AddFeatureManagement();
            builder.Services.AddScoped<IModuleCountryContext, ModuleCountryContext>();
            builder.Services.AddScoped(c => _azureConfiguration);
            builder.Services.AddSingleton(r => _refresher);
            builder.Services.AddSingleton(new CosmosClient(_moduleCountryContextSettings.CosmosDbEndpoint, _azureConfiguration["AADCosmosDbPrimaryKeyKV"], new CosmosClientOptions()));
            builder.Services.AddScoped<ICosmosDbAdapter,CosmosDbAdapter>();
            builder.Services.AddScoped<IDayforceHttpAdapter, DayforceHttpAdapter>();
            builder.Services.AddScoped<IEventGridAdapter, EventGridAdapter>();

        }

        private TokenCredentialAuthProvider AquireToken(IModuleCountryContext moduleCountryContext, IConfigurationRoot azureConfig)
        {
            var AADTenantId = moduleCountryContext.AADTenantId;// "45d4628b-6f3d-4797-9c26-43b13e44e2ba";
            var AADClientId = moduleCountryContext.AADClientId;// "50c2bb81-86bb-437c-a610-509befbdcb35";
            var AADClientSecret = azureConfig[moduleCountryContext.AADClientSecret];// _ "MvQ8Q~Srq3hW6.jIS2aiKtIazsnl9x0oGfmI3c2T";


            ClientSecretCredential clientSecretCredential
                = new ClientSecretCredential(AADTenantId, AADClientId, AADClientSecret);

            return new TokenCredentialAuthProvider(clientSecretCredential);

        }
    }
}
