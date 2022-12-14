
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.ApplicationInsights;
using IVCE.DAI.Adapters.Extensions;
using IVCE.DAI.Adapters.Config;

namespace IVCE.DAI.Adapters
{
    public interface ICosmosDbAdapter
    {
        public Task<IEnumerable<TItem>> QueryItemsAsync<TItem>(string partitionKey, string appTagName);

        public Task<ItemResponse<TItem>> UpsertItemsToContainerAsync<TItem>(TItem item, string id, string partitionKey, IModuleCountryContext moduleCountryContextParam);

        public ItemResponse<TItem> UpsertItemsToContainer<TItem>(TItem item, string id, string partitionKey, IModuleCountryContext moduleCountryContextParam);

        public Task<ItemResponse<TItem>> ReplaceItemAsync<TItem>(TItem item, string id, string partitionKey, string appTagName);

        public Task<IEnumerable<TItem>> QueryExistingItemsAsync<TItem>(string sql, string appTagName);

        Task DeleteItemAsync<TItem>();

    }
    public class CosmosDbAdapter : ICosmosDbAdapter
    {
        private const string ModuleName = "IVCE.DAI.Adapters.CosmosDbAdapter";

        // The Cosmos client instance
        private readonly CosmosClient _cosmosClient;
        private readonly IConfigurationRoot _configuration;
        private readonly TelemetryClient _telemetryClient;


        public CosmosDbAdapter(IConfigurationRoot configuration, CosmosClient cosmosClient, TelemetryClient telemetryClient)
        {

            _configuration = configuration;
            _cosmosClient = cosmosClient;
            _telemetryClient = telemetryClient;

        }

        public  ItemResponse<TItem> UpsertItemsToContainer<TItem>(TItem item, string id, string partitionKey, IModuleCountryContext moduleCountryContext)
        {
            ItemResponse<TItem> itemResponse = null;
            ContainerResponse containerResponse = null;

            try
            {

                var database = _cosmosClient.GetDatabase(moduleCountryContext.CosmosDbId);
                //  var container = database.GetContainer(moduleSettings.CosmosDbContainerId);
                containerResponse =  database.CreateContainerIfNotExistsAsync(moduleCountryContext.CosmosDbContainerId, "/partitionKey").GetAwaiter().GetResult();

                ContainerProperties properties = containerResponse.Container.ReadContainerAsync().GetAwaiter().GetResult();

                // Read the item to see if it exists.  
                itemResponse =  containerResponse.Container.ReadItemAsync<TItem>(id, new PartitionKey(partitionKey)).GetAwaiter().GetResult();

                var itemBody = itemResponse.Resource;

                itemBody = item;

                itemResponse =  containerResponse.Container.ReplaceItemAsync<TItem>(itemBody, id, new PartitionKey(partitionKey)).GetAwaiter().GetResult();

                _telemetryClient.TrackEvent($"{ModuleName}:UpsertItemsToContainer: Employee:({id}) Replaced in EventStore");


            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Create an item in the container representing the Andersen family. Note we provide the value of the partition key for this item,
                itemResponse = (ItemResponse<TItem>) containerResponse.Container.CreateItemAsync<TItem>(item, new PartitionKey(id)).GetAwaiter().GetResult();

                _telemetryClient.TrackEvent($"{ModuleName}:UpsertItemsToContainer: Employee:({id}) Created in EventStore");


            }
            catch (CosmosException ex)
            {
              
                _telemetryClient.TrackEvent($"{ModuleName}:UpsertItemsToContainer: CatchAllException:{ex.Message.ToString()}");
            }


            return itemResponse;
        }
        public async Task<ItemResponse<TItem>> UpsertItemsToContainerAsync<TItem>(TItem item, string id, string partitionKey,IModuleCountryContext moduleCountryContext)
        {
            ItemResponse<TItem> itemResponse=null;
            ContainerResponse containerResponse=null;

            try
            {

                var database = _cosmosClient.GetDatabase(moduleCountryContext.CosmosDbId);
                //  var container = database.GetContainer(moduleSettings.CosmosDbContainerId);
                containerResponse = await database.CreateContainerIfNotExistsAsync(moduleCountryContext.CosmosDbContainerId, "/partitionKey");

                ContainerProperties properties =  containerResponse.Container.ReadContainerAsync().GetAwaiter().GetResult();
               
                // Read the item to see if it exists.  
                itemResponse = await  containerResponse.Container.ReadItemAsync<TItem>(id, new PartitionKey(partitionKey));

           
                var itemBody = itemResponse.Resource;

                itemBody = item;

                itemResponse =  await containerResponse.Container.ReplaceItemAsync<TItem>(itemBody, id, new PartitionKey(partitionKey));

                _telemetryClient.TrackEvent($"{ModuleName}:UpsertItemsToContainerAsync: Employee:({id}) Replaced in EventStore");

                

            }
            catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound) 
            {
                // Create an item in the container representing the Andersen family. Note we provide the value of the partition key for this item,
                itemResponse = (ItemResponse<TItem>)await containerResponse.Container.CreateItemAsync<TItem>(item, new PartitionKey(id));

                _telemetryClient.TrackEvent($"{ModuleName}:UpsertItemsToContainerAsync: Employee:({id}) Created in EventStore");

             

            }
            catch (CosmosException ex)
            {

                _telemetryClient.TrackEvent($"{ModuleName}:UpsertItemsToContainerAsync: CatchAllException:{ex.Message.ToString()}");
            }

