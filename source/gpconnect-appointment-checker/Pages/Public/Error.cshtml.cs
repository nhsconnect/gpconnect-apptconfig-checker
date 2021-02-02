using gpconnect_appointment_checker.SDS.Interfaces;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace gpconnect_appointment_checker.Pages
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class ErrorModel : PageModel
    {
        protected ILogger<ErrorModel> _logger;

        public ErrorModel(IConfiguration configuration, IHttpContextAccessor contextAccessor, ILogger<ErrorModel> logger, ILdapService ldapService)
        {
            _logger = logger;
        }

        public string RequestId { get; set; }

        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            var exceptionHandlerPathFeature =
                HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            _logger.LogError(exceptionHandlerPathFeature?.Error.InnerException, $"Error thrown at {exceptionHandlerPathFeature?.Path} - ");
        }
    }
}
