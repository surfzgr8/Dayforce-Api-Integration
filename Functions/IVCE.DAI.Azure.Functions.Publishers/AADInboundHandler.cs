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
using Azure.Messaging.EventGrid;
using Azure.Core.Serialization;
using IVCE.DAI.Adapters.Extensions;
using IVCE.DAI.Adapters;
using IVCE.DAI.Domain.Models.Canonical;
using IVCE.DAI.Domain.Models.Dayforce;
using System.Globalization;
using IVCE.DAI.Adapters.Config;

namespace IVCE.DAI.Azure.Functions.Publishers
{
    //Build/deploy 1608
    public class AADInboundHandler
    {
        private const string ModuleName = "IVCE.DAI.Azure.Functions.Publishers";

        private readonly IConfigurationRoot _azureConfig;
        private readonly TelemetryClient _telemetryClient;
        private readonly IDayforceHttpAdapter _inboundHttpAdapter;
        private readonly ICosmosDbAdapter _cosmosDbAdapter;
        private readonly IEventGridAdapter _inboundEventGridAdapter;
        private readonly IConfigurationRefresher _configurationRefresher;

        private string _appTagName = string.Empty;


        public AADInboundHandler(IConfigurationRoot azureConfig, TelemetryClient telemetryClient, IDayforceHttpAdapter inboundHttpAdapter,
            ICosmosDbAdapter cosmosDbAdapter, IEventGridAdapter inboundEventGridAdapter, IConfigurationRefresher configurationRefresher)
        {
            _azureConfig = azureConfig
              ?? throw new ArgumentNullException(nameof(azureConfig));

            _inboundHttpAdapter = inboundHttpAdapter
                ?? throw new ArgumentNullException(nameof(inboundHttpAdapter));

            _cosmosDbAdapter = cosmosDbAdapter
                   ?? throw new ArgumentNullException(nameof(cosmosDbAdapter));

            _inboundEventGridAdapter = inboundEventGridAdapter
                ?? throw new ArgumentNullException(nameof(inboundEventGridAdapter));

            _configurationRefresher = configurationRefresher
                ?? throw new ArgumentNullException(nameof(configurationRefresher));

            //telemetryClient is injected automatically by Azure function runtime.
            _telemetryClient = telemetryClient;
        
        }



        [FunctionName("AADInboundHandler_Orchestrator")]
        public async Task<bool> RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {

            log = context.CreateReplaySafeLogger(log);

            _telemetryClient.TrackEvent
                            ($"{ModuleName}:AADInboundHandler_Orchestrator - started with InstanceId:{context.InstanceId}");
            try
            {
                var appTag = context.GetInput<string>();

                _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_Orchestrator - Input passed to AADInboundHandler_Orchestrator is:{context.GetInput<string>()}");

                var moduleCountryContext = await GetModuleCountryContext(appTag);

                var employeeChangesReport = await context.CallActivityAsync<EmployeeChangesReport>("AADInboundHandler_HttpHandlerActivity", JsonSerializer.Serialize(moduleCountryContext));

                _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_HttpHandlerActivity - completed with:{employeeChangesReport?.Data?.Rows?.Count()} rows returned");

                moduleCountryContext.EmployeeChangesReport = employeeChangesReport;

                var canonicalAADUsersList = employeeChangesReport?.Data?.Rows?.Count() > 0
                    ? await context.CallActivityAsync<IEnumerable<CanonicalWorkerItem>>("AADInboundHandler_EnricherActvity", JsonSerializer.Serialize((moduleCountryContext))) : null;


                _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_EnricherActvity - completed with:{canonicalAADUsersList?.Count()} Employees Saved to EventStore");

                var eventGridResponse = canonicalAADUsersList != null
                    ? await context.CallActivityAsync<string>("AADInboundHandler_PublishEventGridActivity", JsonSerializer.Serialize(moduleCountryContext)) : null;

                var eventMessage = eventGridResponse != null
                    ? $"{ModuleName}:AADInboundHandler_PublishEventGridActivity completed with:{canonicalAADUsersList?.Count()} Employees Published to EventGridTopic:{moduleCountryContext.EventGridTopicName}"
                        : $"{ModuleName}:AADInboundHandler_PublishEventGridActivity completed with no rows returned from Dayforce Api";

                _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_PublishEventGridActivity completed");// with:{canonicalAADUsersList?.Count()} Employees Published to EventGridTopic:{moduleCountryContext.EventGridTopicName}");


            }
            catch (Exception ex) when (ex.GetType().Name.Contains("FunctionFailedException"))
            {
                var errorMessage = $"{ModuleName}:AADInboundHandlerOrchestrator \n  Type:{ex.GetType().Name}\n  Message:{ex.Message}";

                var exNew = new Exception($"{ModuleName}:AADInboundHandler_Orchestrator \n  Type:{ex.GetType().Name}\n  Message:{ex.Message}");

                _telemetryClient.TrackEvent(errorMessage);

                _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_Orchestrator - completed with InstanceId:{context.InstanceId}");

                return true;
            }
            catch (Exception ex) when (ex.GetType().Name.Contains("InvalidOperationException"))
            {
                var errorMessage = $"{ModuleName}:AADInboundHandler_Orchestrator \n  Type:{ex.GetType().Name}\n  Message:{ex.Message}";

                var exNew = new Exception(errorMessage);

                _telemetryClient.TrackEvent(errorMessage);


                _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_Orchestrator - completed with InstanceId:{context.InstanceId}");

                return true;
            }
            catch (Exception ex) when (!ex.GetType().Name.Contains("FunctionFailedException"))
            {
                var errorMessage = $"{ModuleName}:AADInboundHandler_Orchestrator \n  Type:{ex.GetType().Name}\n  Message:{ex.Message}";

                var exNew = new Exception(errorMessage);
                log.LogError(errorMessage);

                _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_Orchestrator - completed with errors:\n{errorMessage}");
                _telemetryClient.TrackException(exNew);

                return true;
            }

