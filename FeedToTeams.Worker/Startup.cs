using System;
using System.IO;
using FeedToTeams.Worker.Utilities;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.TokenCacheProviders;
using Microsoft.Identity.Web.TokenCacheProviders.Distributed;

[assembly: FunctionsStartup(typeof(FeedToTeams.Worker.Startup))]
namespace FeedToTeams.Worker
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var serviceProvider = builder.Services.BuildServiceProvider();
            var configuration = serviceProvider.GetService<IConfiguration>();
            var baseUrl = configuration["BaseUrl"];
            var scopes = configuration["Scopes"];
            builder.Services.AddLogging()
                .AddDistributedMemoryCache()
                .AddDistributedSqlServerCache(options =>
                {
                    options.ConnectionString = configuration["FeedToTeamsDbConnection"];
                    options.SchemaName = "dbo";
                    options.TableName = "sqlTokenCache";
                    options.DefaultSlidingExpiration = TimeSpan.FromDays(1);
                })
                .AddDbContext<FeedToTeamsDbContext>(options => options.UseSqlServer(configuration["FeedToTeamsDbConnection"]))
                .AddSingleton<IMsalTokenCacheProvider, MsalDistributedTokenCacheAdapter>()
                .AddScoped<IMsalAccountActivityStore, SqlServerMsalAccountActivityStore>();
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();

            builder.ConfigurationBuilder
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, "local.settings.json"), optional: true, reloadOnChange: false)
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"local.settings.{context.EnvironmentName}.json"), optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
        }
    }
}