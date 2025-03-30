using System.Collections.Concurrent;
using RootAlert.Config;
using RootAlert.Hashing;

namespace RootAlert.Storage.Memory;

internal sealed class MemoryAlertStorage : IRootAlertStorage
{

    private static readonly ConcurrentDictionary<string, ErrorLogEntry> _errorBatch = new();
    private static readonly object _lock = new();

    public async Task AddToBatchAsync(Exception exception, RequestInfo requestInfo)
    {
        string errorKey = await HashGenerator.GenerateErrorHash(exception);

        var exceptionInfo = new ExceptionInfo(exception.Message, exception!.StackTrace ?? "No stack trace available.", exception!.GetType().Name);

        _errorBatch.AddOrUpdate(
            errorKey,
            key => new ErrorLogEntry
            {
                Count = 1,
                Exception = exceptionInfo,
                Request = requestInfo
            },
            (key, existingEntry) =>
            {
                existingEntry.Count += 1;
                return existingEntry;
            }
        );
    }

    public Task ClearBatchAsync()
    {
        _errorBatch.Clear();
        return Task.CompletedTask;
    }

    public Task<List<ErrorLogEntry>> GetBatchAsync()
    {
        return Task.FromResult(_errorBatch.Values.ToList());
    }
}
