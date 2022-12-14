using System;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Cosmos;
using NUnit.Framework;
using Azure.Identity;
using IVCE.DAI.Adapters.Extensions;


using IVCE.DAI.Domain.Models.Canonical;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System.Collections.Generic;
using IVCE.DAI.Adapters.Config;

namespace IVCE.DAI.Adapters.EndToEndTests
{
    public class CosmosDbAdapterTests
    {
        private IServiceProvider _serviceProvider;
        private IConfigurationRoot _configuration;
        private IConfigurationRefresher _refresher;
        private ModuleCountryContext _moduleCountryContext;


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

            _moduleCountryContext = moduleSettingsList.FirstOrDefault(m => m.AppTag == "ESP_AAD");

            services.AddFeatureManagement();
          //  services.AddSingleton<IConfigurationRoot>(configuration);
            services.AddScoped(c => azureConfiguration);
            services.AddSingleton(new TelemetryClient(new TelemetryConfiguration("7ed730db-942f-4fbb-8d07-97dfdabea1a5")));
            services.AddSingleton(_refresher);
            services.AddSingleton(new CosmosClient(_moduleCountryContext.CosmosDbEndpoint, azureConfiguration[_moduleCountryContext.CosmosDbPrimaryKey], new CosmosClientOptions()));
            services.AddSingleton(typeof(CosmosDbAdapter));

            return azureConfiguration;
        }



        private CanonicalWorkerItem canonicalAADUserItem => new CanonicalWorkerItem
        {
            id= "suttonThree12345",//Guid.NewGuid().ToString(),
            partitionKey = "suttonThree12345",
            Header = new Header
            {
                EmployeeXRefCode = "API_Test_1",
                ApplicationId="DAI",
                LogicalDelete=false,
                OperationStatus="NEW"
              
            },
            WorkerItem=new Item
            {
                PreferredLastName = "Albany",
                FirstName = "Alba",
                LastName = "Sutton",
                EmployeeEmploymentNumber = "sutton12345",
                EmploymentStatusName = "ACTIVE"
            }
        };

        [Test]
        public async Task WhenCallingUpsertItemsToContainerAsyncResultIsReturned()
        {

            var cosmosDbAdpter = _serviceProvider.GetRequiredService<CosmosDbAdapter>();

            var response = await cosmosDbAdpter.UpsertItemsToContainerAsync<CanonicalWorkerItem>
                (this.canonicalAADUserItem, this.canonicalAADUserItem.id, this.canonicalAADUserItem.partitionKey, _moduleCountryContext);

            Assert.IsTrue(response.StatusCode == HttpStatusCode.Created);

        }


        [Test]
        public async Task WhenCallingQueryItemsAsyncResultIsReturned()
        {

            var cosmosDbAdpter = _serviceProvider.GetRequiredService<CosmosDbAdapter>();

            var response = await cosmosDbAdpter.QueryItemsAsync<CanonicalWorkerItem>(this.canonicalAADUserItem.partitionKey,"ESP_AAD");

            Assert.IsTrue(response.Any() == true);

        }

        [Test]
        public async Task WhenCallingQueryItemsAsyncForExistingUsers()
        {

            var cosmosDbAdpter = _serviceProvider.GetRequiredService<CosmosDbAdapter>();

            var newHireList =
                    cosmosDbAdpter.QueryItemsAsync<CanonicalWorkerItem>(this.canonicalAADUserItem.partitionKey, "ESP_AAD").Result;

            // Does the user already exist
            var itemResponse = newHireList.Any() ? null : new List<CanonicalWorkerItem>();

        
            Assert.IsTrue(itemResponse == null);

        }

        [Test]
        public async Task WhenCallingQueryItemsAsyncForTimer()
        {

            var cosmosDbAdpter = _serviceProvider.GetRequiredService<CosmosDbAdapter>();

            var currentTime = DateTimeOffset.UtcNow.ToString("HH:mm");

            var timerSchedules = await _configuration.DeserialiseForAsync<TimerSchedule>("TimerSchedules");
            var timerSchedule = timerSchedules.FirstOrDefault(T => T.ScheduledTime== "06:00");

            if (timerSchedule != null)
            { }


            Assert.IsTrue(timerSchedule != null);

        }

        [Test]
        public async Task WhenCallingQueryItemsAsyncForTerminatedUsers()
        {
            var cosmosDbAdpter = _serviceProvider.GetRequiredService<CosmosDbAdapter>();

            // get all terminated users hires from eventstore with a start date within the next 14 days
            var  sql = $"select * from c where c.AADUserItem.EmploymentStatusCode in ('TERMINATED') " +
                $" and c.Header.OperationStatus in ('Registered')" +
                $" and substring(c.AADUserItem.LeaveDate,0,10) >= GetCurrentDateTime()";

            var terminatedUsersList
                    = await cosmosDbAdpter.QueryExistingItemsAsync<CanonicalWorkerItem>(sql, "ESP_AAD");

            // iterate through each employee and create record in AAD
            terminatedUsersList.ToList().ForEach(async canonicalAADUserItem =>
            {
                try
                {
                 //   var success = await this.ProcessTerminatedEmployees(canonicalAADUserItem, moduleCountryContext);
                }
                catch (Exception ex)
                {
                  //  _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_Orchestrator:ProcessTerminatedEmployees - failed for AAD User with EmployeeXrefCode:{canonicalAADUserItem.partitionKey}");
                    //_telemetryClient.TrackException(ex);
                }
            });

            Assert.IsTrue(terminatedUsersList.Any());

        }
    }
}