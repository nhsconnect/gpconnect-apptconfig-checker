using gpconnect_appointment_checker.DAL.Interfaces;
using gpconnect_appointment_checker.Helpers;
using gpconnect_appointment_checker.SDS.Interfaces;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using User = gpconnect_appointment_checker.DTO.Request.Application.User;

namespace gpconnect_appointment_checker.SDS
{
    public class LdapTokenService : ILdapTokenService
    {
        private readonly ILogger<LdapTokenService> _logger;
        private readonly ILdapService _ldapService;
        private readonly IApplicationService _applicationService;

        public LdapTokenService(IServiceProvider serviceProvider)
        {
            _ldapService = serviceProvider.GetRequiredService<ILdapService>();
            _logger = serviceProvider.GetService<ILogger<LdapTokenService>>() ?? NullLogger<LdapTokenService>.Instance;
            _applicationService = serviceProvider.GetRequiredService<IApplicationService>();
        }

        public Task ExecutionTokenValidation(TokenValidatedContext context)
        {
            try
            {
                if (context == null) throw new ArgumentNullException(nameof(context));
                if (context.Principal == null) throw new ArgumentNullException(nameof(context.Principal));

                var emailAddress = StringExtensions.Coalesce(context.Principal.GetClaimValue("Email"), context.Principal.GetClaimValue("Email Address"));
                var odsCode = context.Principal.GetClaimValue("ODS");
                var organisationDetails = _ldapService.GetOrganisationDetailsByOdsCode(odsCode);
                if (organisationDetails != null)
                {
                    var organisation = _applicationService.GetOrganisation(organisationDetails.ODSCode);
                    var loggedOnUser = _applicationService.LogonUser(new User
                    {
                        EmailAddress = emailAddress,
                        DisplayName = context.Principal.GetClaimValue("DisplayName"),
                        OrganisationId = organisation.OrganisationId
                    });

                    if (context.Principal.Identity is ClaimsIdentity identity)
                    {
                        identity.AddOrReplaceClaimValue("Email", emailAddress);
                        identity.AddClaim(new Claim("OrganisationName", organisationDetails.OrganisationName));
                        identity.AddClaim(new Claim("UserSessionId", loggedOnUser.UserSessionId.ToString()));
                        identity.AddClaim(new Claim("UserId", loggedOnUser.UserId.ToString()));
                        identity.AddClaim(new Claim("ProviderODSCode", odsCode));
                        identity.AddClaim(new Claim("IsAuthorised", loggedOnUser.IsAuthorised.ToString()));
                    }

                    if (!loggedOnUser.IsAuthorised)
                    {
                        context.Response.Redirect("/AccessDenied");
                        context.HandleResponse();
                    }
                }
            }
            catch (Exception exc)
            {
                _logger.LogError("An error occurred attempting to authorise the user", exc);
                throw;
            }
            return Task.CompletedTask;
        }
    }
}
