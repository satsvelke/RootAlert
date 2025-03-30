using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RootAlert.Config;
using RootAlert.Storage;
using StackExchange.Redis;

namespace RootAlert.Redis
{
    public sealed class RedisAlertStorage : IRootAlertStorage
    {
        private readonly IDatabase _database;
        private const string _batchKey = "RootAlert:ErrorBatch";
        private readonly ILogger<RedisAlertStorage> _logger;

        public RedisAlertStorage(string redisConnectionString, ILogger<RedisAlertStorage>? logger = null)
        {
            var redis = ConnectionMultiplexer.Connect(redisConnectionString);
            _database = redis.GetDatabase();
            _logger = logger ?? NullLogger<RedisAlertStorage>.Instance;
        }

        public async Task AddToBatchAsync(Exception exception, RequestInfo requestInfo)
        {
            try
            {
                var exceptionInfo = new ExceptionInfo(exception.Message, exception!.StackTrace ?? "No stack trace available.", exception!.GetType().Name);

                var errorEntry = new ErrorLogEntry
                {
                    Count = 1,
                    Exception = exceptionInfo,
                    Request = requestInfo
                };

                await _database.ListRightPushAsync(_batchKey, JsonSerializer.Serialize(errorEntry));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RootAlert : Failed to add data");
            }
        }

        public async Task<List<ErrorLogEntry>> GetBatchAsync()
        {

            try
            {
                var errorBatch = new Dictionary<string, ErrorLogEntry>();
                var errorCount = await _database.ListLengthAsync(_batchKey);

                var errorEntries = await _database.ListRangeAsync(_batchKey, 0, errorCount - 1);

                foreach (var errorJson in errorEntries)
                {
                    if (!string.IsNullOrEmpty(errorJson))
                    {
                        var errorEntry = JsonSerializer.Deserialize<ErrorLogEntry>(errorJson!);
                        if (errorEntry != null)
                        {
                            string errorKey = $"{errorEntry.Exception?.Message}-{errorEntry.Exception?.StackTrace}";

                            if (errorBatch.ContainsKey(errorKey))
                            {
                                errorBatch[errorKey].Count += 1;
                            }
                            else
                            {
                                errorEntry.Count = 1;
                                errorBatch[errorKey] = errorEntry;
                            }
                        }
                    }
                }

                await _database.KeyDeleteAsync(_batchKey);

                return errorBatch.Values.ToList();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RootAlert : Failed to retrive data");
            }

            return new List<ErrorLogEntry>();
        }

        public async Task ClearBatchAsync()
        {
            try
            {
                await _database.KeyDeleteAsync(_batchKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RootAlert : Failed to delete data");
            }
        }
    }
}