            return itemResponse;
        }

        public Task DeleteItemAsync<TItem>()
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<TItem>> QueryItemsAsync<TItem>(string partitionKey, string appTagName)
        {
            var moduleCountryContextList = await _configuration.DeserialiseForAsync<ModuleCountryContext>("ModuleSettings");
            var moduleCountryContext = moduleCountryContextList.FirstOrDefault(m => m.AppTag == appTagName);

            var database = _cosmosClient.GetDatabase(moduleCountryContext.CosmosDbId);
            var containerResponse = await database.CreateContainerIfNotExistsAsync(moduleCountryContext.CosmosDbContainerId, "/partitionKey");

            var sqlQueryText = $"SELECT * FROM c WHERE c.partitionKey = '{partitionKey}'";


            var query = containerResponse.Container.GetItemQueryIterator<TItem>(new QueryDefinition(sqlQueryText));

            List<TItem> results = new List<TItem>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();

                results.AddRange(response.ToList());
            }

            return results;
        }

        public async Task<IEnumerable<TItem>> QueryExistingItemsAsync<TItem>(string sqlQueryText, string appTagName)
        {
            var moduleCountryContextList = await _configuration.DeserialiseForAsync<ModuleCountryContext>("ModuleSettings");
            var moduleCountryContext = moduleCountryContextList.FirstOrDefault(m => m.AppTag == appTagName);

            var database = _cosmosClient.GetDatabase(moduleCountryContext.CosmosDbId);
            var containerResponse = await database.CreateContainerIfNotExistsAsync(moduleCountryContext.CosmosDbContainerId, "/partitionKey");

            var query = containerResponse.Container.GetItemQueryIterator<TItem>(new QueryDefinition(sqlQueryText));

            List<TItem> results = new List<TItem>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();

                results.AddRange(response.ToList());
            }

            return results;
        }

        public async Task<ItemResponse<TItem>> ReplaceItemAsync<TItem>(TItem item, string id, string partitionKey, string appTagName)
        {
            var moduleCountryContextList = await _configuration.DeserialiseForAsync<ModuleCountryContext>("ModuleSettings");
            var moduleCountryContext = moduleCountryContextList.FirstOrDefault(m => m.AppTag == appTagName);

            var database = _cosmosClient.GetDatabase(moduleCountryContext?.CosmosDbId);
            var container = database.GetContainer(moduleCountryContext?.CosmosDbContainerId);

            ItemResponse<TItem> itemResponse = await container.ReadItemAsync<TItem>(id, new PartitionKey(partitionKey));
            var itemBody = itemResponse.Resource;

            itemBody = item;

            itemResponse = await container.ReplaceItemAsync<TItem>(itemBody, id, new PartitionKey(partitionKey));

            return itemResponse;
        }


    }
}
