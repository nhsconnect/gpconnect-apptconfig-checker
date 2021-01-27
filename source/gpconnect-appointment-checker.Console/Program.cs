using gpconnect_appointment_checker.Console.Helpers;
using gpconnect_appointment_checker.DTO.Request.GpConnect;
using gpconnect_appointment_checker.DTO.Response.Application;
using gpconnect_appointment_checker.DTO.Response.Configuration;
using gpconnect_appointment_checker.DTO.Response.GpConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Novell.Directory.Ldap;
using Npgsql;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace gpconnect_appointment_checker.Console
{
    class Program
    {
        static string _connectionstring;
        static List<LdapQuery> _ldapQueries;
        static SpineConfiguration _spineConfiguration;
        static X509Certificate _clientCertificate;

        static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                System.Console.WriteLine("Please pass the first argument as DB connection string in the format Server=PG_HOST;Port=PG_PORT;Database=PG_DB;User Id=PG_USER;Password=PG_PASS");
                Environment.Exit(-1);
            }

            _connectionstring = args.First();
            _ldapQueries = GetLdapQueries();
            _spineConfiguration = GetSpineConfiguration();

            System.Console.WriteLine($"Using DB connection string: {_connectionstring}");

            var providerCodes = new List<string> { "A20047", "X26", "J82132", "B82619", "B82617", "B82614", "RR8", "G82809", "RYEA3", "G82796", "G82719", "RX8", "M84040", "B85033" };
            var consumerCodes = new List<string> { "A20047", "X26", "J82132", "B82619", "B82617", "B82614", "RR8", "G82809", "RYEA3", "G82796", "G82719", "RX8", "M84040", "B85033" };

            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var providerOrganisationResults = RunParallel<Organisation>(providerCodes, "GetOrganisationDetailsByOdsCode");
            var consumerOrganisationResults = RunParallel<Organisation>(consumerCodes, "GetOrganisationDetailsByOdsCode");
            var providerEndpointResults = RunParallel<Spine>(providerCodes, "GetGpProviderEndpointAndPartyKeyByOdsCode");
            var consumerEndpointResults = RunParallel<Spine>(consumerCodes, "GetGpProviderEndpointAndPartyKeyByOdsCode");
            var capabilityStatementResults = RunParallelAPICall(providerCodes, "GetGpProviderAsIdByOdsCodeAndPartyKey");
            stopWatch.Stop();

            System.Console.WriteLine("Provider Organisation Details");
            System.Console.WriteLine("=============================");

            for (var i = 0; i < providerOrganisationResults.Count; i++)
            {
                System.Console.WriteLine($"[{i + 1}] {providerOrganisationResults[i]?.ODSCode} {providerOrganisationResults[i]?.OrganisationTypeCode} {providerOrganisationResults[i]?.OrganisationName} {providerOrganisationResults[i]?.PostalAddress} {providerOrganisationResults[i]?.PostalCode}");
            }

            System.Console.WriteLine("Consumer Organisation Details");
            System.Console.WriteLine("=============================");

            for (var i = 0; i < consumerOrganisationResults.Count; i++)
            {
                System.Console.WriteLine($"[{i + 1}] {consumerOrganisationResults[i]?.ODSCode} {consumerOrganisationResults[i]?.OrganisationTypeCode} {consumerOrganisationResults[i]?.OrganisationName} {consumerOrganisationResults[i]?.PostalAddress} {consumerOrganisationResults[i]?.PostalCode}");
            }

            System.Console.WriteLine("Provider Endpoint Details");
            System.Console.WriteLine("=========================");

            for (var i = 0; i < providerEndpointResults.Count; i++)
            {
                System.Console.WriteLine($"[{i + 1}] {providerEndpointResults[i]?.asid} {providerEndpointResults[i]?.ssp_hostname}");
            }

            System.Console.WriteLine("Consumer Endpoint Details");
            System.Console.WriteLine("=========================");

            for (var i = 0; i < consumerEndpointResults.Count; i++)
            {
                System.Console.WriteLine($"[{i + 1}] {consumerEndpointResults[i]?.asid} {consumerEndpointResults[i]?.ssp_hostname}");
            }

            System.Console.WriteLine("Capability Statement Details");
            System.Console.WriteLine("============================");

            for (var i = 0; i < capabilityStatementResults.Count; i++)
            {
                System.Console.WriteLine($"[{i + 1}] {capabilityStatementResults[i]?.Name} {capabilityStatementResults[i]?.Description} {capabilityStatementResults[i]?.ResourceType}");
            }

            System.Console.WriteLine($"{providerCodes.Count + consumerCodes.Count} ODS code(s) took {stopWatch.Elapsed.TotalSeconds} seconds");
        }

        public static List<T> RunParallel<T>(List<string> odsCodes, string queryName) where T : class
        {
            var processedCodes = new ConcurrentBag<T>();
            Parallel.ForEach(odsCodes, (odsCode) =>
                {
                    processedCodes.Add(RunLdapQuery<T>(odsCode, queryName));
                });
            return processedCodes.ToList();
        }

        public static List<CapabilityStatement> RunParallelAPICall(List<string> odsCodes, string queryName)
        {
            var processedCapabilityStatements = new ConcurrentBag<CapabilityStatement>();
            Parallel.ForEach(odsCodes, (odsCode) =>
            {
                processedCapabilityStatements.Add(ConstructApiCall(odsCode));
            });
            return processedCapabilityStatements.ToList();
        }

        private static CapabilityStatement ConstructApiCall(string odsCode)
        {
            CapabilityStatement result = null;
            var providerOrganisationDetails = RunLdapQuery<Organisation>(odsCode, "GetOrganisationDetailsByOdsCode");
            var consumerOrganisationDetails = RunLdapQuery<Organisation>(odsCode, "GetOrganisationDetailsByOdsCode");

            var providerGpConnectDetails = RunLdapQuery<Spine>(odsCode, "GetGpProviderEndpointAndPartyKeyByOdsCode");
            var consumerGpConnectDetails = RunLdapQuery<Spine>(odsCode, "GetGpProviderEndpointAndPartyKeyByOdsCode");

            var providerAsId = RunLdapQuery<Spine>(odsCode, "GetGpProviderAsIdByOdsCodeAndPartyKey", providerGpConnectDetails?.party_key);
            if (providerAsId != null)
            {
                providerGpConnectDetails.asid = providerAsId.asid;
                result = ExecuteApiCall(providerGpConnectDetails, providerOrganisationDetails, consumerGpConnectDetails, consumerOrganisationDetails);
            }
            return result;
        }

        private static CapabilityStatement ExecuteApiCall(Spine providerGpConnectDetails, Organisation providerOrganisationDetails, Spine consumerGpConnectDetails, Organisation consumerOrganisationDetails)
        {
            var userGuid = Guid.NewGuid().ToString();
            var tokenHandler = new JwtSecurityTokenHandler
            {
                SetDefaultTimesOnTokenCreation = false
            };

            var tokenIssuer = "ldap.gov.uk";
            var tokenAudience = providerGpConnectDetails.ssp_hostname;
            var tokenIssuedAt = DateTimeOffset.Now;
            var tokenExpiration = DateTimeOffset.Now.AddMinutes(5);

            var tokenDescriptor = BuildSecurityTokenDescriptor(tokenIssuer, tokenAudience, userGuid, tokenIssuedAt, tokenExpiration);
            AddRequestingDeviceClaim(tokenDescriptor);
            AddRequestingOrganisationClaim(providerOrganisationDetails, tokenDescriptor);
            AddRequestingPractitionerClaim(tokenDescriptor, userGuid);

            var token = AddTokenHeader(tokenHandler, tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            var requestParameters = new RequestParameters
            {
                BearerToken = tokenString,
                SspFrom = "100000000001",
                SspTo = providerGpConnectDetails.asid,
                UseSSP = false,
                SspHostname = providerGpConnectDetails.ssp_hostname,
                ConsumerODSCode = consumerOrganisationDetails.ODSCode,
                ProviderODSCode = providerOrganisationDetails.ODSCode,
                InteractionId = "urn:nhs:names:services:gpconnect:fhir:rest:read:metadata-1",
                SpineMessageTypeId = 2
            };

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/fhir+json"));
                client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };

                AddRequiredRequestHeaders(requestParameters, client);
                using var request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"{AddSecureSpineProxy(providerGpConnectDetails.ssp_hostname, requestParameters)}/metadata")
                };

                var response = client.SendAsync(request).Result;
                var responseStream = response.Content.ReadAsStringAsync().Result;

                var result = JsonConvert.DeserializeObject<CapabilityStatement>(responseStream);
                return result;
            }
        }

        private static void AddRequestingOrganisationClaim(Organisation organisationDetails,
            SecurityTokenDescriptor tokenDescriptor)
        {
            tokenDescriptor.Claims.Add("requesting_organization", new RequestingOrganisation
            {
                resourceType = "Organization",
                name = "Organisation Name",
                identifier = new List<Identifier>
                {
                    new Identifier
                    {
                        system = "https://fhir.nhs.uk/Id/ods-organization-code",
                        value = organisationDetails.ODSCode
                    }
                }
            });
        }

        private static void AddRequestingDeviceClaim(SecurityTokenDescriptor tokenDescriptor)
        {
            tokenDescriptor.Claims.Add("requesting_device", new RequestingDevice
            {
                resourceType = "Device",
                model = "Product Name",
                version = "Product Version",
                identifier = new List<Identifier>
                {
                    new Identifier
                    {
                        system = "https://appointmentchecker.gpconnect.nhs.uk/Id/device-identifier",
                        value = "UNK"
                    }
                }
            });
        }

        private static void AddRequestingPractitionerClaim(SecurityTokenDescriptor tokenDescriptor, string userGuid)
        {
            tokenDescriptor.Claims.Add("requesting_practitioner", new RequestingPractitioner
            {
                resourceType = "Practitioner",
                id = userGuid,
                name = new List<Name>
                {
                    new Name
                    {
                        family = "Family Name",
                        given = new List<string> { "Given Name" }
                    }
                },
                identifier = new List<Identifier>
                {
                    new Identifier
                    {
                        system = "https://fhir.nhs.uk/Id/sds-user-id",
                        value = "UNK"
                    },
                    new Identifier
                    {
                        system = "https://fhir.nhs.uk/Id/sds-role-profile-id",
                        value = "UNK"
                    },
                    new Identifier
                    {
                        system = "https://appointmentchecker.gpconnect.nhs.uk/Id/email-address",
                        value = "Email Address"
                    },
                    new Identifier
                    {
                        system = "https://appointmentchecker.gpconnect.nhs.uk/Id/nhsmail-sid",
                        value = "Sid"
                    }
                }
            });
        }

        private static void AddRequiredRequestHeaders(RequestParameters requestParameters, HttpClient client)
        {
            client.DefaultRequestHeaders.Add("Ssp-From", requestParameters.SspFrom);
            client.DefaultRequestHeaders.Add("Ssp-To", requestParameters.SspTo);
            client.DefaultRequestHeaders.Add("Ssp-InteractionID", requestParameters.InteractionId);
            client.DefaultRequestHeaders.Add("Ssp-TraceID", Guid.NewGuid().ToString());
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", requestParameters.BearerToken);
        }

        private static string AddSecureSpineProxy(string baseAddress, RequestParameters requestParameters)
        {
            return requestParameters.UseSSP ? AddScheme(requestParameters.SspHostname) + "/" + baseAddress : baseAddress;
        }

        private static string AddScheme(string sspHostname)
        {
            return !sspHostname.StartsWith("https://") ? "https://" + sspHostname : sspHostname;
        }

        private static JwtSecurityToken AddTokenHeader(JwtSecurityTokenHandler tokenHandler, SecurityTokenDescriptor tokenDescriptor)
        {
            var token = tokenHandler.CreateJwtSecurityToken(tokenDescriptor);
            return token;
        }

        private static SecurityTokenDescriptor BuildSecurityTokenDescriptor(string tokenIssuer, string tokenAudience,
            string userGuid, DateTimeOffset tokenIssuedAt, DateTimeOffset tokenExpiration)
        {
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = tokenIssuer,
                Audience = tokenAudience,
                Claims = new Dictionary<string, object>()
                {
                    {GPConnect.Constants.TokenRequestValues.ReasonForRequestKey, GPConnect.Constants.TokenRequestValues.ReasonForRequestValue},
                    {GPConnect.Constants.TokenRequestValues.RequestedScopeKey, GPConnect.Constants.TokenRequestValues.RequestedScopeValue},
                    {GPConnect.Constants.TokenRequestValues.TokenSubject, userGuid}
                },
                IssuedAt = tokenIssuedAt.DateTime,
                Expires = tokenExpiration.DateTime
            };
            return tokenDescriptor;
        }

        private static T RunLdapQuery<T>(string odsCode, string queryName, string partyKey = "") where T : class
        {
            var query = _ldapQueries.FirstOrDefault(x => x.query_name == queryName);
            T result = null;

            using (var ldapConnection = new LdapConnection
            {
                SecureSocketLayer = _spineConfiguration.sds_use_ldaps,
                ConnectionTimeout = _spineConfiguration.timeout_seconds * 1000
            })
            {
                if (_spineConfiguration.sds_use_mutualauth)
                {
                    System.Console.WriteLine("Using Mutual Auth");

                    var clientCertData = CertificateHelper.ExtractCertInstances(_spineConfiguration.client_cert);
                    var clientPrivateKeyData = CertificateHelper.ExtractKeyInstance(_spineConfiguration.client_private_key);
                    var x509ClientCertificate = new X509Certificate2(clientCertData.FirstOrDefault());

                    var privateKey = RSA.Create();
                    privateKey.ImportRSAPrivateKey(clientPrivateKeyData, out _);
                    var x509CertificateWithPrivateKey = x509ClientCertificate.CopyWithPrivateKey(privateKey);
                    var pfxFormattedCertificate = new X509Certificate2(x509CertificateWithPrivateKey.Export(X509ContentType.Pfx, string.Empty), string.Empty);

                    _clientCertificate = pfxFormattedCertificate;

                    ldapConnection.UserDefinedServerCertValidationDelegate += ValidateServerCertificateChain;
                    ldapConnection.UserDefinedClientCertSelectionDelegate += SelectLocalCertificate;
                }

                ldapConnection.Connect(_spineConfiguration.sds_hostname, _spineConfiguration.sds_port);
                ldapConnection.Bind(string.Empty, string.Empty);

                var searchResults = RunSearch(ldapConnection, query, odsCode, partyKey);

                var jsonDictionary = JsonConvert.SerializeObject(searchResults);
                if (searchResults.Count > 0)
                {
                    result = JsonConvert.DeserializeObject<T>(jsonDictionary);
                }

                ldapConnection.Disconnect();
                ldapConnection.Dispose();
            }
            return result;
        }

        private static bool ValidateServerCertificateChain(object sender, X509Certificate pfxFormattedCertificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            System.Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            var serverCert = _spineConfiguration.server_ca_certchain;
            var serverCertData = CertificateHelper.ExtractCertInstances(serverCert);
            var x509ServerCertificateSubCa = new X509Certificate2(serverCertData[0]);
            var x509ServerCertificateRootCa = new X509Certificate2(serverCertData[1]);

            chain.Reset();
            chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreRootRevocationUnknown;
            chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            chain.ChainPolicy.ExtraStore.Add(x509ServerCertificateSubCa);
            chain.ChainPolicy.ExtraStore.Add(x509ServerCertificateRootCa);

            //if (chain.Build(pfxFormattedCertificate)) return true;
            if (chain.ChainStatus.Where(chainStatus => chainStatus.Status != X509ChainStatusFlags.NoError).All(chainStatus => chainStatus.Status != X509ChainStatusFlags.UntrustedRoot)) return false;
            var providedRoot = chain.ChainElements[^1];
            return x509ServerCertificateRootCa.Thumbprint == providedRoot.Certificate.Thumbprint;
        }

        private static X509Certificate SelectLocalCertificate(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return _clientCertificate;
        }

        private static Dictionary<string, object> RunSearch(LdapConnection ldapConnection, LdapQuery ldapQuery, string odsCode, string partyKey = "")
        {
            var results = new Dictionary<string, object>();
            var filter = ldapQuery.query_text.Replace("{odsCode}", odsCode).Replace("{partyKey}", partyKey);
            var searchResults = ldapConnection.Search(
                ldapQuery.search_base,
                LdapConnection.ScopeSub,
                filter,
                ldapQuery.query_attributes,
                false);

            while (searchResults.HasMore())
            {
                var nextEntry = searchResults.Next();
                var attributeSet = nextEntry.GetAttributeSet();

                foreach (var attribute in attributeSet)
                {
                    results.TryAdd(attribute.Name, attribute.StringValue);
                }
            }
            return results;
        }

        private static List<LdapQuery> GetLdapQueries()
        {
            var results = new List<LdapQuery>();

            using NpgsqlConnection connection = new NpgsqlConnection(_connectionstring);
            {
                var command = new NpgsqlCommand("configuration.get_sds_queries", connection);
                command.CommandType = CommandType.StoredProcedure;
                connection.Open();

                var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    results.Add(new LdapQuery
                    {
                        query_name = reader.GetString("query_name"),
                        search_base = reader.GetString("search_base"),
                        query_text = reader.GetString("query_text"),
                        query_attributes = reader.GetString("query_attributes").Split(",")
                    });
                }

                connection.Close();
            }
            return results;
        }

        private static SpineConfiguration GetSpineConfiguration()
        {
            var result = new SpineConfiguration();

            using NpgsqlConnection connection = new NpgsqlConnection(_connectionstring);
            {
                var command = new NpgsqlCommand("configuration.get_spine_configuration", connection);
                command.CommandType = CommandType.StoredProcedure;
                connection.Open();

                var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    result = new SpineConfiguration
                    {
                        use_ssp = reader.GetBoolean("use_ssp"),
                        ssp_hostname = reader.GetNullableString("ssp_hostname"),
                        sds_hostname = reader.GetString("sds_hostname"),
                        sds_port = reader.GetInt32("sds_port"),
                        sds_use_ldaps = reader.GetBoolean("sds_use_ldaps"),
                        party_key = reader.GetString("party_key"),
                        asid = reader.GetString("asid"),
                        organisation_id = reader.GetInt32("organisation_id"),
                        timeout_seconds = reader.GetInt32("timeout_seconds"),
                        client_cert = reader.GetNullableString("client_cert"),
                        client_private_key = reader.GetNullableString("client_private_key"),
                        server_ca_certchain = reader.GetNullableString("server_ca_certchain"),
                        sds_use_mutualauth = reader.GetBoolean("sds_use_mutualauth"),
                        spine_fqdn = reader.GetString("spine_fqdn")
                    };
                }

                connection.Close();
            }
            return result;
        }
    }

    internal static class NgpsqlDataReaderExtensionMethods
    {
        public static string GetNullableString(this NpgsqlDataReader reader, string columnName)
        {
            if (reader.IsDBNull(columnName))
                return null;

            return reader.GetString(columnName);
        }
    }

    internal class LdapQuery
    {
        public string query_name { get; set; }
        public string search_base { get; set; }
        public string query_text { get; set; }
        public string[] query_attributes { get; set; }
    }

    internal class SpineConfiguration
    {
        public bool use_ssp { get; set; }
        public string ssp_hostname { get; set; }
        public string sds_hostname { get; set; }
        public int sds_port { get; set; }
        public bool sds_use_ldaps { get; set; }
        public int organisation_id { get; set; }
        public string party_key { get; set; }
        public string asid { get; set; }
        public int timeout_seconds { get; set; }
        public string client_cert { get; set; }
        public string client_private_key { get; set; }
        public string server_ca_certchain { get; set; }
        public string spine_fqdn { get; set; }
        public bool sds_use_mutualauth { get; set; }
    }
}
