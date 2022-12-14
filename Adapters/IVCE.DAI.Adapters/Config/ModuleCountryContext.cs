using IVCE.DAI.Common.Helpers;
using System.Text.Json.Serialization;
using IVCE.DAI.Domain.Models.Dayforce;
using System;



namespace IVCE.DAI.Adapters.Config
{
    public interface IModuleCountryContext
    {
        public string AppTag { get; set; }
        public string AssignManagerFeature { get; set; }
        public string CronSchedule { get; set; }
        public string MaxRetryCount { get; set; }
        public string FeatureFlagName { get; set; }
        public string ApiBaseUri { get; set; }
        public string SuffixUri { get; set; }

        public string CountryCode { get; set; }
        public string DayforceGuidCountryQueryParameters { get; set; }
        public string DayforceGuidEffectiveStartDateQueryParameters { get; set; }
        public string DayforceGuidEffectiveEndDateQueryParameters { get; set; }
        public string DayforceDefaultEffectiveStartDate { get; set; }
        public string DayforceEnvironment { get; set; }
        public string DayforceOAuthUri { get; set; }
        public string DayforceOAuthUsername { get; set; }
        public string DayforceClientId { get; set; }
        public string DayforcePwdKV { get; set; }
        public string EffectiveStartDateIntervalDays { get; set; }
        public string EventGridTopicName { get; set; }
        public string EventGridTopicEndpoint { get; set; }
        public string EventGridOperationEventType { get; set; }
        public string EventGridOperationStatus { get; set; }
        public string EventGridOperationRouteTo { get; set; }
        public string EventGridTopicKey { get; set; }
        public string CosmosDbEndpoint { get; set; }
        public string CosmosDbId { get; set; }
        public string CosmosDbContainerId { get; set; }
        public string CosmosDbPrimaryKey { get; set; }
        public DateTimeOffset EventGridOperationDate { get; set; }

        public string AADUsername { get; set; }
        public string AADPassword { get; set; }
        public string AADClientSecret { get; set; }
        public string AADTenantId { get; set; }
        public string AADClientId { get; set; }
        public string AADHostName { get; set; }
        public EmployeeChangesReport EmployeeChangesReport { get; set; }

    }
    public partial class ModuleCountryContext : IModuleCountryContext
    {

        [JsonPropertyName("AppTag")]
        public string AppTag { get; set; }

        [JsonPropertyName("AssignManagerFeature")]
        public string AssignManagerFeature { get; set; }

        [JsonPropertyName("CronSchedule")]
        public string CronSchedule { get; set; }

        [JsonPropertyName("MaxRetryCount")]
        public string MaxRetryCount { get; set; }

        [JsonPropertyName("FeatureFlagName")]
        public string FeatureFlagName { get; set; }

        [JsonPropertyName("ApiBaseUri")]
        public string ApiBaseUri { get; set; }

        [JsonPropertyName("SuffixUri")]
        public string SuffixUri { get; set; }

        [JsonPropertyName("DayforceGuidCountryQueryParameters")]
        public string DayforceGuidCountryQueryParameters { get; set; }

        [JsonPropertyName("DayforceGuidEffectiveStartDateQueryParameters")]
        public string DayforceGuidEffectiveStartDateQueryParameters { get; set; }

        [JsonPropertyName("DayforceGuidEffectiveEndDateQueryParameters")]
        public string DayforceGuidEffectiveEndDateQueryParameters { get; set; }

        [JsonPropertyName("DayforceDefaultEffectiveStartDate")]
        public string DayforceDefaultEffectiveStartDate { get; set; }

        [JsonPropertyName("DayforceEnvironment")]
        public string DayforceEnvironment { get; set; }

        [JsonPropertyName("DayforceOAuthUri")]
        public string DayforceOAuthUri { get; set; }

        [JsonPropertyName("DayforceOAuthUsername")]
        public string DayforceOAuthUsername { get; set; }

        [JsonPropertyName("DayforceClientId")]
        public string DayforceClientId { get; set; }

        [JsonPropertyName("DayforcePwdKV")]
        public string DayforcePwdKV { get; set; }

        [JsonPropertyName("EffectiveStartDateIntervalDays")]
        public string EffectiveStartDateIntervalDays { get; set; }

        [JsonPropertyName("CountryCode")]
        public string CountryCode { get; set; }

        [JsonPropertyName("EventGridTopicName")]
        public string EventGridTopicName { get; set; }

        [JsonPropertyName("EventGridTopicEndpoint")]
        public string EventGridTopicEndpoint { get; set; }

        [JsonPropertyName("EventGridTopicKey")]
        public string EventGridTopicKey { get; set; }

        [JsonPropertyName("CosmosDbEndpoint")]
        public string CosmosDbEndpoint { get; set; }

        [JsonPropertyName("CosmosDbId")]
        public string CosmosDbId { get; set; }

        [JsonPropertyName("CosmosDbContainerId")]
        public string CosmosDbContainerId { get; set; }

        [JsonPropertyName("CosmosDbPrimaryKey")]
        public string CosmosDbPrimaryKey { get; set; }


        [JsonPropertyName("EmployeeChangesReport")]
        public EmployeeChangesReport EmployeeChangesReport { get; set; }

        [JsonPropertyName("EventGridOperationDate")]
        public DateTimeOffset EventGridOperationDate { get; set; }

        [JsonPropertyName("EventGridOperationEventType")]
        public string EventGridOperationEventType { get; set; }

        [JsonPropertyName("EventGridOperationStatus")]
        public string EventGridOperationStatus { get; set; }

        [JsonPropertyName("EventGridOperationRouteTo")]
        public string EventGridOperationRouteTo { get; set; }

        [JsonPropertyName("AADUsername")]
        public string AADUsername { get; set; }

        [JsonPropertyName("AADPassword")]
        public string AADPassword { get; set; }

        [JsonPropertyName("AADClientSecret")]
        public string AADClientSecret { get; set; }

        [JsonPropertyName("AADTenantId")]
        public string AADTenantId { get; set; }

        [JsonPropertyName("AADClientId")]
        public string AADClientId { get; set; }

        [JsonPropertyName("AADHostName")]
        public string AADHostName { get; set; }


    }


}

