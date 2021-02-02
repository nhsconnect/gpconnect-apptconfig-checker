using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using gpconnect_appointment_checker.DAL.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace gpconnect_appointment_checker.DAL
{
    public class DataService : IDataService
    {
        private readonly ILogger<DataService> _logger;
        private readonly IConfiguration _configuration;

        public DataService(IConfiguration configuration, ILogger<DataService> logger)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public List<T> ExecuteFunction<T>(string schemaName, string functionName) where T : class
        {
            try
            {
                using var connection = new NpgsqlConnection(_configuration.GetConnectionString(ConnectionStrings.DefaultConnection));
                var results = connection.Query<T>($"{schemaName}.{functionName}", null, commandType: System.Data.CommandType.StoredProcedure).AsList();
                return results;
            }
            catch (PostgresException exc)
            {
                _logger?.LogError(exc,
                    IsDuplicateKeyException(exc)
                        ? $"A duplicate key exception has occurred while attempting to execute the function {schemaName}.{functionName}"
                        : $"An error has occurred while attempting to execute the function {schemaName}.{functionName}");

                throw;
            }
        }

        public List<T> ExecuteFunction<T>(string schemaName, string functionName, DynamicParameters parameters) where T : class
        {
            try
            {
                using var connection = new NpgsqlConnection(_configuration.GetConnectionString(ConnectionStrings.DefaultConnection));
                var results = connection.Query<T>($"{schemaName}.{functionName}", parameters, commandType: System.Data.CommandType.StoredProcedure).AsList();
                return results;
            }
            catch (PostgresException exc)
            {
                _logger?.LogError(exc,
                    IsDuplicateKeyException(exc)
                        ? $"A duplicate key exception has occurred while attempting to execute the function {schemaName}.{functionName}"
                        : $"An error has occurred while attempting to execute the function {schemaName}.{functionName}");

                throw;
            }
        }

        public int ExecuteFunction(string schemaName, string functionName, DynamicParameters parameters)
        {
            try
            {
                using var connection = new NpgsqlConnection(_configuration.GetConnectionString(ConnectionStrings.DefaultConnection));
                var rowsInserted = connection.Execute($"{schemaName}.{functionName}", parameters, commandType: System.Data.CommandType.StoredProcedure);
                return rowsInserted;
            }
            catch (PostgresException exc)
            {
                _logger?.LogError(exc,
                    IsDuplicateKeyException(exc)
                        ? $"A duplicate key exception has occurred while attempting to execute the function {schemaName}.{functionName}"
                        : $"An error has occurred while attempting to execute the function {schemaName}.{functionName}");

                throw;
            }
        }

        public async Task<List<T>> ExecuteFunctionAsync<T>(string schemaName, string functionName, DynamicParameters parameters, CancellationToken cancellationToken) where T : class
        {
            try
            {
                await using var connection = new NpgsqlConnection(_configuration.GetConnectionString(ConnectionStrings.DefaultConnection));
                var results = (await connection.QueryAsync<T>($"{schemaName}.{functionName}", parameters, commandType: System.Data.CommandType.StoredProcedure)).AsList();
                return results;
            }
            catch (PostgresException exc)
            {
                _logger?.LogError(
                    IsDuplicateKeyException(exc)
                        ? $"A duplicate key exception has occurred while attempting to execute the function {schemaName}.{functionName}"
                        : $"An error has occurred while attempting to execute the function {schemaName}.{functionName}",
                    exc);
                throw;
            }
        }

        public async Task<int> ExecuteFunctionAsync(string schemaName, string functionName, DynamicParameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                await using var connection = new NpgsqlConnection(_configuration.GetConnectionString(ConnectionStrings.DefaultConnection));
                var rowsInserted = await connection.ExecuteAsync($"{schemaName}.{functionName}", parameters, commandType: System.Data.CommandType.StoredProcedure);
                return rowsInserted;
            }
            catch (PostgresException exc)
            {
                _logger?.LogError(
                    IsDuplicateKeyException(exc)
                        ? $"A duplicate key exception has occurred while attempting to execute the function {schemaName}.{functionName}"
                        : $"An error has occurred while attempting to execute the function {schemaName}.{functionName}",
                    exc);
                throw;
            }
        }

        protected bool IsDuplicateKeyException(PostgresException ex)
        {
            return ex.SqlState == "23505";
        }
    }
}
