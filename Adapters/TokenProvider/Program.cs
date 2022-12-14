using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TokenProvider
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Here is your Access Token:");
            await SignIn();
            Console.ReadLine();
        }

        private async static Task SignIn()
        {
            var builder = new ConfigurationBuilder()
                 .SetBasePath(Directory.GetCurrentDirectory())
                 .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var configuration = builder.Build();

            var clientId = configuration["AzureAd:ClientId"];
            var AadInstance = configuration["AzureAd:AADInstance"];
            var multilexHostingDesktopApiScope = configuration["AzureAd:DAIClaimsEndpoint"];

            var tenant = configuration["AzureAd:TenantId"];

            var _scopes = new string[] { multilexHostingDesktopApiScope };

            var authority = string.Format(CultureInfo.InvariantCulture, AadInstance, tenant);

            var _app = PublicClientApplicationBuilder.Create(clientId)
                     .WithAuthority(authority)
                     .WithDefaultRedirectUri()
                     .Build();

            var accounts = (await _app.GetAccountsAsync()).ToList();

            try
            {
                // Force a sign-in (PromptBehavior.Always), as the ADAL web browser might contain cookies for the current user, and using .Auto
                // would re-sign-in the same user
                var result = await _app.AcquireTokenInteractive(_scopes)
                    .WithAccount(accounts.FirstOrDefault())
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync()
                    .ConfigureAwait(false);

                Console.WriteLine(result.AccessToken.ToString());
            }
            catch (MsalException ex)
            {
                if (ex.ErrorCode == "access_denied")
                {
                    // The user canceled sign in, take no action.
                }
                else
                {
                    // An unexpected error occurred.
                    string message = ex.Message;
                    if (ex.InnerException != null)
                    {
                        message += "Error Code: " + ex.ErrorCode + "Inner Exception : " + ex.InnerException.Message;
                    }
                }
            }
        }
    }
}
