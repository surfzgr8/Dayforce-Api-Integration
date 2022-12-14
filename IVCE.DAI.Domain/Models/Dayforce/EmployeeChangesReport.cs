namespace IVCE.DAI.Domain.Models.Dayforce
{
    using System;
    using System.Text.Json.Serialization;
  
    public partial class EmployeeChangesReport
    {
        [JsonPropertyName("Data")] 
        public Data Data { get; set; }

        [JsonPropertyName("Paging")]
        public Paging Paging { get; set; }
    }

    public partial class Data
    {
        [JsonPropertyName("XRefCode")]
        public string XRefCode { get; set; }

        [JsonPropertyName("Rows")]
        public Row[] Rows { get; set; }
    }

    /// <summary>
    /// Ignore Id fields
    /// </summary>
    public partial class Row
    {
        [JsonPropertyName("Employee_XRefCode")]
        public string Employee_XRefCode { get; set; }

        [JsonPropertyName("Employee_FirstName")]
        public string Employee_FirstName { get; set; }

        [JsonPropertyName("Employee_LastName")]
        public string Employee_LastName { get; set; }

        [JsonPropertyName("Employee_SecondLastName")]
        public string Employee_SecondLastName { get; set; }


        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Employee_CommonName")]
        public string Employee_CommonName { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Employee_PreferredLastName")]
        public string Employee_PreferredLastName { get; set; }

#nullable enable
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Employee_HireDate")]
        public DateTime? Employee_HireDate { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Employee_OriginalHireDate")]
        public DateTime? Employee_OriginalHireDate { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Employee_StartDate")]
        public DateTime? Employee_StartDate { get; set; }

        // Location/Site Ref Code
        [JsonPropertyName("OrgUnit_XRefCode")]
        public string? OrgUnit_XRefCode { get; set; }


        // Location/SiteName
        [JsonPropertyName("OrgUnit_ShortName")]
        public string? OrgUnit_ShortName { get; set; }

        // Department
        [JsonPropertyName("Department_ShortName")]
        public string? Department_ShortName { get; set; }


        [JsonPropertyName("EmploymentStatus_XrefCode")]
        public string? EmploymentStatus_XrefCode { get; set; }


        [JsonPropertyName("EmploymentStatus_ShortName")]
        public string? EmploymentStatus_ShortName { get; set; }

        [JsonPropertyName("EmploymentStatus_LongName")]
        public string? EmploymentStatus_LongName { get; set; }

        [JsonPropertyName("EmploymentStatusReason_XRefCode")]
        public string? EmploymentStatusReason_XRefCode { get; set; }

        // Job Name
        [JsonPropertyName("Job_ShortName")]
        public string? Job_ShortName { get; set; }

        //Location Ledger Code
        [JsonPropertyName("OrgUnit_LedgerCode")]
        public string? OrgUnit_LedgerCode { get; set; }

        [JsonPropertyName("EmployeeManager_ManagerEmployeeNumber")]
        public string? EmployeeManager_ManagerEmployeeNumber { get; set; }

        [JsonPropertyName("EmployeeManager_ManagerDisplayName")]
        public string? EmployeeManager_ManagerDisplayName { get; set; }

        [JsonPropertyName("EmployeeManager_ManagerElectronicAddress")]
        public string? EmployeeManager_ManagerElectronicAddress { get; set; }


        // Site Name
        [JsonPropertyName("LegalEntity_ShortName")]
        public string? LegalEntity_ShortName { get; set; }

        // Site Code
        [JsonPropertyName("LegalEntity_XRefCode")]
        public string? LegalEntity_XRefCode { get; set; }

   
        [JsonPropertyName("GeoCountry_CountryCode")]
        public string? GeoCountry_CountryCode { get; set; }

        [JsonPropertyName("GeoCountry_ISO31662Code")]
        public string? GeoCountry_ISO31662Code { get; set; }

        [JsonPropertyName("Brand")]
        public string? Brand { get; set; }

        // Brand Name
        [JsonPropertyName("DenormOrgUnit_Field1002")]
        public string? DenormOrgUnit_Field1002 { get; set; }


        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("Employee_TerminationDate")]
        public DateTimeOffset? Employee_TerminationDate { get; set; }

        // Brand Code
        [JsonPropertyName("OrgUnitCopy1_XRefCode")]
        public string? OrgUnitCopy1_XRefCode { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonPropertyName("DenormEmployeeContact_BusinessEmail")]
        public string? DenormEmployeeContact_BusinessEmail { get; set; }


        [JsonPropertyName("DenormEmployeeContact_BusinessPhone")]
        public string? DenormEmployeeContact_BusinessPhone { get; set; }
        [JsonPropertyName("DenormEmployeeContact_MobilePhone")]
        public string? DenormEmployeeContact_MobilePhone { get; set; }

        [JsonPropertyName("DenormEmployeeContact_PersonalEmail")]
        public string? DenormEmployeeContact_PersonalEmail { get; set; }


#nullable disable

    }

    public partial class Paging
    {
        [JsonPropertyName("Next")]
        public string Next { get; set; }
    }

}






