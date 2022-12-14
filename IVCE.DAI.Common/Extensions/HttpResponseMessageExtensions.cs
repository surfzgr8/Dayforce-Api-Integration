using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace IVCE.DAI.Common.Extensions
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task EnsureSuccessStatusCodeAsync(this HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var content = await response.Content.ReadAsStringAsync();

            if (response.Content != null)
                response.Content.Dispose();

            throw new SimpleHttpResponseException(response.StatusCode, content);
        }

        public static void EnsureSuccessStatusCode(this HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (response.Content != null)
                response.Content.Dispose();

            throw new SimpleHttpResponseException(response.StatusCode, content);
        }

        public static async Task EnsureSuccessStatusCodeExtAsync(this HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            // var contentMessage = string.IsNullOrWhiteSpace(content) ? string.Empty : $"Content: {content}";
            var contentMessage = await response.Content.ReadAsStringAsync();

            throw new HttpRequestException(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Response status code does not indicate success: {0} ({1}).{2}", (int)response.StatusCode, response.ReasonPhrase,
                         contentMessage));
        }


        public static void EnsureSuccessStatusCodeExt(this HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            // var contentMessage = string.IsNullOrWhiteSpace(content) ? string.Empty : $"Content: {content}";
            var contentMessage = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            throw new HttpRequestException(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Response status code does not indicate success: {0} ({1}).{2}", (int)response.StatusCode, response.ReasonPhrase,
                         contentMessage));
        }

        public static string TryGetSuccessStatusCode(this HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return null;

            // initial Url redirected by server
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // then return redirected url this ensures auth headers are followed
                return response.RequestMessage.RequestUri.ToString();
            }

            // var contentMessage = string.IsNullOrWhiteSpace(content) ? string.Empty : $"Content: {content}";
            var contentMessage = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            throw new HttpRequestException(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "Response status code does not indicate success: {0} ({1}).{2}", (int)response.StatusCode, response.ReasonPhrase,
                         contentMessage));
        }
    }

    public class SimpleHttpResponseException : Exception
    {
        public HttpStatusCode StatusCode { get; private set; }

        public SimpleHttpResponseException(HttpStatusCode statusCode, string content) : base(content)
        {
            StatusCode = statusCode;
        }

    }



}
