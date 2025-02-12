using Microsoft.AspNetCore.Http;
using RootAlert.Config;

namespace RootAlert.Alerts
{
    public interface IAlertService
    {
        Task SendBatchAlertAsync(IList<ErrorLogEntry> errors);
    }
}
