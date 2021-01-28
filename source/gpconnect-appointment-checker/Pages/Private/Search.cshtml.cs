using gpconnect_appointment_checker.Configuration.Infrastructure.Logging.Interface;
using gpconnect_appointment_checker.DAL.Interfaces;
using gpconnect_appointment_checker.DTO.Response.Application;
using gpconnect_appointment_checker.DTO.Response.Configuration;
using gpconnect_appointment_checker.DTO.Response.GpConnect;
using gpconnect_appointment_checker.GPConnect.Constants;
using gpconnect_appointment_checker.GPConnect.Interfaces;
using gpconnect_appointment_checker.Helpers;
using gpconnect_appointment_checker.Helpers.Constants;
using gpconnect_appointment_checker.SDS.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace gpconnect_appointment_checker.Pages
{
    public partial class SearchModel : PageModel
    {
        protected IConfiguration _configuration;
        protected IHttpContextAccessor _contextAccessor;
        protected ILogger<SearchModel> _logger;
        protected ILdapService _ldapService;
        protected IApplicationService _applicationService;
        protected ITokenService _tokenService;
        protected IGpConnectQueryExecutionService _queryExecutionService;
        protected IAuditService _auditService;
        protected readonly ILoggerManager _loggerManager;
        protected Stopwatch _stopwatch = new Stopwatch();
        protected List<string> _auditSearchParameters = new List<string>(new [] { "", "", "" });
        protected List<string> _auditSearchIssues = new List<string>();

        public SearchModel(IConfiguration configuration, IHttpContextAccessor contextAccessor, ILogger<SearchModel> logger, ILdapService ldapService, ITokenService tokenService, IGpConnectQueryExecutionService queryExecutionService, IApplicationService applicationService, IAuditService auditService, ILoggerManager loggerManager = null)
        {
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _logger = logger;
            _ldapService = ldapService;
            _tokenService = tokenService;
            _queryExecutionService = queryExecutionService;
            _applicationService = applicationService;
            _auditService = auditService;
            if (null != loggerManager)
            {
                _loggerManager = loggerManager;
            }
        }

        public IActionResult OnGet()
        {
            var userCode = User.GetClaimValue("ODS");
            if (!string.IsNullOrEmpty(userCode)) ProviderODSCode = userCode;
            return Page();
        }

        public async Task<IActionResult> OnPostSearchAsync()
        {
            if (ModelState.IsValid)
            {
                ProviderODSCode = ProviderODSCode.ToUpper().StripNonAlphanumericCharacters();
                ConsumerODSCode = ConsumerODSCode.ToUpper().StripNonAlphanumericCharacters();
                _stopwatch.Start();
                await GetSearchResults();
                _stopwatch.Stop();
                SearchDuration = _stopwatch.Elapsed.TotalSeconds;
                _queryExecutionService.SendToAudit(_auditSearchParameters, _auditSearchIssues, _stopwatch, SearchResultsCount);
            }
            return Page();
        }

        public IActionResult OnPostClear()
        {
            ProviderODSCode = null;
            ConsumerODSCode = null;
            SelectedDateRange = DateRanges.First().Value;
            IncludePastAppointments = false;
            ModelState.Clear();
            return Page();
        }

        private async Task GetSearchResults()
        {
            try
            {
                //LDAP Query 1 - Get the organisation details for the Provider ODS Code
                var providerOrganisationDetails = _ldapService.GetOrganisationDetailsByOdsCode(ProviderODSCode);
                //LDAP Query 2 - Get the organisation details for the Consumer ODS Code
                var consumerOrganisationDetails = _ldapService.GetOrganisationDetailsByOdsCode(ConsumerODSCode);

                _auditSearchParameters[0] = ConsumerODSCode;
                _auditSearchParameters[1] = ProviderODSCode;
                _auditSearchParameters[2] = SelectedDateRange;

                ProviderODSCodeFound = providerOrganisationDetails != null;
                ConsumerODSCodeFound = consumerOrganisationDetails != null;

                if (ProviderODSCodeFound && ConsumerODSCodeFound)
                {
                    //LDAP Query 3 - Get the endpoint and party key for the Provider ODS Code
                    var providerGpConnectDetails =
                        _ldapService.GetGpProviderEndpointAndPartyKeyByOdsCode(ProviderODSCode);
                    //LDAP Query 4 - Get the endpoint and party key for the Consumer ODS Code
                    var consumerGpConnectDetails =
                        _ldapService.GetGpProviderEndpointAndPartyKeyByOdsCode(ConsumerODSCode);

                    ProviderEnabledForGpConnectAppointmentManagement = providerGpConnectDetails != null;

                    if (ProviderEnabledForGpConnectAppointmentManagement && consumerOrganisationDetails != null)
                    {
                        //LDAP Query 5 - Get the AsId for the Provider ODS Code to check they are enabled for GP Connect
                        var providerAsId =
                            _ldapService.GetGpProviderAsIdByOdsCodeAndPartyKey(ProviderODSCode,
                                providerGpConnectDetails.party_key);
                        ProviderASIDPresent = providerAsId != null;

                        if (ProviderASIDPresent)
                        {
                            ProviderPublisher = providerAsId.product_name;
                            providerGpConnectDetails.asid = providerAsId.asid;
                            await PopulateSearchResults(providerGpConnectDetails, providerOrganisationDetails,
                                consumerGpConnectDetails, consumerOrganisationDetails);
                            SearchAtResultsText =
                                $"{providerOrganisationDetails.OrganisationName} ({providerOrganisationDetails.ODSCode}) - {StringExtensions.AddressBuilder(providerOrganisationDetails.PostalAddressFields.ToList(), providerOrganisationDetails.PostalCode)}";
                            SearchOnBehalfOfResultsText =
                                $"{consumerOrganisationDetails.OrganisationName} ({consumerOrganisationDetails.ODSCode}) - {StringExtensions.AddressBuilder(consumerOrganisationDetails.PostalAddressFields.ToList(), consumerOrganisationDetails.PostalCode)}";
                        }
                        else
                        {
                            _auditSearchIssues.Add(SearchConstants.ISSUEWITHGPCONNECTPROVIDERTEXT);
                        }
                    }
                    else
                    {
                        _auditSearchIssues.Add(string.Format(SearchConstants.ISSUEWITHGPCONNECTPROVIDERNOTENABLEDTEXT,
                            ProviderODSCode));
                    }
                }
                else
                {
                    if (!ProviderODSCodeFound)
                        _auditSearchIssues.Add(string.Format(SearchConstants.ISSUEWITHPROVIDERODSCODETEXT, ProviderODSCode));
                    if (!ConsumerODSCodeFound)
                        _auditSearchIssues.Add(string.Format(SearchConstants.ISSUEWITHCONSUMERODSCODETEXT, ConsumerODSCode));
                }
            }
            catch (Novell.Directory.Ldap.LdapException)
            {
                LdapErrorRaised = true;
                _auditSearchIssues.Add(SearchConstants.ISSUEWITHLDAPTEXT);
            }
            catch (TimeoutException)
            {
                TimeoutErrorRaised = true;
                _auditSearchIssues.Add(SearchConstants.ISSUEWITHTIMEOUTTEXT);
            }
        }

        private async Task PopulateSearchResults(Spine providerGpConnectDetails, Organisation providerOrganisationDetails,
            Spine consumerGpConnectDetails, Organisation consumerOrganisationDetails)
        {
            var requestParameters = _tokenService.ConstructRequestParameters(
                _contextAccessor.HttpContext.GetAbsoluteUri(), providerGpConnectDetails, providerOrganisationDetails,
                consumerGpConnectDetails, consumerOrganisationDetails, (int)SpineMessageTypes.GpConnectSearchFreeSlots);

            var startDate = Convert.ToDateTime(SelectedDateRange.Split(":")[0]);
            var endDate = Convert.ToDateTime(SelectedDateRange.Split(":")[1]);

            if (requestParameters != null)
            {
                //Step 3 - CALL PROVIDER METADATA ENDPOINT
                //Get capability statement
                var capabilityStatement = await _queryExecutionService.ExecuteFhirCapabilityStatement(requestParameters, providerGpConnectDetails.ssp_hostname);
                CapabilityStatementOk = (capabilityStatement.Issue?.Count == 0 || capabilityStatement.Issue == null);

                if (CapabilityStatementOk)
                {
                    var searchResults = await _queryExecutionService.ExecuteFreeSlotSearch(requestParameters, startDate, endDate, providerGpConnectDetails.ssp_hostname, IncludePastAppointments);
                    SlotSearchOk = searchResults?.Issue == null;

                    if (SlotSearchOk)
                    {
                        SearchResults = new List<List<SlotEntrySimple>>();
                        var locationGrouping = searchResults?.SlotEntrySimple.GroupBy(l => l.LocationName)
                            .Select(grp => grp.ToList()).ToList();
                        SearchResultsCount = searchResults?.SlotEntrySimple.Count;

                        if (locationGrouping != null)
                        {
                            SearchResults.AddRange(locationGrouping);
                        }
                    }
                    else
                    {
                        ProviderErrorDisplay = searchResults.Issue.FirstOrDefault()?.Details.Coding.FirstOrDefault()?.Display;
                        ProviderErrorCode = searchResults.Issue.FirstOrDefault()?.Details.Coding.FirstOrDefault()?.Code;
                        ProviderErrorDiagnostics = StringExtensions.Coalesce(searchResults.Issue.FirstOrDefault()?.Diagnostics, searchResults.Issue.FirstOrDefault()?.Details.Text);
                        _auditSearchIssues.Add(string.Format(SearchConstants.ISSUEWITHSENDINGMESSAGETOPROVIDERSYSTEMTEXT, ProviderErrorDisplay, ProviderErrorCode));
                    }
                }
                else
                {
                    if (capabilityStatement?.Issue != null)
                    {
                        ProviderErrorDisplay = capabilityStatement?.Issue?.FirstOrDefault()?.Details.Coding.FirstOrDefault()?.Display;
                        ProviderErrorCode = capabilityStatement?.Issue?.FirstOrDefault()?.Details.Coding.FirstOrDefault()?.Code;
                        ProviderErrorDiagnostics = StringExtensions.Coalesce(capabilityStatement?.Issue?.FirstOrDefault()?.Diagnostics, capabilityStatement?.Issue.FirstOrDefault()?.Details.Text);
                        _auditSearchIssues.Add(string.Format(SearchConstants.ISSUEWITHSENDINGMESSAGETOPROVIDERSYSTEMTEXT, ProviderErrorDisplay, ProviderErrorCode));
                    }
                }
            }
        }

        private List<SelectListItem> GetDateRanges()
        {
            var weeksToGet = _configuration["General:max_num_weeks_search"].StringToInteger(12);
            var dateRange = new List<SelectListItem>();
            var firstDayOfCurrentWeek = DateTime.Now.StartOfWeek(DayOfWeek.Monday);

            for (var i = 0; i < weeksToGet; i++)
            {
                var week = new SelectListItem
                {
                    Text = $"{firstDayOfCurrentWeek:ddd d MMM} - {firstDayOfCurrentWeek.AddDays(6):ddd d MMM}",
                    Value = $"{firstDayOfCurrentWeek:d-MMM-yyyy}:{firstDayOfCurrentWeek.AddDays(6):d-MMM-yyyy}"
                };
                dateRange.Add(week);
                firstDayOfCurrentWeek = firstDayOfCurrentWeek.AddDays(7);
            }
            return dateRange;
        }
    }
}
