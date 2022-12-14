using System;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;
using NUnit.Framework;
using IVCE.DAI.Adapters.Inbound;
using Microsoft.Extensions.DependencyInjection;
using IVCE.DAI.Adapters.Models;
using IVCE.DAI.Adapters.Extensions;
using System.Linq;

namespace IVCE.DAI.Adapters.UnitTets
{
    public class InboundHttpAdapterTest
    {

        [SetUp]
        public void Setup()
        {
            //var loggerFactory = LoggerFactory.Create(builder =>
            //{
            //    builder
            //        .AddFilter("Microsoft", LogLevel.Warning)
            //        .AddFilter("System", LogLevel.Warning)
            //        .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
            //        .AddConsole()
            //        .AddEventSourceLogger();
            //});

            //var logger = loggerFactory.CreateLogger<IInboundHttpAdapter>();
            var serviceCollection = new ServiceCollection();

            ConfigureServices(serviceCollection); 

        }

        private void ConfigureServices(IServiceCollection services)
        {
            var settings = new ConfigurationBuilder()
                    .AddJsonFile("config/appsettings.json")
                    .AddEnvironmentVariables()
                    .Build();

            var configuration = new ConfigurationBuilder()
               .AddJsonFile("config/appsettings.json")
               .AddEnvironmentVariables()
               .AddAzureAppConfiguration(options =>
               {
                   options.Connect(settings["ConnectionStrings:AppConfig"])
                           .Select(KeyFilter.Any, settings["DOTNET_ENVIRONMENT"])
                           .UseFeatureFlags()
                           .ConfigureRefresh(refresh =>
                           {
                               refresh.Register(".appconfig.featureflag/IBERIA_AAD_FEATURE", settings["DOTNET_ENVIRONMENT"], true)
                                      .SetCacheExpiration(TimeSpan.FromSeconds(1));
                           });
                   //_refresher = options.GetRefresher();
               })
               .Build();

  

            services.AddSingleton<IConfiguration>(configuration);
            //services.AddSingleton(_refresher);
            //services.AddSingleton(typeof(MainWindow));
            //services.AddSingleton<IModulesFactory, ModulesFactory>();
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }
    }
}