            _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_Orchestrator - completed with InstanceId:{context.InstanceId}");

            return true;
        }

        [FunctionName("AADInboundHandler_HttpHandlerActivity")]
        public async Task<EmployeeChangesReport> AADInboundHandler_HttpHandlerActivity([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {

            var input = context.GetInput<string>();

            var moduleCountryContext
                = JsonSerializer.Deserialize<ModuleCountryContext>(input);

            _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_HttpHandlerActivity - started for module:{moduleCountryContext.AppTag}");

            return await _inboundHttpAdapter.ReceiveAsync<EmployeeChangesReport>(moduleCountryContext);

        }

        [FunctionName("AADInboundHandler_EnricherActvity")]
        public async Task<IEnumerable<CanonicalWorkerItem>> AADInboundHandler_EnricherActvity([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {

            var input = context.GetInput<string>();

            var moduleCountryContext
                = JsonSerializer.Deserialize<ModuleCountryContext>(input);

            _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_EnricherActvity - started for module:{moduleCountryContext.AppTag}");

            var employeeChangesReport = moduleCountryContext.EmployeeChangesReport;

            var canonicalAADUserItemList = new List<CanonicalWorkerItem>();
            try
            {
                employeeChangesReport.Data.Rows.ToList().ForEach(e =>
               {

                   canonicalAADUserItemList.Add(new CanonicalWorkerItem
                   {
                       partitionKey = e.Employee_XRefCode,
                       id = e.Employee_XRefCode,

                       Header = new Header
                       {
                           //AAD_Id="",
                           ApplicationId = moduleCountryContext.AppTag,
                           EmployeeXRefCode = e.Employee_XRefCode,
                           LogicalDelete = false, // maps to accountEnabled in AAD
                           OperationDate = DateTimeOffset.UtcNow,
                           //OperationStatus = "Consumed", // Consumed/Registered/Terminated
                           Region = e.GeoCountry_CountryCode,
                           //SaveStatus = "Create" // Create/Update/Delete(Logical)

                       },
                       WorkerItem = new Item
                       {
                          
                           // AccountEnabled = Let subscriber function set this as used in logic to determine current status,
                           BrandCode = e.OrgUnitCopy1_XRefCode,
                           BrandName = e.DenormOrgUnit_Field1002,
                           BusinessMobileNumber = e.DenormEmployeeContact_MobilePhone,
                           BusinessPhoneNumber = e.DenormEmployeeContact_BusinessPhone,
                           BusinessEmailAddress = e.DenormEmployeeContact_BusinessEmail,
                           CountryCode = e.GeoCountry_CountryCode,
                           ISOCountryCode = e.GeoCountry_ISO31662Code,
                           Department = e.Department_ShortName,
                           EmployeeEmploymentNumber = e.Employee_XRefCode,
                           EmployeeJobTitle = e.Job_ShortName,
                           PreferredLastName = e.Employee_CommonName,
                           EmploymentStatusName = e.EmploymentStatus_ShortName,
                           //    EmploymentStatusReason = e.EmploymentStatusReason_LongName,
                           FirstName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(e.Employee_FirstName.ToLower()),
                           HireDate = e.Employee_StartDate,
                           JobTitle = e.Job_ShortName,
                           LastName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(e.Employee_LastName.ToLower()),
                           LeaveDate = e.Employee_TerminationDate,
                           LineManagerEmail = e.EmployeeManager_ManagerElectronicAddress,
                           LineManagerEmployeeEmploymentNumber = e.EmployeeManager_ManagerEmployeeNumber,
                           LineManagerName = e.EmployeeManager_ManagerDisplayName,
                           SecondarySites = null,
                           SiteCode = e.OrgUnit_XRefCode,
                           SiteCostCode = e.OrgUnit_LedgerCode,
                           SiteName = e.OrgUnit_ShortName,
                           StartDate = e.Employee_StartDate,
                           EmploymentStatusReasonCode = e.EmploymentStatusReason_XRefCode,
                           EmploymentStatusCode = e.EmploymentStatus_XrefCode,
                           City=e.Brand
                           
                       }

                   });

                   _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_EnricherActvity - Canonical Employee: ({ canonicalAADUserItemList.LastOrDefault().WorkerItem.EmployeeEmploymentNumber}) Enriched");

                   //  Only upsert if user is not already registered and/or terminated user, and this is not an Update(TODO: implement update logic. Will be based on DF status ie Active and already have a Login)

                   // is this a new hire that has not been through the system  and registered in AAD, then get all of the existing users so we dont update again
                   var sql = $"SELECT * FROM c WHERE c.Header.OperationStatus in ('Register','Registered','Failed','Terminate','Terminated')  and c.id='{ canonicalAADUserItemList.LastOrDefault().id}'";

                   var existingEmployeesList =
                            _cosmosDbAdapter.QueryExistingItemsAsync<CanonicalWorkerItem>
                                (sql, moduleCountryContext.AppTag).Result;


                   // is this a New Hire that has never been process byt this system and does not exist in the eventstore
                   if (!existingEmployeesList.Any() && canonicalAADUserItemList.LastOrDefault().WorkerItem?.EmploymentStatusReasonCode=="NEWHIRE")
                   {
                       canonicalAADUserItemList.LastOrDefault().Header.OperationStatus = "Register";
                       canonicalAADUserItemList.LastOrDefault().Header.SaveStatus = "Inserted";
                   }

                   // is this a ReHire that has never been process by this system and does not exist in the eventstore
                   if (!existingEmployeesList.Any() && canonicalAADUserItemList.LastOrDefault().WorkerItem?.EmploymentStatusReasonCode == "REHIRE")
                   {
                       canonicalAADUserItemList.LastOrDefault().Header.OperationStatus = "Register";
                       canonicalAADUserItemList.LastOrDefault().Header.SaveStatus = "Inserted";
                   }

                   // is this a Terminated user who has not been processed previously, therefore does not exist in the eventstore
                   if (!existingEmployeesList.Any() 
                        && canonicalAADUserItemList.LastOrDefault().WorkerItem?.EmploymentStatusCode == "TERMINATED"
                            && !string.IsNullOrEmpty( canonicalAADUserItemList.LastOrDefault().WorkerItem?.LeaveDate.ToString()))
                   {
                       canonicalAADUserItemList.LastOrDefault().Header.OperationStatus = "Terminate";
                       canonicalAADUserItemList.LastOrDefault().Header.SaveStatus = "Update";
                   }

                   //TODO: add logic for existing users that are already in AD, but have not been processed by this system yet. ie not in the EventStore

                   DateTimeOffset leaveDate = DateTimeOffset.MaxValue;

                   //  Get leave date if this is a leaver
                   if (existingEmployeesList.Any() == true)
                   {
                       if (!string.IsNullOrEmpty(canonicalAADUserItemList.LastOrDefault().WorkerItem?.LeaveDate.ToString()))
                       {
          
                           leaveDate = (DateTimeOffset)canonicalAADUserItemList.LastOrDefault().WorkerItem.LeaveDate;
                       }
                   }

                   // is this a leaver and current date is greater then leave date and they are not already terminated
                   // NB the outbound subscriber process will set these users to Terminated a day later
                   if (existingEmployeesList.Any() && leaveDate != DateTimeOffset.MaxValue 
                        && existingEmployeesList.LastOrDefault().Header.OperationStatus !="Terminated"
                           && canonicalAADUserItemList.LastOrDefault().WorkerItem.EmploymentStatusCode == "TERMINATED")
                   {
                       canonicalAADUserItemList.LastOrDefault().Header.OperationStatus = "Terminate";
                       canonicalAADUserItemList.LastOrDefault().Header.SaveStatus = "Update";
                       canonicalAADUserItemList.LastOrDefault().Header.AAD_Id = existingEmployeesList?.LastOrDefault()?.Header?.AAD_Id;
                   }
                   // Upsert OperationStatus for existing users ie for "Registered/Failed/Terminated"
                   else if (existingEmployeesList.Any())
                   {
                       // logic for handling existing users that include ReHires
                       if (existingEmployeesList?.LastOrDefault()?.Header?.OperationStatus == "Terminated" 
                            && existingEmployeesList?.LastOrDefault()?.WorkerItem.AccountEnabled==false
                            && canonicalAADUserItemList.LastOrDefault().WorkerItem.EmploymentStatusCode== "ACTIVE"
                            && canonicalAADUserItemList.LastOrDefault().WorkerItem?.EmploymentStatusReasonCode == "REHIRE")
                       {
                           // then re-register in AAD. Subscribing function will pick this up
                           canonicalAADUserItemList.LastOrDefault().Header.AAD_Id = existingEmployeesList?.LastOrDefault()?.Header?.AAD_Id;
                           canonicalAADUserItemList.LastOrDefault().Header.OperationStatus = "Register";
                           canonicalAADUserItemList.LastOrDefault().Header.SaveStatus = "Update";
                       }

                       // logic for handling existing users that do not include ReHires
                       if (existingEmployeesList?.LastOrDefault()?.Header?.OperationStatus == "Registered"
                            || existingEmployeesList?.LastOrDefault()?.Header?.OperationStatus == "Register"
                                || existingEmployeesList?.LastOrDefault()?.Header?.OperationStatus == "Failed"
                                    // Or logic for handling current terminated users that are not ReHires
                                    || (existingEmployeesList?.LastOrDefault()?.Header?.OperationStatus == "Terminated"
                                        &&  existingEmployeesList?.LastOrDefault()?.WorkerItem.AccountEnabled == false
                                        && canonicalAADUserItemList.LastOrDefault().WorkerItem.EmploymentStatusCode != "ACTIVE"))
                       {
                           canonicalAADUserItemList.LastOrDefault().Header.AAD_Id = existingEmployeesList?.LastOrDefault()?.Header?.AAD_Id;
                           canonicalAADUserItemList.LastOrDefault().Header.OperationStatus = existingEmployeesList?.LastOrDefault()?.Header?.OperationStatus;
                           canonicalAADUserItemList.LastOrDefault().WorkerItem.AccountEnabled = existingEmployeesList.LastOrDefault().WorkerItem.AccountEnabled;
                           canonicalAADUserItemList.LastOrDefault().WorkerItem.BusinessEmailAddress = existingEmployeesList.LastOrDefault().WorkerItem.BusinessEmailAddress;
                           canonicalAADUserItemList.LastOrDefault().Header.SaveStatus = "Updated";
                       }
                   }
                   // Now Upsert EventStore
                   _cosmosDbAdapter.UpsertItemsToContainer(canonicalAADUserItemList?.LastOrDefault(),
                           canonicalAADUserItemList.LastOrDefault().WorkerItem.EmployeeEmploymentNumber, canonicalAADUserItemList.LastOrDefault().partitionKey, moduleCountryContext);

                   _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_EnricherActvity - Employee: ({canonicalAADUserItemList.LastOrDefault().WorkerItem.EmployeeEmploymentNumber}) Saved to EventStore");
               });
            }
            catch (Exception ex)
            {

                _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_EnricherActvity - Failed for Employee XrefCode: ({canonicalAADUserItemList?.LastOrDefault().id}) CanonicalAADUserItems");

                _telemetryClient.TrackException(ex);

            }

            _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_EnricherActvity - completed for module:{moduleCountryContext.AppTag}");

            return await Task.Run(() => canonicalAADUserItemList);

        }

        [FunctionName("AADInboundHandler_PublishEventGridActivity")]
        public async Task<string> AADInboundHandler_PublishEventGridActivity([ActivityTrigger] IDurableActivityContext context, ILogger log)
        {

            var input = context.GetInput<string>();

            var moduleCountryContext
                = JsonSerializer.Deserialize<ModuleCountryContext>(input);

            moduleCountryContext.EventGridOperationDate = DateTimeOffset.Now;
            moduleCountryContext.EventGridOperationEventType = $"{moduleCountryContext.EventGridOperationEventType}";
            moduleCountryContext.EventGridOperationRouteTo = $"{moduleCountryContext.EventGridOperationRouteTo}";
            moduleCountryContext.EventGridOperationStatus = "Received";

            _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_PublishEventGridActivity - started for module:{moduleCountryContext.AppTag}");

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
                            subject:$"{moduleCountryContext.AppTag}",
                            eventType: $"{moduleCountryContext?.EventGridOperationEventType}",
                            "1.0",
                            // EventGridEvent with custom model serialized to JSON using a custom serializer
                            eventGridCustomDataSerializer.Serialize
                            (
                                // create new moduleCountryContext as we dont want to send complete payload over EventGrid
                                new ModuleCountryContext
                                {
                                    EventGridOperationDate=moduleCountryContext.EventGridOperationDate,
                                    EventGridOperationEventType=moduleCountryContext.EventGridOperationEventType,
                                    EventGridOperationRouteTo=moduleCountryContext.EventGridOperationRouteTo,
                                    EventGridOperationStatus=moduleCountryContext.EventGridOperationStatus
                                }
                            )
                        )
                };


            var response = await _inboundEventGridAdapter.PublishEventListAsync(eventsList, moduleCountryContext);

            return response;
        }


        /// <summary>
        /// Triggers every 30 mins on the hour and half past the hour
        /// </summary>
        /// <param name="myStartTimer"></param>
        /// <param name="orchestrationClient"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("AutoStartTrigger")]
        public async Task Run([TimerTrigger("%CronSchedule%", RunOnStartup = false, UseMonitor = false)] TimerInfo myStartTimer,
          [DurableClient] IDurableClient orchestrationClient, ILogger log)
        {
            // get the time
            var currentTime = DateTimeOffset.UtcNow.ToString("HH:mm");

            _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_AutoStartTrigger - Activated at:{currentTime}");

            var timerSchedules = await _azureConfig.DeserialiseForAsync<TimerSchedule>("TimerSchedules");
            var timerSchedule = timerSchedules.FirstOrDefault(T => T.ScheduledTime == currentTime);

            if (timerSchedule != null)
            {
                _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_AutoStartTrigger - Called  AADInboundHandler_Orchestrator for Application:{ timerSchedule.ScheduledApplication}");
                string instanceId = orchestrationClient.StartNewAsync("AADInboundHandler_Orchestrator", input: timerSchedule.ScheduledApplication).GetAwaiter().GetResult();
            }

            _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_AutoStartTrigger - Completed at:{currentTime}");
        }

        /// <summary>
        /// Used to trigger process manually for testing from Postman, using MasterKey via admin route
        /// </summary>
        /// <param name="req"></param>
        /// <param name="appTag"></param>
        /// <param name="starter"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        [FunctionName("AADInboundHandler_HttpTrigger")]
        public async Task<HttpResponseMessage> AADInboundHandler_HttpTrigger(
            [HttpTrigger(AuthorizationLevel.Admin, "get", "post", Route = "test/{appTag}")] HttpRequestMessage req, string appTag,
            [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {

            _appTagName = appTag;

            _telemetryClient.TrackEvent($"{ModuleName}:AADInboundHandler_HttpTrigger - Activated with Parameter:{appTag}");

            string instanceId = starter.StartNewAsync("AADInboundHandler_Orchestrator", input: appTag).GetAwaiter().GetResult();

            return await starter.WaitForCompletionOrCreateCheckStatusResponseAsync(req, instanceId);
        }



        private async Task<IModuleCountryContext> GetModuleCountryContext(string appTag)
        {

            var moduleCountryContextList = await _azureConfig.DeserialiseForAsync<ModuleCountryContext>("ModuleSettings");

            return moduleCountryContextList.FirstOrDefault(m => m.AppTag == appTag);
        }

    }
}