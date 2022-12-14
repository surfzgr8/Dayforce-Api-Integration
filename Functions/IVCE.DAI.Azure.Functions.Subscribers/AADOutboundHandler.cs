using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.ApplicationInsights;
using Microsoft.Graph;
using Microsoft.FeatureManagement;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Azure;
using Azure.Messaging.EventGrid;
using Azure.Core.Serialization;
using IVCE.DAI.Adapters.Extensions;
using IVCE.DAI.Adapters;
using IVCE.DAI.Domain.Models.Canonical;
using IVCE.DAI.Domain.Models.Dayforce;
using IVCE.DAI.Common.Helpers;
using IVCE.DAI.Adapters.Config;

namespace IVCE.DAI.Azure.Functions.Subscribers
{
    //
    public class AADOutboundHandler
    {
        private const string ModuleName = "IVCE.DAI.Azure.Functions.Subscribers";

        private readonly IConfigurationRoot _azureConfig;
        private readonly TelemetryClient _telemetryClient;
        private readonly IADGraphServiceClientAdapter _aadGraphServiceClientAdapter;
        private readonly ICosmosDbAdapter _cosmosDbAdapter;
        private readonly IEventGridAdapter _outboundEventGridAdapter;
        private readonly IConfigurationRefresher _configurationRefresher;
        private readonly IFeatureManager _featureManager;
        private IModuleCountryContext _moduleCountryContext;
        private string _eventGridMessage = string.Empty;




        public AADOutboundHandler(IConfigurationRoot azureConfig, TelemetryClient telemetryClient, IADGraphServiceClientAdapter aadGraphServiceClientAdapter,
                ICosmosDbAdapter cosmosDbAdapter, IEventGridAdapter outboundEventGridAdapter, IConfigurationRefresher configurationRefresher, IFeatureManager featureManager)
        {
            _azureConfig = azureConfig
              ?? throw new ArgumentNullException(nameof(azureConfig));

            _aadGraphServiceClientAdapter = aadGraphServiceClientAdapter
                ?? throw new ArgumentNullException(nameof(_aadGraphServiceClientAdapter));

            _cosmosDbAdapter = cosmosDbAdapter
                   ?? throw new ArgumentNullException(nameof(cosmosDbAdapter));

            _outboundEventGridAdapter = outboundEventGridAdapter
                ?? throw new ArgumentNullException(nameof(outboundEventGridAdapter));

            _configurationRefresher = configurationRefresher
                ?? throw new ArgumentNullException(nameof(configurationRefresher));

            _featureManager = featureManager
                ?? throw new ArgumentNullException(nameof(featureManager));

            //telemetryClient is injected automatically by Azure function runtime.
            _telemetryClient = telemetryClient;
        
        }

        /// <summary>
        /// trigger build
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [FunctionName("AADOutboundHandler_Orchestrator")]
        public async Task<bool> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            log = context.CreateReplaySafeLogger(log);

            var appTag = context.GetInput<string>();

            _telemetryClient.TrackEvent($"{ModuleName}:AADOutboundHandler_Orchestrator - Input passed to Orchestrator:{appTag}");


            try
            {

                _moduleCountryContext = await GetModuleCountryContext(appTag);

                // Get all new users who are New Hires Iterate through each user and determine if New hire/Rehire/Termination, then
                // add or disable user
                var success = await context.CallActivityAsync<bool>("AADOutboundHandler_ProcessEmployees", JsonSerializer.Serialize(_moduleCountryContext));

            }
            catch (Exception ex)
            {
                _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_Orchestrator - failed  Context Id:{context.InstanceId}");
                _telemetryClient.TrackException(ex);

            }

            _telemetryClient.TrackEvent($"{ModuleName}:AADOutboundHandler_Orchestrator - completed with InstanceId:{context.InstanceId}");

            return true;
        }



