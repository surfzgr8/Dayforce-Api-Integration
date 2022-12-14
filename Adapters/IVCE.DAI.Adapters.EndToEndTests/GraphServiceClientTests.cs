using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Graph;
using NUnit.Framework;
using Azure.Identity;
using IVCE.DAI.Common.Extensions;
using IVCE.DAI.Domain.Models.Canonical;
using IVCE.DAI.Common.Helpers;
using IVCE.DAI.Adapters.Config;

namespace IVCE.DAI.Adapters.EndToEndTests
{
    public  class GraphServiceClientTests
    {
        private IServiceProvider _serviceProvider;
        private IConfigurationRoot _configuration;
        private IConfigurationRefresher _refresher;
        private IModuleCountryContext _moduleCountryContext;
        //private TelemetryClient telemetryClient;
        //private GraphServiceClient _graphServiceClient;

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

            _moduleCountryContext = moduleSettingsList.FirstOrDefault(m => m.AppTag == "ESP_AAD");

           services.AddSingleton<GraphServiceClient>(sp => {
                return new GraphServiceClient(AquireToken(_moduleCountryContext, azureConfiguration));
            });

            services.AddFeatureManagement();
            services.AddSingleton(new TelemetryClient(new TelemetryConfiguration("7ed730db-942f-4fbb-8d07-97dfdabea1a5")));
            services.AddScoped(c => azureConfiguration);
            services.AddSingleton(_refresher);
            services.AddSingleton(typeof(EventGridAdapter));
            services.AddSingleton(typeof(ADGraphServiceClientAdapter));

            return azureConfiguration;
        }


      
        private TokenCredentialAuthProvider AquireToken(IModuleCountryContext moduleCountryContext,IConfigurationRoot azureConfig)
        {
            var AADTenantId = moduleCountryContext.AADTenantId;// "45d4628b-6f3d-4797-9c26-43b13e44e2ba";
            var AADClientId = moduleCountryContext.AADClientId;// "50c2bb81-86bb-437c-a610-509befbdcb35";
            var AADClientSecret = azureConfig[moduleCountryContext.AADClientSecret];// _ "MvQ8Q~Srq3hW6.jIS2aiKtIazsnl9x0oGfmI3c2T";


            ClientSecretCredential clientSecretCredential
                = new ClientSecretCredential(AADTenantId, AADClientId, AADClientSecret);

           return new TokenCredentialAuthProvider(clientSecretCredential);

        }

        [Test]
        public async Task WhenCallGraphServiceClientUsersAreReturned()
        {
            // string employeeXrefCode = "020720201500";
            string employeeXrefCode = "50100664";// "45010428";
            var _graphServiceClient = _serviceProvider.GetRequiredService<GraphServiceClient>();

            try
            {
                var asyncResult =  _graphServiceClient.Users
                    .Request()
                     .Header("ConsistencyLevel", "eventual")
                        .Filter($"employeeID eq '{employeeXrefCode}'")
                            .Select("id,userPrincipalName,employeeId")
                                .GetAsync().Result;



                Assert.IsTrue(asyncResult.FirstOrDefault().Id !="");
            }
            catch (Exception ex)
            {
            }


         
        }

        [Test]
        public async Task  WhenCallGetAADUserByEmployeeXrefCodeAsyncUsersAreReturned()
        {
            
            var graphServiceClientAdapter = _serviceProvider.GetRequiredService<ADGraphServiceClientAdapter>();

            var graphUser = await graphServiceClientAdapter.GetAADUserByEmployeeXrefCodeAsync("020720201500");
         
            Assert.IsTrue(graphUser.UserPrincipalName.ToString().Length > 0);
         
        }

        [Test]
        public async Task WhenCallGetAADUserByUserPrincipalNameAsyncUsersAreReturned()
        {

            var graphServiceClientAdapter = _serviceProvider.GetRequiredService<ADGraphServiceClientAdapter>();

            var graphUser = await graphServiceClientAdapter.GetAADUserByUserPrincipalNameAsync("raul.valero@41wn2f.onmicrosoft.com");

            Assert.IsTrue(graphUser.EmployeeId.ToString().Length > 0);
        }

        [Test]
        public async Task WhenCallingCreateAADUserAUserIsAddedToAAD()
        {
            var today = DateTimeOffset.UtcNow;

            var graphServiceClientAdapter = _serviceProvider.GetRequiredService<ADGraphServiceClientAdapter>();

           // var graphUser = await graphServiceClientAdapter.GetAADUserByUserPrincipalNameAsync("raul.valero@41wn2f.onmicrosoft.com");

            var canonicalAADUser = new CanonicalWorkerItem
            {
                Header = new Header 
                {
                    AAD_Id="",
                    ApplicationId="ESP_AAD",
                    EmployeeXRefCode="111113444",
                    LogicalDelete=false,
                    OperationDate= DateTimeOffset.UtcNow,
                    OperationStatus="Consumed",
                    Region="ESP"
                },

                WorkerItem=new Item 
                {
                    AccountEnabled=true,
                    BrandCode= "3029-PEÑAGRAND",
                    BrandName= "3029-PEÑAGRAND",
                    BusinessEmailAddress="",
                    BusinessMobileNumber="****",
                    CountryCode="ESP",
                    Department= "Clinical Care Staff",
                    EmployeeEmploymentNumber= "50100669",
                    EmployeeJobTitle= "General Vet",
                    EmploymentStatusCode= "ACTIVE",
                    EmploymentStatusName= "Active",
                    EmploymentStatusReasonCode= "NEWHIRE",
                    FirstName= "Mariano",
                    LastName= "BlancoTwo",
                    HireDate = Convert.ToDateTime( "28/05/2022 00:00:00"),
                    StartDate = Convert.ToDateTime("28/05/2022 00:00:00"),
                    SiteName= "029-3531-CV AF COLABORACIONES VET. - Clinical Care Staff",
                    SiteCode= "3029_3531_CVAFCOLABORACIONESVETERINARIAS_CLINICALCARESTAFF",
                    ISOCountryCode="ES"


                }
            };

            var emailFirstName = EmailHelper.FormatNameParts(canonicalAADUser.WorkerItem.FirstName);
            var emailLastName = EmailHelper.FormatNameParts(canonicalAADUser.WorkerItem.LastName);

            canonicalAADUser.WorkerItem.BusinessEmailAddress
                = (emailFirstName.Length > 0 && emailLastName.Length > 0) ?
                    string.Concat(emailFirstName, '.', emailLastName, '@', _moduleCountryContext.AADHostName)
                        : throw new Exception("Either emailFirstName or emailLastName are null");

            var newUser = await graphServiceClientAdapter.CreateAADUserAsync(canonicalAADUser);

            Assert.IsTrue(newUser.Id.ToString().Length > 0);
        }



        [Test]
        public async Task WhenCallingUpdateAADUserAUserIsDisableddInAAD()
        {
            var today = DateTimeOffset.UtcNow;

            var graphServiceClientAdapter = _serviceProvider.GetRequiredService<ADGraphServiceClientAdapter>();

            // var graphUser = await graphServiceClientAdapter.GetAADUserByUserPrincipalNameAsync("raul.valero@41wn2f.onmicrosoft.com"); var graphUser
            var graphUser = await graphServiceClientAdapter.GetAADUserByEmployeeXrefCodeAsync("50100664");

            graphUser.AccountEnabled = false;

            var res = graphServiceClientAdapter.UpdateAADUserAsync(graphUser);

            
            Assert.IsTrue(res.Id.ToString().Length > 0);
        }
    }
}
