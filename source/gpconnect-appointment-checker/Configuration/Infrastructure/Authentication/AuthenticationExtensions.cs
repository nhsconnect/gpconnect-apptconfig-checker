using gpconnect_appointment_checker.Helpers;
using gpconnect_appointment_checker.SDS;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Threading.Tasks;

namespace gpconnect_appointment_checker.Configuration.Infrastructure.Authentication
{
    public static class AuthenticationExtensions
    {
        public static void ConfigureAuthenticationServices(this IServiceCollection services,
            IConfiguration configuration)
        {

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = configuration.GetSection("SingleSignOn:auth_scheme").GetConfigurationString(throwExceptionIfEmpty: true);
                options.DefaultSignInScheme = configuration.GetSection("SingleSignOn:auth_scheme").GetConfigurationString(throwExceptionIfEmpty: true);
                options.DefaultChallengeScheme = configuration.GetSection("SingleSignOn:challenge_scheme").GetConfigurationString(throwExceptionIfEmpty: true);
            }).AddCookie(options =>
            {
                options.Cookie.HttpOnly = false;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
                options.LoginPath = "/Public/Index";
                options.SlidingExpiration = true;
                options.Events.OnValidatePrincipal = PrincipalValidator.ValidateAsync;
                options.Cookie.Name = ".GpConnectAppointmentChecker.AuthenticationCookie";
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            })
            .AddOpenIdConnect(
                configuration.GetSection("SingleSignOn:challenge_scheme").GetConfigurationString(throwExceptionIfEmpty: true),
                options =>
                {
                    options.SignInScheme = configuration.GetSection("SingleSignOn:auth_scheme").GetConfigurationString(throwExceptionIfEmpty: true);
                    options.Authority = configuration.GetSection("SingleSignOn:auth_endpoint").GetConfigurationString(throwExceptionIfEmpty: true);
                    options.MetadataAddress = configuration.GetSection("SingleSignOn:metadata_endpoint").GetConfigurationString(throwExceptionIfEmpty: true);
                    options.MaxAge = TimeSpan.FromMinutes(2);
                    options.SignedOutRedirectUri = "/Auth/Login";
                    options.SaveTokens = true;
                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("email");
                    options.ResponseType = OpenIdConnectResponseType.CodeIdToken;

                    options.ClientId = configuration.GetSection("SingleSignOn:client_id").GetConfigurationString(throwExceptionIfEmpty: true);
                    options.ClientSecret = configuration.GetSection("SingleSignOn:client_secret").GetConfigurationString(throwExceptionIfEmpty: true);
                    options.CallbackPath = configuration.GetSection("SingleSignOn:callback_path").GetConfigurationString(throwExceptionIfEmpty: true);
                    options.SignedOutCallbackPath = configuration.GetSection("SingleSignOn:signed_out_callback_path").GetConfigurationString(throwExceptionIfEmpty: true);
                    options.Events = new OpenIdConnectEvents
                    {
                        OnTokenValidated = context =>
                        {
                            var ldapTokenService = new LdapTokenService(services.BuildServiceProvider());
                            var tokenValidation = ldapTokenService.ExecutionTokenValidation(context);
                            return tokenValidation;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            if (context.Exception == null)
                            {
                                context.Response.Redirect("/AccessDenied");
                            }
                            context.Response.Redirect("/Error");
                            context.HandleResponse();
                            return Task.CompletedTask;
                        },
                        OnRemoteFailure = context =>
                        {
                            context.Response.Redirect("/Error");
                            context.HandleResponse();
                            return Task.CompletedTask;
                        }
                    };
                });
        }
    }
}
