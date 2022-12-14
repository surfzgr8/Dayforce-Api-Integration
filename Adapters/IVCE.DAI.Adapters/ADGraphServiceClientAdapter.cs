using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.Graph;
using IVCE.DAI.Domain.Models.Canonical;
using IVCE.DAI.Common.Helpers;

namespace IVCE.DAI.Adapters
{
    public interface IADGraphServiceClientAdapter
    {
        public User GetAADUserByEmployeeXrefCode(string employeeXrefCode);
        public Task<User> GetAADUserByEmployeeXrefCodeAsync(string employeeXrefCode);
        public Task<User> GetAADUserByUserPrincipalNameAsync(string userPrincipalName);
        public Task<User> CreateAADUserAsync(CanonicalWorkerItem canonicalAADUserItem);
        public Task<User> UpdateAADUserAsync(CanonicalWorkerItem canonicalAADUserItem);
        public Task<User> UpdateAADUserAsync(User updatedUser);
        public bool UpdateAADUser(CanonicalWorkerItem canonicalAADUserItem);
        public Task AssignManagerAsync(CanonicalWorkerItem canonicalAADUserItem, string managerAAD_Id);
    }

    public class ADGraphServiceClientAdapter : IADGraphServiceClientAdapter
    {
        private const string ModuleName = "IVCE.DAI.Adapters.ADGraphServiceClientAdapter";


        private readonly IConfigurationRoot _configuration;
        private readonly TelemetryClient _telemetryClient;
        private readonly GraphServiceClient _graphServiceClient;

        public ADGraphServiceClientAdapter(IConfigurationRoot configuration, GraphServiceClient graphServiceClient, TelemetryClient telemetryClient)
        {
            _configuration = configuration;
            _graphServiceClient = graphServiceClient;
            _telemetryClient = telemetryClient;

        }

        public async Task<User> CreateAADUserAsync(CanonicalWorkerItem canonicalAADUserItem)
        {
            var user = new User
            {
                AccountEnabled = true,// canonicalAADUserItem.WorkerItem.AccountEnabled,
                City = canonicalAADUserItem.WorkerItem.City,
                Country = canonicalAADUserItem.WorkerItem.CountryCode,
                Department = canonicalAADUserItem.WorkerItem.Department,
                DisplayName = string.Concat(canonicalAADUserItem.WorkerItem.FirstName, " ", canonicalAADUserItem.WorkerItem.LastName),
                MailNickname = string.Concat(EmailHelper.FormatNameParts(canonicalAADUserItem.WorkerItem.FirstName), canonicalAADUserItem.WorkerItem.LastName.Substring(0,1)),
                GivenName = canonicalAADUserItem.WorkerItem.FirstName,
                //nb: PreferredName can only be set in a Patch Operation
               // PreferredName = canonicalAADUserItem.WorkerItem.PreferredLastName,
                Surname = canonicalAADUserItem.WorkerItem.LastName,
                JobTitle = canonicalAADUserItem.WorkerItem.EmployeeJobTitle,
                PasswordPolicies = "DisablePasswordExpiration",
                PasswordProfile = new PasswordProfile
                {
                    Password = "P@55W0rd",
                    ForceChangePasswordNextSignIn = true
                },
                OfficeLocation = canonicalAADUserItem.WorkerItem.SiteName,
                //  PostalCode = "98052",
                PreferredLanguage = canonicalAADUserItem.WorkerItem.PreferredLnguage,
                MobilePhone = canonicalAADUserItem.WorkerItem.BusinessMobileNumber,
                UsageLocation = canonicalAADUserItem.WorkerItem.ISOCountryCode,
                UserPrincipalName = canonicalAADUserItem.WorkerItem.BusinessEmailAddress.ToLower(),
                EmployeeId = canonicalAADUserItem.WorkerItem.EmployeeEmploymentNumber,

            };

            _graphServiceClient.Users
                        .Request().MiddlewareOptions.Add(typeof(RetryHandlerOption).ToString(), new RetryHandlerOption() { MaxRetry = 3 });

            //
            return await _graphServiceClient.Users
                    .Request()
                        .AddAsync(user);
        }

        public async Task<User> UpdateAADUserAsync(CanonicalWorkerItem canonicalAADUserItem)
        {
            var user = new User
            {
                Id = canonicalAADUserItem.Header.AAD_Id,
                AccountEnabled = canonicalAADUserItem.WorkerItem.AccountEnabled,
                City = canonicalAADUserItem.WorkerItem.BrandCode,
                Country = canonicalAADUserItem.WorkerItem.CountryCode,
                Department = canonicalAADUserItem.WorkerItem.Department,
                DisplayName = string.Concat(canonicalAADUserItem.WorkerItem.FirstName, " ", canonicalAADUserItem.WorkerItem.LastName),
                MailNickname = string.Concat(canonicalAADUserItem.WorkerItem.FirstName, canonicalAADUserItem.WorkerItem.LastName.Substring(0, 1)),
                GivenName = canonicalAADUserItem.WorkerItem.PreferredLastName,
                JobTitle = canonicalAADUserItem.WorkerItem.EmployeeJobTitle,
                PasswordPolicies = "DisablePasswordExpiration",
                PasswordProfile = new PasswordProfile
                {
                    Password = "P@55W0rd",
                    ForceChangePasswordNextSignIn = true
                },
                OfficeLocation = canonicalAADUserItem.WorkerItem.SiteName,
                PreferredLanguage = canonicalAADUserItem.WorkerItem.PreferredLnguage,
                Surname = canonicalAADUserItem.WorkerItem.LastName,
                MobilePhone = canonicalAADUserItem.WorkerItem.BusinessMobileNumber,
                UsageLocation = canonicalAADUserItem.WorkerItem.CountryCode,
                UserPrincipalName = canonicalAADUserItem.WorkerItem.BusinessEmailAddress,
                EmployeeId = canonicalAADUserItem.WorkerItem.EmployeeEmploymentNumber,

            };

            _graphServiceClient.Users
                  .Request().MiddlewareOptions.Add(typeof(RetryHandlerOption).ToString(), new RetryHandlerOption() { MaxRetry = 3 });

            //
            return await _graphServiceClient.Users[canonicalAADUserItem.Header.AAD_Id]
                    .Request()
                         .UpdateAsync(user);
        }

