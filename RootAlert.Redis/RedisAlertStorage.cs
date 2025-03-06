using System.Text.Json;
using Microsoft.AspNetCore.Http;
using RootAlert.Config;
using RootAlert.Storage;
using StackExchange.Redis;

namespace RootAlert.Redis
{
    public class RedisAlertStorage : IRootAlertStorage
    {
        private readonly IDatabase _database;
        private const string _batchKey = "RootAlert:ErrorBatch";

        public RedisAlertStorage(string redisConnectionString)
        {
            var redis = ConnectionMultiplexer.Connect(redisConnectionString);
            _database = redis.GetDatabase();
        }

        public async Task AddToBatchAsync(Exception exception, HttpContext context)
        {

            var exceptionInfo = new ExceptionInfo(exception.Message, exception!.StackTrace ?? "No stack trace available.", exception!.GetType().Name);

            var errorEntry = new ErrorLogEntry
            {
                Count = 1,
                Exception = exceptionInfo,
                Request = new RequestInfo(
                    context.Request.Path,
                    context.Request.Method,
                    context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString()))
            };

            await _database.ListRightPushAsync(_batchKey, JsonSerializer.Serialize(errorEntry));
        }

        public async Task<List<ErrorLogEntry>> GetBatchAsync()
        {
            var errorBatch = new Dictionary<string, ErrorLogEntry>(); // Key: Message + StackTrace
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

        public async Task ClearBatchAsync()
        {
            await _database.KeyDeleteAsync(_batchKey);
        }
    }
}
