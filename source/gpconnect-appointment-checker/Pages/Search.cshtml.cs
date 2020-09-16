﻿using gpconnect_appointment_checker.Helpers;
using gpconnect_appointment_checker.ViewModels.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using gpconnect_appointment_checker.SDS.Interfaces;

namespace gpconnect_appointment_checker.Pages
{
    public class SearchModel : PageModel
    {
        public List<SelectListItem> DateRanges => GetDateRanges();
        public SearchResultItemList SearchResults => GetSearchResults();

        protected IConfiguration _configuration;
        protected IHttpContextAccessor _contextAccessor;
        protected ILogger<SearchModel> _logger;
        protected ILdapService _ldapService;

        public SearchModel(IConfiguration configuration, IHttpContextAccessor contextAccessor, ILogger<SearchModel> logger, ILdapService ldapService)
        {
            _configuration = configuration;
            _contextAccessor = contextAccessor;
            _logger = logger;
            _ldapService = ldapService;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPostSearch(SearchForm searchForm)
        {
            var organisationDetails = await _ldapService.GetOrganisationDetailsByOdsCode(searchForm.ConsumerODSCode);

            if (!ModelState.IsValid)
            {
                return Page();
            }
            return Page();
        }

        public IActionResult OnPostClear()
        {
            return Page();
        }

        private List<SelectListItem> GetDateRanges()
        {
            _logger.LogInformation("Getting DateRanges");
            
            var weeksToGet = 12;
            var dateRange = new List<SelectListItem>();
            var firstDayOfCurrentWeek = DateTime.Now.StartOfWeek(DayOfWeek.Monday);

            for (var i = 0; i < weeksToGet; i++)
            {
                var week = new SelectListItem
                {
                    Text = $"{firstDayOfCurrentWeek:ddd d-MMM} - {firstDayOfCurrentWeek.AddDays(6):ddd d-MMM}",
                    Value = $"{firstDayOfCurrentWeek:d-MMM-yyyy}:{firstDayOfCurrentWeek.AddDays(6):d-MMM-yyyy}"
                };
                dateRange.Add(week);
                firstDayOfCurrentWeek = firstDayOfCurrentWeek.AddDays(7);
            }
            return dateRange;
        }

        private SearchResultItemList GetSearchResults()
        {
            var results = new SearchResultItemList
            {
                new SearchResultItem()
                {
                    AppointmentDate = DateTime.Now.ToString("ddd d MMM yyyy"),
                    DeliveryChannel = "In Person",
                    Duration = 10.DurationFormatter("Mins"),
                    Location = "Laurel Bank Surgery, North Lane, Skipton",
                    Practitioner = "ROBERTS, Sam (Mr)",
                    PractitionerRole = "Nurse Practitioner",
                    PractitionerGender = "Male",
                    SessionName = "Nurse Clinic",
                    SlotType = "Child Immunisation",
                    StartTime = DateTime.Now.ToString("t")
                }
            };
            return results;
        }
    }
}
