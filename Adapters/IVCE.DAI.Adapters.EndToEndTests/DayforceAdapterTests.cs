using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using NUnit.Framework;
using IVCE.DAI.Adapters.Extensions;
using Azure.Identity;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Graph;
using IVCE.DAI.Domain.Models.Dayforce;
using IVCE.DAI.Adapters.Config;

namespace IVCE.DAI.Adapters.EndToEndTests
{
    public class DayforceAdpterTests
    {
        private IServiceProvider _serviceProvider;
        private IConfigurationRoot _configuration;
        private IConfigurationRefresher _refresher;
        private IModuleCountryContext _moduleCountryContext;
        private TelemetryClient telemetryClient;
        private GraphServiceClient _graphServiceClient;

        [SetUp]
        public void Setup()
        {
            var serviceCollection = new ServiceCollection();

            _configuration = ConfigureServices(serviceCollection);

            _serviceProvider = serviceCollection.BuildServiceProvider();


        }

        private IConfigurationRoot ConfigureServices(IServiceCollection services)
        {
            var settings = new ConfigurationBuilder()
                    .AddJsonFile("Config/appsettings.json")
                    .AddEnvironmentVariables()
                    .Build();

            var o = new DefaultAzureCredentialOptions();
            o.VisualStudioTenantId = settings["AzureAd:TenantId"];
            //  configurationBuilder.AddAzureKeyVault(new Uri(preConfig["KeyVaultName"]), new DefaultAzureCredential(o));


            var azureConfiguration = new ConfigurationBuilder()
               .AddJsonFile("Config/appsettings.json")
               .AddEnvironmentVariables()
               // .AddApplicationInsightsSettings(developerMode:true,endpointAddress:"",instrumentationKey:"")
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


            var moduleSettingsListTask = azureConfiguration.DeserialiseForAsync<ModuleCountryContext>("ModuleSettings").GetAwaiter();
            var moduleSettingsList = moduleSettingsListTask.GetResult();

            _moduleCountryContext = moduleSettingsList.FirstOrDefault(m => m.AppTag == "IBERIA_AAD");

            var httpClientBuilder = services.AddHttpClient("IBERIA_AAD", client =>
            {
                client.BaseAddress = new Uri(_moduleCountryContext.ApiBaseUri.ToString());
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            services.AddFeatureManagement();
            services.AddSingleton(new TelemetryClient(new TelemetryConfiguration("7ed730db-942f-4fbb-8d07-97dfdabea1a5")));
            //services.AddSingleton<IConfiguration>(azureConfiguration);
            services.AddScoped(c => azureConfiguration);
            services.AddSingleton(_refresher);
            services.AddSingleton(typeof(DayforceHttpAdapter));

            return azureConfiguration;
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

        [Test]
        public async Task WhenCallingReceiveAsyncAResultIsReturned()
        {
            var inboundHttpAdapter = _serviceProvider.GetRequiredService<DayforceHttpAdapter>();

            var list = await inboundHttpAdapter.ReceiveAsync<EmployeeChangesReport>(_moduleCountryContext);

            Assert.IsTrue(list.Data.Rows.Count() > 0);


        }

        //[Test]
        //public async Task WhenCallGraphServiceClientUsersAreReturned()
        //{
        //    string employeeXrefCode = "020720201500";
        //    var graphClient = _serviceProvider.GetRequiredService<GraphServiceClient>();

        //    var graphResult = graphClient.Users.Request().GetAsync().Result;

        //    var ret = await graphClient.Users
        //          .Request()
        //           .Header("ConsistencyLevel", "eventual")
        //              .Filter($"employeeID eq '{employeeXrefCode}'")
        //                  .Select("id,userPrincipalName,employeeId")
        //                      .GetAsync();

        //    var user = ret.FirstOrDefault();

        //    Assert.IsTrue(graphResult.ToList().Count > 0);
        //}

        //[Test]
        //public async Task WhenCallGetAADUserByEmployeeXrefCodeAsyncUsersAreReturned()
        //{

        //    var graphServiceClientAdapter = _serviceProvider.GetRequiredService<ADGraphServiceClientAdapter>();

        //    var graphUser = await graphServiceClientAdapter.GetAADUserByEmployeeXrefCodeAsync("020720201500");

        //    Assert.IsTrue(graphUser.UserPrincipalName.ToString().Length > 0);
        //}

        //[Test]
        //public async Task WhenCallGetAADUserByUserPrincipalNameAsyncUsersAreReturned()
        //{

        //    var graphServiceClientAdapter = _serviceProvider.GetRequiredService<ADGraphServiceClientAdapter>();

        //    var graphUser = await graphServiceClientAdapter.GetAADUserByUserPrincipalNameAsync("raul.valero@41wn2f.onmicrosoft.com");

        //    Assert.IsTrue(graphUser.EmployeeId.ToString().Length > 0);
        //}
    }
}
