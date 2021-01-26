using gpconnect_appointment_checker.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using System;
using System.Threading.Tasks;

namespace gpconnect_appointment_checker.Configuration.Infrastructure.Authentication
{
    internal static class PrincipalValidator
    {
        internal static Task ValidateAsync(CookieValidatePrincipalContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            var userId = context.Principal.GetClaimValue("UserId");
            var userSessionId = context.Principal.GetClaimValue("UserSessionId");
            var isAuthorised = bool.Parse(context.Principal.GetClaimValue("IsAuthorised"));

            if (userId == null || !isAuthorised)
            {
                context.RejectPrincipal();
            }

            NLog.LogManager.Configuration.Variables["userId"] = userId;
            NLog.LogManager.Configuration.Variables["userSessionId"] = userSessionId;

            context.Options.ExpireTimeSpan = TimeSpan.FromMinutes(2);
            
            return Task.CompletedTask;
        }
    }
}
