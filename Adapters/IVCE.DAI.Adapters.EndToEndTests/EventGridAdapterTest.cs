using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using NUnit.Framework;
using IVCE.DAI.Adapters.Extensions;
using Azure.Messaging.EventGrid;
using Azure.Identity;
using System.Text.Json;
using Azure.Core.Serialization;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using IVCE.DAI.Adapters.Config;

namespace IVCE.DAI.Adapters.EndToEndTests
{
    public class InboundEventGridAdapterTest
    {
        private IServiceProvider _serviceProvider;
        private IConfigurationRoot _configuration;
        private IConfigurationRefresher _refresher;
        private IModuleCountryContext _moduleCountryContext;
        private TelemetryClient telemetryClient;
      

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



            services.AddFeatureManagement();
            services.AddSingleton(new TelemetryClient(new TelemetryConfiguration("7ed730db-942f-4fbb-8d07-97dfdabea1a5")));
            services.AddScoped(c => azureConfiguration);
            services.AddSingleton(_refresher);
            services.AddSingleton(typeof(EventGridAdapter));

            return azureConfiguration;
        }


        [Test]
        public async Task WhenCallingPublishAsyncAResultIsReturned()
        {


            _moduleCountryContext.EventGridOperationDate = DateTimeOffset.Now;
            _moduleCountryContext.EventGridOperationRouteTo = $"{_moduleCountryContext.EventGridTopicName}";
            _moduleCountryContext.EventGridOperationStatus = "PublishEmplyeeEvent";

            var eventMessage = new EventGridEvent(
             subject: $"{_moduleCountryContext.AppTag}",
             eventType: $"Employees.Registration.{_moduleCountryContext?.EventGridOperationEventType}",
             dataVersion: "1.0",
                 data: new
                 {
                     OperationDate = $"{_moduleCountryContext?.EventGridOperationDate}",
                     OperationStatus = $"{_moduleCountryContext?.EventGridOperationStatus}",
                     OperationRouteTo = $"{_moduleCountryContext?.EventGridOperationRouteTo}"
                 }
          );

            var inboundEventGridAdapter = _serviceProvider.GetRequiredService<EventGridAdapter>();

            var response = await inboundEventGridAdapter.PublishEventAsync<EventGridEvent>(eventMessage, _moduleCountryContext);

            Assert.IsTrue(response.IsError==false);


        }

        [Test]
        public void WhenCallingPublishEventListResultIsReturned()
        {

            var inboundEventGridAdapter = _serviceProvider.GetRequiredService<EventGridAdapter>();
            _moduleCountryContext.EventGridOperationDate = DateTimeOffset.Now;
            _moduleCountryContext.EventGridOperationRouteTo = $"{_moduleCountryContext.EventGridTopicName}";
            _moduleCountryContext.EventGridOperationStatus = "PublishEmployeeSaveEvent";

         
            var eventGridCustomDataSerializer = new JsonObjectSerializer(
                 new JsonSerializerOptions()
                 {
                     PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                 });

            // Add EventGridEvents to a list to publish to the topic
            var eventsList = new List<EventGridEvent>
                {
                    // EventGridEvent with custom model serialized to JSON
                    new EventGridEvent(
                        subject:"Employees.Registration.AAD-Registration",// $"{moduleCountryContext.AppTag}",
                        eventType: "Employees.Registration.AAD-Registration", //$"Employees.Registration.{moduleCountryContext?.OperationEventType}",
                        "1.0",
                        // EventGridEvent with custom model serialized to JSON using a custom serializer
                        eventGridCustomDataSerializer.Serialize
                            (new ModuleCountryContext()
                                {
                                    EventGridOperationDate = DateTimeOffset.Now,
                                    EventGridOperationEventType = "PublishEmployeeSaveEvent",
                                    EventGridOperationRouteTo="UKS-AZ-DAI-AAD-REGISTRATION-TOPIC",
                                    EventGridOperationStatus="Register"
                                })
                            ),
                };


            var response = inboundEventGridAdapter.PublishEventList(eventsList, _moduleCountryContext);

          
       
           Assert.IsTrue(response=="OK");


        }

        [Test]
        public async Task WhenCallingPublishEventListAsyncResultIsReturned()
        {

            var inboundEventGridAdapter = _serviceProvider.GetRequiredService<EventGridAdapter>();
            _moduleCountryContext.EventGridOperationDate = DateTimeOffset.Now;
            _moduleCountryContext.EventGridOperationRouteTo = $"{_moduleCountryContext.EventGridTopicName}";
            _moduleCountryContext.EventGridOperationStatus = "PublishEmployeeSaveEvent";


            var eventGridCustomDataSerializer = new JsonObjectSerializer(
                 new JsonSerializerOptions()
                 {
                     PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                 });

            // Add EventGridEvents to a list to publish to the topic
            var eventsList = new List<EventGridEvent>
                {
                    // EventGridEvent with custom model serialized to JSON
                    new EventGridEvent(
                        subject:"Employees.Registration.AAD-Registration",// $"{moduleCountryContext.AppTag}",
                        eventType: "Employees.Registration.AAD-Registration", //$"Employees.Registration.{moduleCountryContext?.OperationEventType}",
                        "1.0",
                        // EventGridEvent with custom model serialized to JSON using a custom serializer
                        eventGridCustomDataSerializer.Serialize
                            (new ModuleCountryContext()
                                {
                                    EventGridOperationDate = DateTimeOffset.Now,
                                    EventGridOperationEventType = "PublishEmployeeSaveEvent",
                                    EventGridOperationRouteTo="UKS-AZ-DAI-AAD-REGISTRATION-TOPIC",
                                    EventGridOperationStatus="Register"
                                })
                            ),
                };


            var response = await inboundEventGridAdapter.PublishEventListAsync(eventsList, _moduleCountryContext);

            Assert.IsTrue(response == "OK");

        }



    }

}
