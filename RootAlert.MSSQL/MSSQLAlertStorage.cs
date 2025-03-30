using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RootAlert.Config;
using RootAlert.Storage;

namespace RootAlert.MSSQL
{
    public sealed class MSSQLAlertStorage : IRootAlertStorage
    {
        private readonly string _connectionString;
        private readonly ILogger<MSSQLAlertStorage> _logger;

        public MSSQLAlertStorage(string connectionString, ILogger<MSSQLAlertStorage>? logger = null)
        {
            _connectionString = connectionString;
            _logger = logger ?? NullLogger<MSSQLAlertStorage>.Instance;
        }

        public async Task AddToBatchAsync(Exception exception, RequestInfo request)
        {

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
        IF EXISTS (SELECT 1 FROM RootAlertLogs 
                   WHERE ExceptionMessage = @Message 
                   AND RequestUrl = @RequestUrl 
                   AND HttpMethod = @HttpMethod)
        BEGIN
            UPDATE RootAlertLogs
            SET ErrorCount = ErrorCount + 1, Processed = 0, CreatedAt = GETUTCDATE()
            WHERE ExceptionMessage = @Message 
            AND RequestUrl = @RequestUrl 
            AND HttpMethod = @HttpMethod;
        END
        ELSE
        BEGIN
            INSERT INTO RootAlertLogs (ExceptionMessage, StackTrace, ExceptionName, 
                                       RequestUrl, HttpMethod, Headers, 
                                       ErrorCount, CreatedAt, Processed)
            VALUES (@Message, @StackTrace, @ExceptionName, 
                    @RequestUrl, @HttpMethod, @Headers, 
                    1, GETUTCDATE(), 0);
        END";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Message", exception.Message);
                command.Parameters.AddWithValue("@StackTrace", exception.StackTrace ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ExceptionName", exception.GetType().Name);
                command.Parameters.AddWithValue("@RequestUrl", request.Url);
                command.Parameters.AddWithValue("@HttpMethod", request.Method);
                command.Parameters.AddWithValue("@Headers", request.Headers ?? (object)DBNull.Value);

                await command.ExecuteNonQueryAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RootAlert : Failed to add data");
            }
        }



        public async Task ClearBatchAsync()
        {

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = "DELETE FROM RootAlertLogs";

                using var command = new SqlCommand(query, connection);
                await command.ExecuteNonQueryAsync();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RootAlert : Failed to clear data");
            }
        }


        public async Task<List<ErrorLogEntry>> GetBatchAsync()
        {

            try
            {
                var logs = new List<ErrorLogEntry>();

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var query = @"
        SELECT ExceptionMessage, StackTrace, ExceptionName, 
               RequestUrl, HttpMethod, Headers, 
               SUM(ErrorCount) AS TotalCount, CreatedAt
        FROM RootAlertLogs
        WHERE Processed = 0
        GROUP BY ExceptionMessage, StackTrace, ExceptionName, 
                 RequestUrl, HttpMethod, Headers, CreatedAt
        ORDER BY CreatedAt DESC";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    logs.Add(new ErrorLogEntry
                    {
                        Count = reader.GetInt32(6),  // Total Error Count

                        Exception = new ExceptionInfo(
                            reader.GetString(0),  // ExceptionMessage
                            reader.IsDBNull(1) ? string.Empty : reader.GetString(1),  // StackTrace
                            reader.IsDBNull(2) ? string.Empty : reader.GetString(2)   // ExceptionName
                        ),

                        Request = new RequestInfo(
                            reader.IsDBNull(3) ? string.Empty : reader.GetString(3),  // RequestUrl
                            reader.IsDBNull(4) ? string.Empty : reader.GetString(4),  // HttpMethod
                            reader.IsDBNull(5) ? string.Empty :
                                                reader.GetString(5) // Headers
                        )
                    });
                }

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RootAlert : Failed to retrive data");
            }

            return new List<ErrorLogEntry>();
        }

        private Dictionary<string, string> DeserializeHeaders(string json)
        {
            return string.IsNullOrWhiteSpace(json)
                ? new Dictionary<string, string>()
                : JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }
    }
}
