using System;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using RootAlert.Config;
using RootAlert.Hashing;
using RootAlert.Processing;

namespace RootAlert.Storage.Memory;

public class MemoryAlertStorage : IRootAlertStorage
{

    private static readonly ConcurrentDictionary<string, (int Count, Exception exception, RequestInfo requestInfo)> _errorBatch = new();
    private static readonly object _lock = new();

    public async Task AddToBatchAsync(Exception exception, HttpContext context)
    {
        string errorKey = await HashGenerator.GenerateErrorHash(exception);

        var requestInfo = new RequestInfo(
            context.Request.Path,
            context.Request.Method,
            context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
        );

        lock (_lock)
        {
            if (_errorBatch.ContainsKey(errorKey))
            {
                _errorBatch[errorKey] = (_errorBatch[errorKey].Count + 1, exception, requestInfo);
            }
            else
            {
                _errorBatch[errorKey] = (1, exception, requestInfo);
            }
        }
    }

    public Task ClearBatchAsync()
    {
        return Task.Run(() => _errorBatch.Clear());
    }

    public Task<List<(int Count, Exception Exception, RequestInfo RequestInfo)>> GetBatchAsync()
    {
        return Task.Run(() => _errorBatch.Values.ToList());
    }
}
