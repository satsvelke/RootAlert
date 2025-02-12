
using Microsoft.AspNetCore.Http;
using RootAlert.Config;

namespace RootAlert.Storage
{
    public interface IRootAlertStorage
    {
        Task AddToBatchAsync(Exception exception, HttpContext context);
        Task<List<(int Count, Exception Exception, RequestInfo RequestInfo)>> GetBatchAsync();
        Task ClearBatchAsync();
    }
}
