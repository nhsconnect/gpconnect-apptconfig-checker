using gpconnect_appointment_checker.Caching;
using gpconnect_appointment_checker.Caching.Interfaces;
using gpconnect_appointment_checker.Configuration;
using gpconnect_appointment_checker.Configuration.Infrastructure.Logging.Interface;
using gpconnect_appointment_checker.DAL;
using gpconnect_appointment_checker.DAL.Application;
using gpconnect_appointment_checker.DAL.Audit;
using gpconnect_appointment_checker.DAL.Caching;
using gpconnect_appointment_checker.DAL.Configuration;
using gpconnect_appointment_checker.DAL.Interfaces;
using gpconnect_appointment_checker.DAL.Logging;
using gpconnect_appointment_checker.GPConnect;
using gpconnect_appointment_checker.GPConnect.Interfaces;
using gpconnect_appointment_checker.SDS;
using gpconnect_appointment_checker.SDS.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;

namespace gpconnect_appointment_checker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHost(o =>
                {
                    o.CaptureStartupErrors(true).UseSetting("detailedErrors", "");
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddSingleton<IDataService, DataService>();
                        services.AddSingleton<ICachingService, CachingService>();
                        services.AddSingleton<IDatabaseExpiredItemsRemoverLoop, DatabaseExpiredItemsRemoverLoop>();
                        services.AddSingleton<IDistributedCache, PgSqlCache>();
                        services.AddScoped<ISDSQueryExecutionService, SDSQueryExecutionService>();
                        services.AddScoped<IGpConnectQueryExecutionService, GpConnectQueryExecutionService>();
                        services.AddScoped<IConfigurationService, ConfigurationService>();
                        services.AddScoped<IAuditService, AuditService>();
                        services.AddScoped<ITokenService, TokenService>();
                        services.AddScoped<IApplicationService, ApplicationService>();
                        services.AddTransient<ILdapService, LdapService>();
                        services.AddTransient<ILdapTokenService, LdapTokenService>();
                        services.AddScoped<ILogService, LogService>();
                        services.AddSingleton<ILoggerManager, LoggerManager>();
                    });
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseKestrel(options =>
                    {
                        options.AddServerHeader = false;
                    });
                }).ConfigureAppConfiguration(AddCustomConfiguration)
                .ConfigureLogging((builderContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConfiguration(builderContext.Configuration.GetSection("Logging"));
                }).UseNLog();

        private static void AddCustomConfiguration(HostBuilderContext context, IConfigurationBuilder builder)
        {
            builder.AddEnvironmentVariables("GPCONNECTAPPOINTMENTCHECKER_");
            builder.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();            
            builder.AddConfiguration(options =>
            {
                options.ConnectionString = configuration.GetConnectionString("DefaultConnection");
            });
        }
    }
}
