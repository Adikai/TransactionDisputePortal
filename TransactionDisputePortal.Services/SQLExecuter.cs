using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using TransactionDisputePortal.Core.Interfaces;


namespace TransactionDisputePortal.Services
{
    public class SqlExecuter : ISqlExecuter
    {
        private readonly string _connectionString;
        private const int MaxRetryAttempts = 3;

        // Transient SQL Error Numbers (Timeouts, Deadlocks, Connection Reset, Azure Throttling)
        private static readonly HashSet<int> TransientErrorNumbers = new()
    {
        -2,    // Client Execution Timeout
        20,    // Encryption handshake failed
        64,    // Connection error during payload send/receive
        233,   // Connection initialization error
        1205,  // Deadlock victim
        10053, // Transport-level error (Connection broken by server)
        10054, // Connection forcibly closed by remote host
        10060, // Network connection timeout
        40197, // Service error processing request
        40501, // Service busy (Azure SQL Throttling)
        40613, // Database unavailable
        10928, // Resource limit reached (Azure SQL)
        10929  // Resource limit reached (Azure SQL)
    };

        public SqlExecuter(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(
            string sql,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(async connection =>
            {
                var command = new CommandDefinition(sql, param, commandType: commandType, cancellationToken: cancellationToken);
                return await connection.QueryAsync<T>(command);
            });
        }

        public async Task<T?> QueryFirstOrDefaultAsync<T>(
            string sql,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(async connection =>
            {
                var command = new CommandDefinition(sql, param, commandType: commandType, cancellationToken: cancellationToken);
                return await connection.QueryFirstOrDefaultAsync<T>(command);
            });
        }

        public async Task<int> ExecuteAsync(
            string sql,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            CancellationToken cancellationToken = default)
        {
            return await ExecuteWithRetryAsync(async connection =>
            {
                var command = new CommandDefinition(sql, param, commandType: commandType, cancellationToken: cancellationToken);
                return await connection.ExecuteAsync(command);
            });
        }

        private async Task<T> ExecuteWithRetryAsync<T>(Func<SqlConnection, Task<T>> operation)
        {
            int attempt = 0;

            while (true)
            {
                attempt++;
                try
                {
                   
                    await using var connection = new SqlConnection(_connectionString);
                    await connection.OpenAsync();

                    return await operation(connection);
                }
                catch (SqlException ex) when (IsTransient(ex) && attempt < MaxRetryAttempts)
                {
                    int delayMilliseconds = (int)(Math.Pow(2, attempt) * 100);
                    await Task.Delay(delayMilliseconds);
                }
            }
        }

        private static bool IsTransient(SqlException ex)
        {
            foreach (SqlError error in ex.Errors)
            {
                if (TransientErrorNumbers.Contains(error.Number))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
