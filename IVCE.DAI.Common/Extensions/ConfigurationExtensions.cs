
using System.Data.Common;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace IVCE.DAI.Common.Extensions
{
    public static class ConfigurationExtensions
    {
        public static async Task<IEnumerable<T>> DeserialiseForAsync<T>(this IConfiguration config, string keyName)
        {
            var json = config.GetValue<string>(keyName);

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));

            return await JsonSerializer.DeserializeAsync<IEnumerable<T>>(ms).ConfigureAwait(false);
        }

        public static async Task<string> DatabaseDescriptionForAsync(this IConfiguration config, string connectionStringName)
        {
            var dbConnectionStringBuilder = new DbConnectionStringBuilder
            {
                ConnectionString = config.GetValue<string>(connectionStringName)
            };

            Task<string> getDatabaseDescriptionTask =
               Task.Run(() =>
               {
                   var dbName = dbConnectionStringBuilder["Initial Catalog"] as string;
                   var dbServer = dbConnectionStringBuilder["Data Source"] as string;

                   return $"{dbName} database on {dbServer}";
               });

            await getDatabaseDescriptionTask.ConfigureAwait(false);

            return getDatabaseDescriptionTask.Result;
        }
    }
}
