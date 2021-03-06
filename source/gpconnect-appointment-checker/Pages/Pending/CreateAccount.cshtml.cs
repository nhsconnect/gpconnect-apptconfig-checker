using gpconnect_appointment_checker.Configuration.Infrastructure.Logging.Interface;
using gpconnect_appointment_checker.DAL.Interfaces;
using gpconnect_appointment_checker.DTO.Request.Application;
using gpconnect_appointment_checker.Helpers;
using gpconnect_appointment_checker.Helpers.Enumerations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace gpconnect_appointment_checker.Pages
{
    public partial class CreateAccountModel : PageModel
    {
        protected ILogger<CreateAccountModel> _logger;
        protected IConfiguration _configuration;
        protected IHttpContextAccessor _contextAccessor;
        protected IApplicationService _applicationService;
        protected IEmailService _emailService;
        protected readonly ILoggerManager _loggerManager;

        public CreateAccountModel(IConfiguration configuration, IHttpContextAccessor contextAccessor, ILogger<CreateAccountModel> logger, IApplicationService applicationService, IEmailService emailService, ILoggerManager loggerManager = null)
        {
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _applicationService = applicationService;
            _emailService = emailService;
            if (null != loggerManager)
            {
                _loggerManager = loggerManager;
            }
        }

        public IActionResult OnGet()
        {
            ModelState.ClearValidationState("JobRole");
            ModelState.ClearValidationState("Organisation");
            ModelState.ClearValidationState("AccessRequestReason");
            return Page();
        }

        public async Task<IActionResult> OnPostSendFormAsync()
        {
            var emailSent = false;
            if (ModelState.IsValid)
            {
                var userCreateAccount = new UserCreateAccount
                {
                    EmailAddress = User.GetClaimValue("Email"),
                    JobRole = JobRole,
                    OrganisationName = Organisation,
                    Reason = AccessRequestReason,
                    DisplayName = User.GetClaimValue("DisplayName"),
                    OrganisationId = User.GetClaimValue("OrganisationId").StringToInteger()
                };
                var createdUser = _applicationService.AddOrUpdateUser(userCreateAccount);               

                if (createdUser != null && createdUser.UserAccountStatusId == (int)UserAccountStatus.Pending)
                {
                    emailSent = _emailService.SendUserCreateAccountEmail(createdUser, userCreateAccount);                    
                }

                TempData["EmailAddressManual"] = _configuration.GetSection("General:get_access_email_address").GetConfigurationString(string.Empty);
                TempData["EmailSent"] = emailSent;

                return Redirect("/Pending/Index");
            }
            return Page();
        }

        public IActionResult OnPostClear()
        {
            JobRole = null;
            Organisation = User.GetClaimValue("OrganisationName");
            AccessRequestReason = null;
            ModelState.Clear();
            return Page();
        }
    }
}
