using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RootAlert.Config;
using RootAlert.Storage;
using StackExchange.Redis;

namespace RootAlert.Redis
{
    public class RedisAlertStorage : IRootAlertStorage
    {

        private readonly IConnectionMultiplexer? _redis;
        private const string _batchKey = "RootAlert:ErrorBatch";
        private readonly ILogger<RedisAlertStorage> _logger;
        private readonly string _redisConnectionString;

        public RedisAlertStorage(string redisConnectionString, ILogger<RedisAlertStorage>? logger = null)
        {
            _redisConnectionString = redisConnectionString;
            _logger = logger ?? NullLogger<RedisAlertStorage>.Instance;

            try
            {
                _redis = ConnectionMultiplexer.Connect(redisConnectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RootAlert: Failed to connect to Redis");
            }
        }

        protected virtual IDatabase GetDatabase()
        {
            if (_redis == null)
            {
                throw new InvalidOperationException("Redis connection not established");
            }
            return _redis.GetDatabase();
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
                var database = GetDatabase();
                await database.ListRightPushAsync(_batchKey, JsonSerializer.Serialize(errorEntry));
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
                var database = GetDatabase();

                var errorBatch = new Dictionary<string, ErrorLogEntry>();
                var errorCount = await database.ListLengthAsync(_batchKey);

                var errorEntries = await database.ListRangeAsync(_batchKey, 0, errorCount - 1);

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

                await database.KeyDeleteAsync(_batchKey);

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
                var database = GetDatabase();
                await database.KeyDeleteAsync(_batchKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RootAlert : Failed to delete data");
            }
        }
    }
}
