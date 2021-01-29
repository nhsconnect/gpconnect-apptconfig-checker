using gpconnect_appointment_checker.DAL.Interfaces;
using gpconnect_appointment_checker.DTO.Request.Application;
using gpconnect_appointment_checker.Helpers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace gpconnect_appointment_checker.Controllers
{
    public class AuthController : Controller
    {
        private readonly IApplicationService _applicationService;
        private readonly IHttpContextAccessor _contextAccessor;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IApplicationService applicationService, IHttpContextAccessor contextAccessor, ILogger<AuthController> logger)
        {
            _applicationService = applicationService;
            _contextAccessor = contextAccessor;
            _logger = logger;
        }

        [Route("/Auth/Login")]
        [AllowAnonymous]
        public async Task Login(string returnUrl = "/")
        {
            await HttpContext.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties()
            {
                RedirectUri = returnUrl,
                ExpiresUtc = DateTimeOffset.Now.AddMinutes(30)
            });
        }

        [Route("/Auth/Logout")]
        public async Task<IActionResult> Logout()
        {
            var userSessionId = _contextAccessor.HttpContext?.User.GetClaimValue("UserSessionId").StringToInteger(0);
            try
            {
                if (userSessionId.HasValue && userSessionId.Value > 0)
                {
                    _applicationService.LogoffUser(new User
                    {
                        EmailAddress = _contextAccessor.HttpContext.User.GetClaimValue("Email"),
                        UserSessionId = userSessionId.Value
                    });
                }

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties
                {
                    RedirectUri = "~/"
                });
                return Redirect("~/");
            }
            catch (Exception exc)
            {
                _logger.LogError("An error occurred in trying to logout the user", exc);
                throw;
            }
        }
    }
}
