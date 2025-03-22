
using RootAlert.Config;

namespace RootAlert.Storage
{
    public interface IRootAlertStorage
    {
        Task AddToBatchAsync(Exception exception, RequestInfo requestInfo);
        Task<List<ErrorLogEntry>> GetBatchAsync();
        Task ClearBatchAsync();
    }
}
