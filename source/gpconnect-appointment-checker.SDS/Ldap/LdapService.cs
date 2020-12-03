﻿using gpconnect_appointment_checker.DAL.Interfaces;
using gpconnect_appointment_checker.DTO.Response.Application;
using gpconnect_appointment_checker.DTO.Response.Configuration;
using gpconnect_appointment_checker.SDS.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Novell.Directory.Ldap;

namespace gpconnect_appointment_checker.SDS
{
    public class LdapService : ILdapService
    {
        private readonly ILogger<LdapService> _logger;
        private readonly ISDSQueryExecutionService _sdsQueryExecutionService;
        private readonly IConfiguration _configuration;
        private readonly IConfigurationService _configurationService;
        private readonly IApplicationService _applicationService;
        private readonly List<SdsQuery> _sdsQueries;

        public LdapService(ILogger<LdapService> logger, ISDSQueryExecutionService sdsQueryExecutionService, IConfiguration configuration, IConfigurationService configurationService, IApplicationService applicationService)
        {
            _logger = logger;
            _sdsQueryExecutionService = sdsQueryExecutionService;
            _configuration = configuration;
            _configurationService = configurationService;
            _applicationService = applicationService;
        }

        public Organisation GetOrganisationDetailsByOdsCode(string odsCode)
        {
            var sdsQuery = GetSdsQueryByName(Constants.LdapQuery.GetOrganisationDetailsByOdsCode);
            var filter = sdsQuery.QueryText.Replace("{odsCode}", Regex.Escape(odsCode));
            try
            {
                var results = _sdsQueryExecutionService.ExecuteLdapQuery<Organisation>(sdsQuery.SearchBase, filter);
                if (results != null)
                {
                    _applicationService.SynchroniseOrganisation(results);
                }
                return results;
            }
            catch (LdapException ldapException)
            {
                _logger.LogError("An LdapException error has occurred while attempting to execute an LDAP query", ldapException);
                throw;
            }
            catch (Exception exc)
            {
                _logger.LogError($"An error has occurred while attempting to execute an LDAP query - filter is {filter} - searchBase is {sdsQuery.SearchBase}", exc);
                throw;
            }
        }

        public Spine GetGpProviderEndpointAndPartyKeyByOdsCode(string odsCode)
        {
            try
            {
                var sdsQuery = GetSdsQueryByName(Constants.LdapQuery.GetGpProviderEndpointAndPartyKeyByOdsCode);
                var filter = sdsQuery.QueryText.Replace("{odsCode}", Regex.Escape(odsCode));
                var result = _sdsQueryExecutionService.ExecuteLdapQuery<Spine>(sdsQuery.SearchBase, filter);
                return result;
            }
            catch (LdapException ldapException)
            {
                _logger.LogError("An LdapException error has occurred while attempting to execute an LDAP query", ldapException);
                throw;
            }
            catch (Exception exc)
            {
                _logger.LogError("An error has occurred while attempting to execute an LDAP query", exc);
                throw;
            }
        }

        public Spine GetGpProviderAsIdByOdsCodeAndPartyKey(string odsCode, string partyKey)
        {
            try
            {
                var sdsQuery = GetSdsQueryByName(Constants.LdapQuery.GetGpProviderAsIdByOdsCodeAndPartyKey);
                var filter = sdsQuery.QueryText.Replace("{odsCode}", Regex.Escape(odsCode)).Replace("{partyKey}", Regex.Escape(partyKey));
                var result = _sdsQueryExecutionService.ExecuteLdapQuery<Spine>(sdsQuery.SearchBase, filter);
                return result;
            }
            catch (LdapException ldapException)
            {
                _logger.LogError("An LdapException error has occurred while attempting to execute an LDAP query", ldapException);
                throw;
            }
            catch (Exception exc)
            {
                _logger.LogError("An error has occurred while attempting to execute an LDAP query", exc);
                throw;
            }
        }

        private SdsQuery GetSdsQueryByName(string queryName)
        {
            try
            {
                var sdsQueryList = _sdsQueries ?? _configurationService.GetSdsQueryConfiguration();
                return sdsQueryList.FirstOrDefault(x => x.QueryName == queryName);
            }
            catch (LdapException ldapException)
            {
                _logger.LogError("An LdapException error has occurred while attempting to execute an LDAP query", ldapException);
                throw;
            }
            catch (Exception exc)
            {
                _logger.LogError("An error has occurred while attempting to load LDAP queries from the database", exc);
                throw;
            }
        }
    }
}
