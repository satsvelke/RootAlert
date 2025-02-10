

using Microsoft.AspNetCore.Http;
using RootAlert.Processing;

namespace RootAlert.Alerts;

public interface IAlertService
{
    Task SendAlertAsync(Exception exception, HttpContext httpContext);
    Task SendBatchAlertAsync(List<(int count, Exception exception, RequestInfo requestInfo)> errors);
}
