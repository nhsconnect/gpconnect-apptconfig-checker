﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Targets;
using System.Data;

namespace gpconnect_appointment_checker.Configuration.Infrastructure
{
    public static class LoggingExtensions
    {
        public static IServiceCollection ConfigureLoggingServices(this IServiceCollection services, IConfiguration configuration)
        {
            var nLogConfiguration = new NLog.Config.LoggingConfiguration();

            var consoleTarget = AddConsoleTarget();
            var databaseTarget = AddDatabaseTarget(configuration);

            nLogConfiguration.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, consoleTarget);
            nLogConfiguration.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, databaseTarget);

            nLogConfiguration.AddTarget(consoleTarget);
            nLogConfiguration.AddTarget(databaseTarget);

            nLogConfiguration.Variables.Add("applicationVersion", ApplicationVersion.GetAssemblyVersion);

            LogManager.Configuration = nLogConfiguration;

            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddConfiguration(configuration.GetSection("Logging"));
            });

            return services;
        }

        private static DatabaseTarget AddDatabaseTarget(IConfiguration configuration)
        {
            var databaseTarget = new DatabaseTarget
            {
                Name = "Database",
                ConnectionString = configuration.GetConnectionString("DefaultConnection"),
                CommandType = CommandType.Text,
                CommandText = "INSERT INTO logging.error_log (Application, Logged, Level, Message, Logger, CallSite, Exception, User_Id, User_Session_Id) VALUES (@Application, @Logged, @Level, @Message, @Logger, @Callsite, @Exception, @UserId, @UserSessionId)",
                DBProvider = "Npgsql.NpgsqlConnection, Npgsql"
            };

            databaseTarget.Parameters.Add(new DatabaseParameterInfo
            {
                Name = "@Application",
                Layout = "${var:applicationVersion}",
                DbType = DbType.String.ToString()
            });

            databaseTarget.Parameters.Add(new DatabaseParameterInfo
            {
                Name = "@Logged",
                Layout = "${date}",
                DbType = DbType.DateTime.ToString()
            });

            databaseTarget.Parameters.Add(new DatabaseParameterInfo
            {
                Name = "@Level",
                Layout = "${level:uppercase=true}",
                DbType = DbType.String.ToString()
            });

            databaseTarget.Parameters.Add(new DatabaseParameterInfo
            {
                Name = "@Level",
                Layout = "${level:uppercase=true}",
                DbType = DbType.String.ToString()
            });

            databaseTarget.Parameters.Add(new DatabaseParameterInfo
            {
                Name = "@Message",
                Layout = "${message}",
                DbType = DbType.String.ToString()
            });

            databaseTarget.Parameters.Add(new DatabaseParameterInfo
            {
                Name = "@Logger",
                Layout = "${logger}",
                DbType = DbType.String.ToString()
            });

            databaseTarget.Parameters.Add(new DatabaseParameterInfo
            {
                Name = "@Callsite",
                Layout = "${callsite:filename=true}",
                DbType = DbType.String.ToString()
            });

            //var exceptionLayout = new JsonLayout();
            //exceptionLayout.Attributes.Add(new JsonAttribute("exception", "${exception:format=shortType,message,stacktrace:innerFormat=shortType,message}"));
            //exceptionLayout.Attributes.Add(new JsonAttribute("innerException", new JsonLayout
            //{
            //    Attributes =
            //    {
            //        new JsonAttribute("type", "${exception:format=:innerFormat=Type:InnerExceptionSeparator=|}"),
            //        new JsonAttribute("message", "${exception:format=:innerFormat=Message:InnerExceptionSeparator=|}")
            //    }
            //}));

            databaseTarget.Parameters.Add(new DatabaseParameterInfo
            {
                Name = "@Exception",
                Layout = "${exception:format=shortType,message,stacktrace:innerFormat=shortType,message}",
                DbType = DbType.String.ToString()
            });

            databaseTarget.Parameters.Add(new DatabaseParameterInfo
            {
                Name = "@UserId",
                Layout = "${var:userId}",
                DbType = DbType.Int32.ToString()
            });

            databaseTarget.Parameters.Add(new DatabaseParameterInfo
            {
                Name = "@UserSessionId",
                Layout = "${var:userSessionId}",
                DbType = DbType.Int32.ToString()
            });
            return databaseTarget;
        }

        private static ConsoleTarget AddConsoleTarget()
        {
            var consoleTarget = new ConsoleTarget
            {
                Name = "Console",
                Layout = "${var:applicationVersion}|${date}|${level:uppercase=true}|${message}|${logger}|${callsite:filename=true}|${exception:format=stackTrace}|${var:userId}|${var:userSessionId}"
            };
            return consoleTarget;
        }

        public static string GetSessionId(this HttpContext context)
        {
            var hasSession = context.Features.Get<ISessionFeature>()?.Session != null;
            return hasSession ? context.Session != null ? context.Session.Id : string.Empty : string.Empty;
        }
    }
}