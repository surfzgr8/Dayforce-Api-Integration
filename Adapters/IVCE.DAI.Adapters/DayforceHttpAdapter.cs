using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Identity.Client;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using IVCE.DAI.Adapters.Extensions;
using IVCE.DAI.Domain.Models.Dayforce;
using IVCE.DAI.Adapters.Config;

namespace IVCE.DAI.Adapters
{
    public interface IDayforceHttpAdapter
    {

        Task<TReturn> ReceiveAsync<TReturn>(IModuleCountryContext moduleSettings);
        Task<TReturn> SendAsync<TReturn>(IModuleCountryContext moduleSettings);
        //Task<T> ReceiveAsync<T>(string queryParameters);
        //Task SendAsync(string body);
        //Task SendUpdateAsync(string body );
 
        Task InitialiseHttpClient(IModuleCountryContext moduleCountryContext);

    }

    public class DayforceHttpAdapter : IDayforceHttpAdapter
    {
        private const string ModuleName = "IVCE.DAI.Adapters.InboundHttpAdapter";

        private readonly IConfigurationRoot _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TelemetryClient _telemetryClient;
        private HttpClient _httpClient;
        //private readonly string _appTagName;

        public DayforceHttpAdapter(IConfigurationRoot configuration, IHttpClientFactory httpClientFactory, TelemetryClient telemetryClient)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _telemetryClient = telemetryClient;

        }