        public bool UpdateAADUser(CanonicalWorkerItem canonicalAADUserItem)
        {
            var user = new User
            {
                Id = canonicalAADUserItem.Header.AAD_Id,
                AccountEnabled = canonicalAADUserItem.WorkerItem.AccountEnabled,
                City = canonicalAADUserItem.WorkerItem.BrandCode,
                Country = canonicalAADUserItem.WorkerItem.CountryCode,
                Department = canonicalAADUserItem.WorkerItem.Department,
                DisplayName = string.Concat(canonicalAADUserItem.WorkerItem.FirstName, " ", canonicalAADUserItem.WorkerItem.LastName),
                MailNickname = string.Concat(canonicalAADUserItem.WorkerItem.FirstName, canonicalAADUserItem.WorkerItem.LastName.Substring(0, 1)),
                GivenName = canonicalAADUserItem.WorkerItem.PreferredLastName,
                JobTitle = canonicalAADUserItem.WorkerItem.EmployeeJobTitle,
                PasswordPolicies = "DisablePasswordExpiration",
                PasswordProfile = new PasswordProfile
                {
                    Password = "P@55W0rd",
                    ForceChangePasswordNextSignIn = true
                },
                OfficeLocation = canonicalAADUserItem.WorkerItem.SiteName,
                //  PostalCode = "98052",
                PreferredLanguage = canonicalAADUserItem.WorkerItem.PreferredLnguage,
                Surname = canonicalAADUserItem.WorkerItem.LastName,
                MobilePhone = canonicalAADUserItem.WorkerItem.BusinessMobileNumber,
                UsageLocation = canonicalAADUserItem.WorkerItem.CountryCode,
                UserPrincipalName = canonicalAADUserItem.WorkerItem.BusinessEmailAddress,
                EmployeeId = canonicalAADUserItem.WorkerItem.EmployeeEmploymentNumber,

            };

            _graphServiceClient.Users
                  .Request().MiddlewareOptions.Add(typeof(RetryHandlerOption).ToString(), new RetryHandlerOption() { MaxRetry = 3 });

            //
             _graphServiceClient.Users[canonicalAADUserItem.Header.AAD_Id]
                    .Request()
                         .UpdateAsync(user);
            return true;
        }

        public async Task<User> UpdateAADUserAsync(User updatedUser)
        {
     
            _graphServiceClient.Users
                  .Request().MiddlewareOptions.Add(typeof(RetryHandlerOption).ToString(), new RetryHandlerOption() { MaxRetry = 3 });

            //
            return await _graphServiceClient.Users[updatedUser.Id]
                    .Request()
                         .UpdateAsync(updatedUser);
        }
        public async Task<User> GetAADUserByEmployeeXrefCodeAsync(string employeeXrefCode)
        {


            try
            {
                var asyncResult = await _graphServiceClient.Users
                    .Request()
                     .Header("ConsistencyLevel", "eventual")
                        .Filter($"employeeID eq '{employeeXrefCode}'")
                            .Select("id,userPrincipalName,employeeId")
                                .GetAsync();

                return asyncResult?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);
            }

            return null;
        }

        public  User GetAADUserByEmployeeXrefCode(string employeeXrefCode)
        {
 

            try
            {
                var syncResult =  _graphServiceClient.Users
                    .Request()
                     .Header("ConsistencyLevel", "eventual")
                        .Filter($"employeeID eq '{employeeXrefCode}'")
                            .Select("id,userPrincipalName,employeeId")
                                .GetAsync()?.Result;

                return syncResult?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _telemetryClient.TrackException(ex);
            }

            return null;
        }
        public async Task<User> GetAADUserByUserPrincipalNameAsync(string userPrincipalName)
        {

            var asyncResult = await _graphServiceClient.Users
                .Request()
                    .Header("ConsistencyLevel", "eventual")
                        .Filter($"userPrincipalName eq '{userPrincipalName}'")
                            .Select("id,userPrincipalName,employeeId")
                                .GetAsync();

            return asyncResult.FirstOrDefault();
        }

        public async Task AssignManagerAsync(CanonicalWorkerItem canonicalAADUserItem,string managerAAD_Id)
        {
            await _graphServiceClient.Users[canonicalAADUserItem.Header.AAD_Id].Manager.Reference
                    .Request()
                        .PutAsync(managerAAD_Id);
        }
    }
}
