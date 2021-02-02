using gpconnect_appointment_checker.Caching;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace gpconnect_appointment_checker.Configuration.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureApplicationServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
        {
            services.AddDistributedPgSqlCache(o =>
            {
                o.DefaultSlidingExpiration = TimeSpan.FromMinutes(30);
                o.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(60);
            });

            services.AddSession(s =>
            {
                s.Cookie.Name = ".GpConnectAppointmentChecker.Session";

                s.IdleTimeout = new System.TimeSpan(0, 30, 0);

                s.Cookie.HttpOnly = false;
                s.Cookie.IsEssential = true;
            });

            services.Configure<CookiePolicyOptions>(options =>
            {
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.AddHsts(options =>
            {
                options.IncludeSubDomains = true;
                options.MaxAge = TimeSpan.FromDays(730);
            });

            services.AddResponseCaching();
            services.AddResponseCompression();
            services.AddHttpContextAccessor();

            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizeFolder("/Private"); 
                options.Conventions.AllowAnonymousToFolder("/Public");
                options.Conventions.AddPageRoute("/Private/Search", "/Search");
                options.Conventions.AddPageRoute("/Public/Error", "/Error");
                options.Conventions.AddPageRoute("/Public/AccessDenied", "/AccessDenied");
                options.Conventions.AddPageRoute("/Public/Accessibility", "/Accessibility");
                options.Conventions.AddPageRoute("/Public/PrivacyAndCookies", "/PrivacyAndCookies");
                options.Conventions.AddPageRoute("/Public/Help/Help", "/Help");
            });

            services.AddAntiforgery(options =>
            {
                options.SuppressXFrameOptionsHeader = true;
                options.Cookie.HttpOnly = false;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.None;
            });
            HttpClientExtensions.AddHttpClientServices(services, configuration, env);
            return services;
        }
    }
}