        public async Task InitialiseHttpClient(IModuleCountryContext moduleCountryContext)
        {
           
            _httpClient = _httpClientFactory.CreateClient(moduleCountryContext.AppTag);

            var authToken = await this.AquireBearerToken(moduleCountryContext);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);
            _httpClient.BaseAddress = new Uri(moduleCountryContext.ApiBaseUri);

        }


        private async Task<string> AquireBearerToken(IModuleCountryContext moduleCountryContext)
        {
            var httpClient = _httpClientFactory.CreateClient(moduleCountryContext.AppTag);
            httpClient.BaseAddress = new Uri(moduleCountryContext.DayforceOAuthUri);

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

            // password stored in KeyVault Ref
            var dfPwd = _configuration[moduleCountryContext.DayforcePwdKV];

            var dict = new Dictionary<string, string>();
            dict.Add("Content-Type", "application/x-www-form-urlencoded");

            var formBodyDict = new Dictionary<string, string>();

            formBodyDict.Add("Content-Type", "application/x-www-form-urlencoded");
            formBodyDict.Add("grant_type", "password");
            formBodyDict.Add("companyId", moduleCountryContext.DayforceEnvironment);
            formBodyDict.Add("username", moduleCountryContext.DayforceOAuthUsername);
            formBodyDict.Add("password", dfPwd);
            formBodyDict.Add("client_id", moduleCountryContext.DayforceClientId);
 

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));

                HttpResponseMessage response = await client.PostAsync(moduleCountryContext.DayforceOAuthUri, new FormUrlEncodedContent(formBodyDict));

                // Invokes exception when http client errors.
                response.EnsureSuccessStatusCodeExt();
                var result= response.Content.ReadAsStringAsync().Result;

                try
                {

                    // return await JsonSerializer.DeserializeAsync<TReturn>(stream, options);
                    var authTokenObj = JsonSerializer.Deserialize<DayForceAuthToken>(result, options);

                    return authTokenObj.AccessToken;

                }
                catch (HttpRequestException ex)
                {
                    _telemetryClient.TrackException(ex);
                    throw;
                }
            }

        }

        public async Task<TReturn> ReceiveAsync<TReturn>(IModuleCountryContext moduleCountryContext)
        {
            HttpResponseMessage response = null;

           await InitialiseHttpClient(moduleCountryContext);

            _telemetryClient.TrackEvent($"{ModuleName}:ReceiveAsync started for module:{moduleCountryContext.AppTag}");

            var effectiveStartDate =string.IsNullOrEmpty(moduleCountryContext.DayforceDefaultEffectiveStartDate) 
                    ? DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"): moduleCountryContext.DayforceDefaultEffectiveStartDate;

            var effectiveEndDate = Convert.ToDateTime(effectiveStartDate).AddDays(Convert.ToDouble(moduleCountryContext.EffectiveStartDateIntervalDays)).ToString("yyyy-MM-dd");

            // request uri will include parameters if needed.
            var requestUri = $"{moduleCountryContext.ApiBaseUri}/{moduleCountryContext.SuffixUri}" +
                    $"?{moduleCountryContext.DayforceGuidCountryQueryParameters}={moduleCountryContext.CountryCode}" +
                        $"&{moduleCountryContext.DayforceGuidEffectiveStartDateQueryParameters}={effectiveStartDate}" +
                           $"&{moduleCountryContext.DayforceGuidEffectiveEndDateQueryParameters}={effectiveEndDate}";

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

            _telemetryClient.TrackEvent($"{ModuleName}:ReceiveAsync - about to call Dayforce Endpoint with uri: {requestUri}");

            using ( response = await _httpClient.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead))
            {
                _telemetryClient.TrackEvent($"{ModuleName}:ReceiveAsync - called Dayforce Endpoint with uri: {_httpClient.BaseAddress.AbsoluteUri}");

                // Determines if original url has been redirected or will raise any other http errors
                var newUri = response.TryGetSuccessStatusCode();

                if(newUri !=null)
                     _telemetryClient.TrackEvent($"{ModuleName}:ReceiveAsync - Dayforce Endpoint has been redirected by Dayforce Server with uri: {newUri}");

                response = (newUri != null) ? await _httpClient.GetAsync(newUri, HttpCompletionOption.ResponseHeadersRead) : response;


                var stream = await response.Content.ReadAsStreamAsync();

                try
                {

                    // return await JsonSerializer.DeserializeAsync<TReturn>(stream, options);
                   return  JsonSerializer.DeserializeAsync<TReturn>(stream, options).GetAwaiter().GetResult();
                
                }
                catch (HttpRequestException ex)
                {
                    _telemetryClient.TrackException(ex);
                   throw;                 
                }

            }

        }

        /// <summary>
        /// Handles a GEt with Body in request
        /// </summary>
        /// <typeparam name="TReturn"></typeparam>
        /// <param name="moduleCountryContext"></param>
        /// <returns></returns>
        public async Task<TReturn> SendAsync<TReturn>(IModuleCountryContext moduleCountryContext)
        {
            await InitialiseHttpClient(moduleCountryContext);

            _telemetryClient.TrackEvent($"{ModuleName}:SendAsync - started for module:{moduleCountryContext.AppTag}");

            // request uri will include parameters if needed.
            var requestUri = $"{moduleCountryContext.ApiBaseUri}/{moduleCountryContext.SuffixUri}/";
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    
            using StringContent content = new StringContent($"{{ \"@CountryCode\": \"{moduleCountryContext.CountryCode}\", \"@EffectiveStart\": \"2022-07-01\"}}");

            // Create HttpRequestMessage and set method, OAuth header, and content.
            using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        
            request.Content = content;

           // return response.Content.ReadAsStringAsync;
            using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
            {
                // Invokes exception when http client errors.
                response.EnsureSuccessStatusCodeExt();

                var stream = await response.Content.ReadAsStreamAsync();

                try
                {
                
                    var objectResponse =  JsonSerializer.DeserializeAsync<TReturn>(stream, options).GetAwaiter().GetResult();

                    return objectResponse;

                }
                catch (HttpRequestException ex)
                {
                    _telemetryClient.TrackException(ex);
                    throw;

                }

            }
        }

        public bool Send(string body)
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(string body)
        {
            throw new NotImplementedException();
        }


        public bool SendUpdate(string body)
        {
            throw new NotImplementedException();
        }

        public Task SendUpdateAsync(string body)
        {
            throw new NotImplementedException();
        }


    }
}
