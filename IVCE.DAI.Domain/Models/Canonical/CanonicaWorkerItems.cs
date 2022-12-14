namespace IVCE.DAI.Domain.Models.Canonical
{

    using System;
    using System.Text.Json.Serialization;

    public partial class CanonicalWorkerItem
    {
        [JsonPropertyName("id")]
        public string id { get; set; }

        [JsonPropertyName("partitionKey")]
        public string partitionKey { get; set; }

        [JsonPropertyName("Header")]
        public Header Header { get; set; }

        [JsonPropertyName("WorkerItem")]
        public Item WorkerItem { get; set; }
    }

    public partial class Header
    {
        [JsonPropertyName("AAD_Id")]
        public string AAD_Id { get; set; }

        [JsonPropertyName("OperationDate")]
        public DateTimeOffset OperationDate { get; set; }

        /// <summary>
        /// Register/Registered/Terminate/Terminated
        /// </summary>
        [JsonPropertyName("OperationStatus")]
        public string OperationStatus { get; set; }


        //Allocate/Allocated
        [JsonPropertyName("DayforceStatus")]
        public string DayforceStatus { get; set; }
        /// <summary>
        /// Insert/Inserted/Update/Updated/Delete/Deleted
        /// </summary>
        [JsonPropertyName("SaveStatus")]
        public string SaveStatus { get; set; }

        [JsonPropertyName("Region")]
        public string Region { get; set; }

        /// <summary>
        /// Identifes Client ie IBERIA_AAD
        /// </summary>
        [JsonPropertyName("ApplicationId")]
        public string ApplicationId { get; set; }

        [JsonPropertyName("EmployeeXRefCode")]
        public string EmployeeXRefCode { get; set; }


        /// <summary>
        /// Set when removed from Client AAD
        /// </summary>
        [JsonPropertyName("LogicalDelete")]
        public bool LogicalDelete { get; set; }

        [JsonPropertyName("RetryCount")]
        public int RetryCount { get; set; }

        [JsonPropertyName("Errors")]
        public string Errors { get; set; }

    }

    public partial class Item
    {
        [JsonPropertyName("AccountEnabled")]
        public bool AccountEnabled { get; set; }

        [JsonPropertyName("EmployeeEmploymentNumber")]
        public string EmployeeEmploymentNumber { get; set; }

        [JsonPropertyName("CountryCode")]
        public string CountryCode { get; set; }

        [JsonPropertyName("ISOCountryCode")]
        public string ISOCountryCode { get; set; }

        [JsonPropertyName("JobTitle")]
        public string JobTitle { get; set; }

        [JsonPropertyName("PreferredLnguage")]
        public string PreferredLnguage { get; set; }

        [JsonPropertyName("FirstName")]
        public string FirstName { get; set; }

        [JsonPropertyName("LastName")]
        public string LastName { get; set; }

        [JsonPropertyName("PreferredLastName")]
        public string PreferredLastName { get; set; }

        [JsonPropertyName("BusinessEmailAddress")]
        public string BusinessEmailAddress { get; set; }

        [JsonPropertyName("SecondaryEmailAddress")]
        public string SecondaryEmailAddress { get; set; }

        [JsonPropertyName("BrandName")]
        public string BrandName { get; set; }

        [JsonPropertyName("BrandCode")]
        public string BrandCode { get; set; }

        [JsonPropertyName("SiteName")]
        public string SiteName { get; set; }

        [JsonPropertyName("City")]
        public string City { get; set; }

        [JsonPropertyName("SiteCode")]
        public string SiteCode { get; set; }

        [JsonPropertyName("SiteCostCode")]
        public string SiteCostCode { get; set; }

        [JsonPropertyName("SecondarySites")]
        public string SecondarySites { get; set; }

        [JsonPropertyName("EmployeeJobTitle")]
        public string EmployeeJobTitle { get; set; }

        [JsonPropertyName("BusinessMobileNumber")]
        public string BusinessMobileNumber { get; set; }

        [JsonPropertyName("BusinessPhoneNumber")]
        public string BusinessPhoneNumber { get; set; }

        [JsonPropertyName("LineManagerName")]
        public string LineManagerName { get; set; }

        [JsonPropertyName("LineManagerEmail")]
        public string LineManagerEmail { get; set; }

        [JsonPropertyName("LineManagerEmployeeEmploymentNumber")]
        public string LineManagerEmployeeEmploymentNumber { get; set; }

#nullable enable
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("HireDate")]
        public DateTimeOffset? HireDate { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("StartDate")]

        public DateTime? StartDate { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("LeaveDate")]
        public DateTimeOffset? LeaveDate { get; set; }
#nullable disable
        [JsonPropertyName("EmploymentStatusReason")]
        public string EmploymentStatusReason { get; set; }
        [JsonPropertyName("EmploymentStatusReasonCode")]
        public string EmploymentStatusReasonCode { get; set; }

        [JsonPropertyName("EmploymentStatusName")]
        public string EmploymentStatusName { get; set; }

        [JsonPropertyName("EmploymentStatusCode")]
        public string EmploymentStatusCode { get; set; }

        [JsonPropertyName("Department")]
        public string Department { get; set; }

        [JsonPropertyName("DepartmentCode")]
        public string DepartmentCode { get; set; }


    }


}