        [FunctionName("AADOutboundHandler_ProcessEmployees")]
        public async Task<bool> AADOutboundHandler_ProcessEmployees([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {
            _telemetryClient.TrackEvent($"{ModuleName}:AADOutboundHandler_ProcessEmployees - started with InstanceId:{context.InstanceId}");

            var input = context.GetInput<string>();

            _telemetryClient.TrackEvent($"{ModuleName}:AADOutboundHandler_ProcessEmployees - Input passed:{input}");

            var moduleCountryContext
                = JsonSerializer.Deserialize<ModuleCountryContext>(input);

            // get all new hires and(potential rehires)  from eventstore with a start date within the next n days set in context settings,
            // take into account past dates if set
            var sql = (string.IsNullOrEmpty(moduleCountryContext.DayforceDefaultEffectiveStartDate.ToString())) ?
                $"select * from c where c.WorkerItem.EmploymentStatusCode in ('ACTIVE') " +
                $" and c.Header.OperationStatus in ('Register')" + //nb could do "and c.Header.OperationStatus in ('Register')""
                $" and DateTimeDiff(\"day\", GetCurrentDateTime(),substring(c.WorkerItem.StartDate, 0, 10) ) <= {moduleCountryContext.EffectiveStartDateIntervalDays} " +
                $" and substring(c.WorkerItem.StartDate,0,10) >= substring(GetCurrentDateTime(),0,10)"
                    :
                $"select * from c where c.WorkerItem.EmploymentStatusCode in ('ACTIVE') " +
                $" and c.Header.OperationStatus in ('Register')" + //nb could do "and c.Header.OperationStatus in ('Register')""
                $" and DateTimeDiff(\"day\", GetCurrentDateTime(),substring(c.WorkerItem.StartDate, 0, 10) ) <= {moduleCountryContext.EffectiveStartDateIntervalDays} " +
                $" and substring(c.WorkerItem.StartDate,0,10) >= '{moduleCountryContext.DayforceDefaultEffectiveStartDate}'" ;

            _telemetryClient.TrackEvent($"{ModuleName}:AADOutboundHandler_ProcessEmployees - Call to CosmosDb with sql:{sql}");

            var newHireUserList
                    = await _cosmosDbAdapter.QueryExistingItemsAsync<CanonicalWorkerItem>(sql, moduleCountryContext.AppTag);

            _telemetryClient.TrackEvent($"{ModuleName}:AADOutboundHandler_ProcessEmployees - Number of Users returned from call to CosmosDb:{newHireUserList?.ToList()?.Count()}");

            // iterate through each employee and create record in AAD
            newHireUserList?.ToList().ForEach(async canonicalAADUserItem =>
            {
                try
                {
                    var success = await this.ProcessNewReHireEmployees(canonicalAADUserItem, moduleCountryContext);
                }
                catch (Exception ex)
                {
                    _telemetryClient.TrackEvent($"{ModuleName}:AADOutboundHandler_Orchestrator:ProcessNewReHireEmployees - failed for AAD User with EmployeeXrefCode:{canonicalAADUserItem.partitionKey}");
                    _telemetryClient.TrackException(ex);
                }
            });


            // get all terminated users where the current date is greater or equal to leave date + one. nb that Terminate status is set in inbound function
            sql = $"select * from c where c.Header.OperationStatus in ('Terminate')" +
                    $" and GetCurrentDateTime() >= datetimeadd(\"day\", 1, substring(c.WorkerItem.LeaveDate, 0, 10))";

            var terminatedUsersList
                    = await _cosmosDbAdapter.QueryExistingItemsAsync<CanonicalWorkerItem>(sql, moduleCountryContext.AppTag);

            // iterate through each employee and create record in AAD
            terminatedUsersList.ToList().ForEach(async canonicalAADUserItem =>
            {
                try
                {

                     await  this.ProcessTerminatedEmployees(canonicalAADUserItem, moduleCountryContext);
                
                }
                catch (Exception ex)
                {
                    _telemetryClient.TrackEvent($"{ModuleName}:AADOutboundHandler_Orchestrator:ProcessTerminatedEmployees - failed for AAD User with EmployeeXrefCode:{canonicalAADUserItem.partitionKey}");
                    _telemetryClient.TrackException(ex);
                }
            });

            _telemetryClient.TrackEvent($"{ModuleName}:AADOutboundHandler_ProcessEmployees - completed with InstanceId:{context.InstanceId}");


           

            return true;


        }

        private async Task<User> ProcessNewReHireEmployees(CanonicalWorkerItem canonicalAADUserItem,IModuleCountryContext moduleCountryContext)
        {
            _telemetryClient.TrackEvent($"{ModuleName}:AADOutboundHandler_ProcessNewHireEmployees - started for Employee Id:{canonicalAADUserItem.partitionKey}");

            try
            {
                // does the user already exist in AD under its EmployeeXrefCode if so then must be a Rehire
                var graphUser
                        = await _aadGraphServiceClientAdapter.GetAADUserByEmployeeXrefCodeAsync(canonicalAADUserItem.partitionKey);

                if (graphUser?.EmployeeId == canonicalAADUserItem.partitionKey)
                {

                    //Enable Account in AAD
                    if (graphUser != null)
                    {
                        graphUser.AccountEnabled = true;
                        var enabledUser = await _aadGraphServiceClientAdapter.UpdateAADUserAsync(graphUser);
                    }

                    // update user in eventstore with userprinciplename from AAD,then quit
                    canonicalAADUserItem.WorkerItem.BusinessEmailAddress = graphUser.UserPrincipalName;
                    canonicalAADUserItem.WorkerItem.AccountEnabled=true;
                    canonicalAADUserItem.Header.OperationStatus = "Registered";
                    canonicalAADUserItem.Header.SaveStatus = "Updated";
                    canonicalAADUserItem.Header.OperationDate = DateTimeOffset.Now;

                    _cosmosDbAdapter.UpsertItemsToContainer<CanonicalWorkerItem>(canonicalAADUserItem, canonicalAADUserItem.id, canonicalAADUserItem.partitionKey, moduleCountryContext);          

                    //TODO: publish Gridevent to trigger update back to DF
                    return graphUser;
                }


                // construct an email Address for user 
                var emailFirstName = EmailHelper.FormatNameParts(canonicalAADUserItem.WorkerItem.FirstName);
                var emailLastName = EmailHelper.FormatNameParts(canonicalAADUserItem.WorkerItem.LastName);

                canonicalAADUserItem.WorkerItem.BusinessEmailAddress
                    = (emailFirstName.Length > 0 && emailLastName.Length > 0) ? 
                        string.Concat(emailFirstName, '.', emailLastName, '@', moduleCountryContext.AADHostName) 
                            : throw new Exception("Either emailFirstName or emailLastName are null");

                // does the users email exist under AD UserPrincipleName then increment surname on email address and persist
                var existingAADUser
                         = await _aadGraphServiceClientAdapter.GetAADUserByUserPrincipalNameAsync(canonicalAADUserItem.WorkerItem.BusinessEmailAddress);

                var isExistingAADUser = false;

                isExistingAADUser = (existingAADUser?.UserPrincipalName == canonicalAADUserItem.WorkerItem.BusinessEmailAddress);

                while (isExistingAADUser)
                {
                    var nextAADUser = EmailHelper.GenerateNextEmailUserSequenceNumber(existingAADUser.UserPrincipalName);

                    existingAADUser
                        = await _aadGraphServiceClientAdapter.GetAADUserByUserPrincipalNameAsync(nextAADUser);

                    isExistingAADUser = (existingAADUser?.UserPrincipalName == nextAADUser) ? true : false;

                    if (!isExistingAADUser)
                        canonicalAADUserItem.WorkerItem.BusinessEmailAddress = nextAADUser;

                }

                // Create new user in AAD,persist users details in Eventstore and trigger EventGrid
                var newUser = await _aadGraphServiceClientAdapter.CreateAADUserAsync(canonicalAADUserItem);

                canonicalAADUserItem.Header.AAD_Id = newUser.Id;
                canonicalAADUserItem.Header.OperationDate = DateTimeOffset.UtcNow;
                canonicalAADUserItem.Header.OperationStatus = "Registered";
                canonicalAADUserItem.Header.SaveStatus = "Updated";
                canonicalAADUserItem.WorkerItem.AccountEnabled = true;

                await _configurationRefresher.RefreshAsync();

                // assign Manager to user in AAD, if this feature is enabled
                if (moduleCountryContext.AssignManagerFeature=="Enabled")             
                    await this.AssignManagerToUser(canonicalAADUserItem);

                // update eventstore
                _cosmosDbAdapter.UpsertItemsToContainer(canonicalAADUserItem, canonicalAADUserItem.id, canonicalAADUserItem.partitionKey, moduleCountryContext);


                // TODO: notification to the updater process to update DF

                _telemetryClient.TrackEvent($"{ModuleName}:AADOutboundHandler_ProcessNewHireEmployees - completed for Employee Id:{canonicalAADUserItem.partitionKey}");

                return (newUser);
            }
            catch (Exception ex)
            {
                canonicalAADUserItem.Header.RetryCount++;

                if (canonicalAADUserItem.Header.RetryCount >= Convert.ToInt16(moduleCountryContext.MaxRetryCount))
                {
                    _telemetryClient.TrackEvent($"{ModuleName}:AADOutboundHandler_ProcessNewHireEmployees - Retrying call to AAD for Employee Id:{canonicalAADUserItem.partitionKey}");

                    canonicalAADUserItem.Header.OperationStatus = "Failed";
                    canonicalAADUserItem.Header.SaveStatus = "Updated";
                    canonicalAADUserItem.Header.OperationDate = DateTimeOffset.UtcNow;
                }

                canonicalAADUserItem.Header.Errors = string.Concat(canonicalAADUserItem.Header.Errors, ":", ex.Message);

                _telemetryClient.TrackException(ex);
            }
            return null;
        }


        private async Task<User> ProcessTerminatedEmployees(CanonicalWorkerItem canonicalAADUserItem,IModuleCountryContext moduleCountryContext)
        {
            _telemetryClient.TrackEvent($"{ModuleName}:AADOutboundHandler_ProcessTerminatedEmployees - started for Employee Id:{canonicalAADUserItem.partitionKey}");

            User disabledUser = null;
           


            // Check user exists in AAD if so disable their account in AAD
            var graphUser
                    = _aadGraphServiceClientAdapter.GetAADUserByEmployeeXrefCode(canonicalAADUserItem.partitionKey);



            if (graphUser != null && !string.IsNullOrEmpty(graphUser.Id))
            {
                graphUser.AccountEnabled = false;
                disabledUser= await _aadGraphServiceClientAdapter.UpdateAADUserAsync(graphUser);

                canonicalAADUserItem.WorkerItem.AccountEnabled = false;
                canonicalAADUserItem.Header.OperationDate = DateTimeOffset.UtcNow;
                canonicalAADUserItem.Header.OperationStatus = "Terminated";
                canonicalAADUserItem.Header.SaveStatus = "Updated";
                canonicalAADUserItem.Header.LogicalDelete = true;
            }

            // update eventstore even if user does not exist in AAD. 
            _cosmosDbAdapter.UpsertItemsToContainer(canonicalAADUserItem, canonicalAADUserItem.id, canonicalAADUserItem.partitionKey, moduleCountryContext);

            _telemetryClient.TrackEvent($"{ModuleName}:AADOutboundHandler_ProcessTerminatedEmployees - completed for Employee Id:{canonicalAADUserItem.partitionKey}");

            return disabledUser;

        }


        private string UpdateEventStore([ActivityTrigger] string name, ILogger log)
        {
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        //[FunctionName("AADOutbound_SubscriberHttpStart")]
        //public async Task<HttpResponseMessage> HttpStart(
        //    [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
        //    [DurableClient] IDurableOrchestrationClient starter,
        //    ILogger log)
        //{
        //    // Function input comes from the request content.
        //    string instanceId = await starter.StartNewAsync("AADOutboundHandlerOrchestrator", null);

        //    log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

        //    return starter.CreateCheckStatusResponse(req, instanceId);
        //}





        private async Task AssignManagerToUser(CanonicalWorkerItem canonicalAADUserItem)
        {

            // get managers AAD Id
            var managerGraphUser
                    = await _aadGraphServiceClientAdapter.GetAADUserByEmployeeXrefCodeAsync(canonicalAADUserItem.WorkerItem.LineManagerEmployeeEmploymentNumber);

            // throw an exception if manager does not exist in AAD
            var managerAADId = string.IsNullOrEmpty(managerGraphUser?.Id) ?
                throw new Exception($"Manager with Employee Xref Number:{canonicalAADUserItem.WorkerItem.LineManagerEmployeeEmploymentNumber} does not exist in downstream AAD for Employee:{canonicalAADUserItem.WorkerItem.EmployeeEmploymentNumber}") : managerGraphUser.Id;

            await _aadGraphServiceClientAdapter.AssignManagerAsync(canonicalAADUserItem, managerAADId);

            return;
        }

        /// <summary>
        /// Used to trigger process manually for testing from Postman, using MasterKey via admin route
        /// </summary>
        /// <param name="req"></param>
        /// <param name="appTag"></param>
        /// <param name="starter"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("AADOutboundHandler_HttpTrigger")]
        public async Task<HttpResponseMessage> AADOutboundHandler_HttpTrigger(
            [HttpTrigger(AuthorizationLevel.Admin, "get", "post", Route = "test/{appTag}")] HttpRequestMessage req,  string appTag,
            [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
    
            _telemetryClient.TrackEvent($"{ModuleName}:AADOutboundHandler_HttpTrigger Activated with Parameter:{appTag}");

            string instanceId = starter.StartNewAsync("AADOutboundHandler_Orchestrator", input: appTag).GetAwaiter().GetResult();

            return await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(req, instanceId);
        }


        [FunctionName("AADOutbound_SubscriberTrigger")]
        public async Task RunTrigger(
            [EventGridTrigger] EventGridEvent eventGridEvent, [DurableClient] IDurableOrchestrationClient client, ILogger log)
        {
            _telemetryClient.TrackEvent($"{ModuleName}:AADOutbound_SubscriberTrigger - Eventgrid message received from Topic:{eventGridEvent.Topic} and Subject:{eventGridEvent.Subject}");

            var appTag = eventGridEvent.Subject;

            string instanceId = await client.StartNewAsync("AADOutboundHandler_Orchestrator", input: appTag);

            // Function input comes from the request content.
            //string instanceId = await client.StartNewAsync("AADOutboundHandler_Orchestrator", input: appTag);

            _telemetryClient.TrackEvent($"{ModuleName}:AADOutbound_SubscriberTrigger - Eventgrid Trigger completed for Event with Topic:{eventGridEvent.Topic} and Subject:{eventGridEvent.Subject}");
        }

        private async Task<IModuleCountryContext> GetModuleCountryContext(string appTag)
        {

            var moduleCountryContextList = await _azureConfig.DeserialiseForAsync<ModuleCountryContext>("ModuleSettings");

            return moduleCountryContextList.FirstOrDefault(m => m.AppTag == appTag);
        }

    }
}
