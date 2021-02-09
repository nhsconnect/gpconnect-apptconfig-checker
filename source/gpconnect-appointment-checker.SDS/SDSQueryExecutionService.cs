using gpconnect_appointment_checker.DAL.Interfaces;
using gpconnect_appointment_checker.DTO.Request.Logging;
using gpconnect_appointment_checker.Helpers;
using gpconnect_appointment_checker.SDS.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Novell.Directory.Ldap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace gpconnect_appointment_checker.SDS
{
    public class SDSQueryExecutionService : ISDSQueryExecutionService
    {
        private static ILogger<SDSQueryExecutionService> _logger;
        private readonly ILogService _logService;
        private static IConfiguration _configuration;
        private readonly IHttpContextAccessor _context;
        private static X509Certificate _clientCertificate;
        private static bool _haveLoggedTlsVersion = false;

        public SDSQueryExecutionService(ILogger<SDSQueryExecutionService> logger, ILogService logService, IConfiguration configuration, IHttpContextAccessor context)
        {
            _logger = logger;
            _configuration = configuration;
            _logService = logService;
            _context = context;
        }

        public T ExecuteLdapQuery<T>(string searchBase, string filter, string[] attributes) where T : class
        {
            try
            {
                var sw = new Stopwatch();
                T result = null;
                sw.Start();
                var logMessage = new SpineMessage
                {
                    RequestPayload = $"{searchBase} {filter} [{string.Join(",", attributes)}]",
                    SpineMessageTypeId = (int)GPConnect.Constants.SpineMessageTypes.SpineLdapQuery
                };

                var results = new Dictionary<string, object>();
                var useLdaps = bool.Parse(_configuration.GetSection("Spine:sds_use_ldaps").Value);
                var useSdsMutualAuth = bool.Parse(_configuration.GetSection("Spine:sds_use_mutualauth").Value);

                // temporarily test downgrade to TLS1.2
                Novell.Directory.Ldap.LdapConnectionOptions ldapConnectionOptions = new LdapConnectionOptions();

                if (useLdaps)
                {
                    ldapConnectionOptions.ConfigureSslProtocols(System.Security.Authentication.SslProtocols.Tls12);
                    ldapConnectionOptions.UseSsl();
                    ldapConnectionOptions.ConfigureLocalCertificateSelectionCallback(SelectLocalCertificate);
                    ldapConnectionOptions.ConfigureRemoteCertificateValidationCallback(ValidateServerCertificate);
                }
                // end

                using (var ldapConnection = new LdapConnection(ldapConnectionOptions)
                {
//                    SecureSocketLayer = useLdaps,
                    ConnectionTimeout = int.Parse(_configuration.GetSection("Spine:timeout_seconds").Value) * 1000
                })
                {

                    if (useSdsMutualAuth)
                    {
                        var clientCert = _configuration.GetSection("spine:client_cert").GetConfigurationString();
                        var serverCert = _configuration.GetSection("spine:server_ca_certchain").GetConfigurationString();
                        var clientPrivateKey = _configuration.GetSection("spine:client_private_key").GetConfigurationString();

                        var clientCertData = CertificateHelper.ExtractCertInstances(clientCert);
                        var clientPrivateKeyData = CertificateHelper.ExtractKeyInstance(clientPrivateKey);
                        var x509ClientCertificate = new X509Certificate2(clientCertData.FirstOrDefault());

                        var privateKey = RSA.Create();
                        privateKey.ImportRSAPrivateKey(clientPrivateKeyData, out _);
                        var x509CertificateWithPrivateKey = x509ClientCertificate.CopyWithPrivateKey(privateKey);
                        var pfxFormattedCertificate = new X509Certificate(x509CertificateWithPrivateKey.Export(X509ContentType.Pfx, string.Empty), string.Empty);

                        _clientCertificate = pfxFormattedCertificate;

//                        ldapConnection.UserDefinedServerCertValidationDelegate += ValidateServerCertificate;
//                        ldapConnection.UserDefinedClientCertSelectionDelegate += SelectLocalCertificate;
                    }

                    var hostName = _configuration.GetSection("Spine:sds_hostname").Value;
                    var hostPort = int.Parse(_configuration.GetSection("Spine:sds_port").Value);

                    ldapConnection.Connect(hostName, hostPort);
                    ldapConnection.Bind(string.Empty, string.Empty);

                    LogTlsVersionOnStartup(ldapConnection);

                    var searchResults = ldapConnection.Search(searchBase, LdapConnection.ScopeSub, filter, attributes, false);

                    while (searchResults.HasMore())
                    {
                        var nextEntry = searchResults.Next();
                        var attributeSet = nextEntry.GetAttributeSet();

                        foreach (var attribute in attributeSet)
                        {
                            results.TryAdd(attribute.Name, attribute.StringValue);
                        }
                    }

                    ldapConnection.Disconnect();
                    ldapConnection.Dispose();
                }

                var jsonDictionary = JsonConvert.SerializeObject(results);
                if (results.Count > 0)
                {
                    result = JsonConvert.DeserializeObject<T>(jsonDictionary);
                }
                logMessage.ResponsePayload = jsonDictionary;
                logMessage.RoundTripTimeMs = sw.ElapsedMilliseconds;
                _logService.AddSpineMessageLog(logMessage);
                return result;

            }
            catch (LdapException ldapException)
            {
                _logger.LogError(ldapException, "An LdapException has occurred while attempting to execute an LDAP query");
                throw;
            }
            catch (Exception exc)
            {
                _logger.LogError(exc, "An Exception has occurred while attempting to execute an LDAP query");
                throw;
            }
        }

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)  // skip this for now; logged as ticket to sort
                return true;

            _logger.LogError($"An error has occurred while attempting to validate the LDAP server certificate: {sslPolicyErrors}");
            return true;
        }

        private static X509Certificate SelectLocalCertificate(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return _clientCertificate;
        }

        private static void LogTlsVersionOnStartup(LdapConnection ldapConnection)
        {
            if (_haveLoggedTlsVersion)
                return;

            try
            {
                string tlsVersion = GetTlsVersionInUse(ldapConnection);
                _logger.LogInformation($"LDAP TLS version in use: {tlsVersion}");
            }
            finally
            {
                _haveLoggedTlsVersion = true;
            }
        }

        private static string GetTlsVersionInUse(LdapConnection ldapConnection)
        {
            try
            {
                System.Reflection.PropertyInfo propertyInfo = typeof(LdapConnection).GetProperty("Connection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var connection = propertyInfo.GetValue(ldapConnection);

                System.Reflection.FieldInfo fieldInfo = connection.GetType().GetField("_inStream", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                System.IO.Stream stream = (System.IO.Stream)fieldInfo.GetValue(connection);

                SslStream sslStream = stream as SslStream;

                if (sslStream == null)
                    return "TLS not enabled";

                return sslStream.SslProtocol.ToString();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting LDAP TLS version");
                return "Error getting LDAP TLS version";
            }
        }
    }
}
