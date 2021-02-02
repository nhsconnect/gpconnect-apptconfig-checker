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
    public class TimeoutErrorModel : PageModel
    {
        protected ILogger<TimeoutErrorModel> _logger;

        public TimeoutErrorModel(IConfiguration configuration, IHttpContextAccessor contextAccessor, ILogger<TimeoutErrorModel> logger, ILdapService ldapService)
        {
            _logger = logger;
        }

        public string RequestId { get; set; }
        public string ExceptionMessage { get; set; }

        public void OnGet()
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            var exceptionHandlerPathFeature =
                HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            _logger.LogError(exceptionHandlerPathFeature?.Error.InnerException, $"Timeout error thrown at {exceptionHandlerPathFeature?.Path} - ");
        }
    }
}
