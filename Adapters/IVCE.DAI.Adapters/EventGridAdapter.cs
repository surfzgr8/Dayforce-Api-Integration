using System;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using Azure;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.ApplicationInsights;
using IVCE.DAI.Adapters.Config;

namespace IVCE.DAI.Adapters
{
    public interface IEventGridAdapter
    {

        Response PublishEvent<TMessage>(EventGridEvent eventGridMessage, IModuleCountryContext moduleCountryContext);
        Task<Response> PublishEventAsync<TMessage>(EventGridEvent eventGridMessage, IModuleCountryContext moduleCountryContext);
        string PublishEventList(IEnumerable<EventGridEvent> eventGridMessageList, IModuleCountryContext moduleCountryContext);
        Task<string> PublishEventListAsync(IEnumerable<EventGridEvent> eventGridMessageList, IModuleCountryContext moduleCountryContext);
        void InitialiseTokenCache();

    }
    public class EventGridAdapter : IEventGridAdapter
    {
        private readonly IConfigurationRoot _configuration;
        private readonly IConfigurationRefresher _configurationRefresher;
        private readonly TelemetryClient _telemetryClient;



        public EventGridAdapter(IConfigurationRoot configuration, IConfigurationRefresher configurationRefresher, TelemetryClient telemetryClient)
        {

            _configuration = configuration;
            _configurationRefresher = configurationRefresher;
            _telemetryClient = telemetryClient;


        }

        public void InitialiseTokenCache()
        {
            throw new NotImplementedException();
        }

        public Response PublishEvent<TMessage>(EventGridEvent eventGridMessage, IModuleCountryContext moduleCountryContext)
        {
            try
            {
                var endpoint = new Uri(moduleCountryContext.EventGridTopicEndpoint);

                // Topic key is stored/secured as a KV reference in Azure App Config
                var credential = new AzureKeyCredential(_configuration[moduleCountryContext.EventGridTopicKey]);

                var client = new EventGridPublisherClient(endpoint, credential);

                return client.SendEvent(eventGridMessage);


            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);
                throw;

            }



        }

        public async Task<Response> PublishEventAsync<TMessage>(EventGridEvent eventGridMessage, IModuleCountryContext moduleCountryContext)
        {
            try
            {
                var endpoint = new Uri(moduleCountryContext.EventGridTopicEndpoint);
                // Topic key is stored/secured as a KV reference in Azure App Config
                var credential = new AzureKeyCredential(_configuration[moduleCountryContext.EventGridTopicKey]);

                var client = new EventGridPublisherClient(endpoint, credential);

                return await client.SendEventAsync(eventGridMessage);
            }
            catch (Exception)
            {

                throw;

            }

        }

        public string PublishEventList(IEnumerable<EventGridEvent> eventGridMessageList, IModuleCountryContext moduleCountryContext)
        {
            try
            {
                var endpoint = new Uri(moduleCountryContext.EventGridTopicEndpoint);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

                // Topic key is stored/secured as a KV reference in Azure App Config
                var credential = new AzureKeyCredential(_configuration[moduleCountryContext.EventGridTopicKey]);

                var client = new EventGridPublisherClient(endpoint, credential);

                using (var response = client.SendEvents(eventGridMessageList))
                {               
                   return response.ReasonPhrase;
                }
            }
            catch (Exception)
            {
           
                throw;

            }

        }
        public async Task<string> PublishEventListAsync(IEnumerable<EventGridEvent> eventGridMessageList, IModuleCountryContext moduleCountryContext)
        {
            try
            {
                var endpoint = new Uri(moduleCountryContext.EventGridTopicEndpoint);

                // Topic key is stored/secured as a KV reference in Azure App Config
                var credential = new AzureKeyCredential(_configuration[moduleCountryContext.EventGridTopicKey]);

                var client = new EventGridPublisherClient(endpoint, credential);

                using (var response =await client.SendEventsAsync(eventGridMessageList))
                {
                    return response.ReasonPhrase;
                }
            }
            catch (Exception)
            {
                //_telemetryClient.TrackException(ex);
                throw;

            }
        }
    }
}
