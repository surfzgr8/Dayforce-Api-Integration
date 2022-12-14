using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Logging;

namespace AzureAppConfigSampleFunction
{
    public class AzureAppConfigSampleFunction
    {
        private readonly IConfiguration _configuration;
        private readonly IConfigurationRefresher _configurationRefresher;

        public AzureAppConfigSampleFunction(IConfiguration configuration, IConfigurationRefresherProvider refresherProvider)
        {
            _configuration = configuration;
            _configurationRefresher = refresherProvider.Refreshers.First();
        }

        // Uncomment the FunctionName attribute below to enable the function
        //[FunctionName("AzureAppConfigSampleFunction")]
        public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            await _configurationRefresher.TryRefreshAsync();

            // Read the key value in App Configuration.
            string key = "TestApp:Settings:Message";
            string message = $"'{key}': '{_configuration[key] ?? "Key not found. Please create the key-value in your Azure App Configuration store."}'";
            return (ActionResult)new OkObjectResult(message);
        }
    }
}


