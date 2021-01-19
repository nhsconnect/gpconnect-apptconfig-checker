using gpconnect_appointment_checker.Console.Helpers;
using Novell.Directory.Ldap;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
            try
            {
                if ((args == null) || (args.Length == 0))
                {
                    System.Console.WriteLine("Please pass the first argument as DB connection string in the format Server=PG_HOST;Port=PG_PORT;Database=PG_DB;User Id=PG_USER;Password=PG_PASS");
                    Environment.Exit(-1);
                }

                _connectionstring = args.First();

                System.Console.WriteLine($"Using DB connection string: {_connectionstring}");

                int numberOfGoes = 1;

                if (args.Length > 1)
                    int.TryParse(args.Skip(1).First(), out numberOfGoes);

                _ldapQueries = GetLdapQueries();
                _spineConfiguration = GetSpineConfiguration();

                System.Console.WriteLine($"Running {numberOfGoes} iteration(s).");
                RunLdapQueries(numberOfGoes);
            }
            catch (InterThreadException e)
            {
                System.Console.WriteLine($"An InterThreadException has occurred: {e}");
                throw;
            }
            catch (LdapException e)
            {
                System.Console.WriteLine($"An LdapException has occurred: {e}");
                throw;
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"An Exception has occurred: {e}");
                throw;
            }
        }

        private static async Task RunLdapQueries(int numberOfGoes)
        {
            var odsCodes = new List<string> { "A20047", "X26", "J82132", "B82619", "B82617", "B82614", "RR8" };

            var query = _ldapQueries.FirstOrDefault(x => x.query_name == "GetOrganisationDetailsByOdsCode");

            for (var i = 0; i < numberOfGoes; i++)
            {
                var results = new List<Dictionary<string, object>>();

                using (LdapConnection ldapConnection = new LdapConnection())
                {
                    ldapConnection.SecureSocketLayer = _spineConfiguration.sds_use_ldaps;
                    ldapConnection.ConnectionTimeout = _spineConfiguration.timeout_seconds * 1000;

                    if (_spineConfiguration.sds_use_mutualauth)
                    {
                        System.Console.WriteLine("Using Mutual Auth");

                        var clientCertData = CertificateHelper.ExtractCertInstances(_spineConfiguration.client_cert);
                        var clientPrivateKeyData = CertificateHelper.ExtractKeyInstance(_spineConfiguration.client_private_key);
                        var x509ClientCertificate = new X509Certificate2(clientCertData.FirstOrDefault());

                        var privateKey = RSA.Create();
                        privateKey.ImportRSAPrivateKey(clientPrivateKeyData, out _);
                        var x509CertificateWithPrivateKey = x509ClientCertificate.CopyWithPrivateKey(privateKey);
                        var pfxFormattedCertificate = new X509Certificate(x509CertificateWithPrivateKey.Export(X509ContentType.Pfx, string.Empty), string.Empty);

                        _clientCertificate = pfxFormattedCertificate;

                        ldapConnection.UserDefinedServerCertValidationDelegate += ValidateServerCertificate;
                        ldapConnection.UserDefinedClientCertSelectionDelegate += SelectLocalCertificate;
                    }

                    ldapConnection.Connect(_spineConfiguration.sds_hostname, _spineConfiguration.sds_port);

                    var batchSize = 5;
                    int numberOfBatches = (int)Math.Ceiling((double)odsCodes.Count / batchSize);

                    for (int j = 0; j < numberOfBatches; j++)
                    {
                        var currentOdsCodes = odsCodes.Skip(i * batchSize).Take(batchSize);
                        var tasks = currentOdsCodes.Select(odsCode => RunSearch(ldapConnection, query.search_base, query.query_text.Replace("{odsCode}", odsCode), query.query_attributes));
                        //results.AddRange(await Task.WhenAll(tasks));
                    }
                }
                //System.Console.WriteLine($"For query {filter} - iteration {i + 1}, result count is {results.Count}");
            }
        }

        private static Dictionary<string, object> RunSearch(LdapConnection ldapConnection, string searchBase, string queryText, string[] queryAttributes)
        {
            var results = new Dictionary<string, object>();
            var searchResults = ldapConnection.Search(searchBase, LdapConnection.ScopeSub, queryText, queryAttributes, false);
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

        private static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            System.Console.WriteLine("Certificate error: {0}", sslPolicyErrors);
            return true;
        }

        private static X509Certificate SelectLocalCertificate(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            return _clientCertificate;
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
                        query_attributes = new[] { reader.GetString("query_attributes") }
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

    public class LdapQuery
    {
        public string query_name { get; set; }
        public string search_base { get; set; }
        public string query_text { get; set; }
        public string[] query_attributes { get; set; }
    }

    public class SpineConfiguration
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
