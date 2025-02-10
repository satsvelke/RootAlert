
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RootAlert.Config;
using RootAlert.Processing;

namespace RootAlert.Alerts;

internal sealed class EmailAlertService : IAlertService
{

    private readonly EmailSettings _settings;
    private readonly ILogger<EmailAlertService> _logger;

    public EmailAlertService(EmailSettings settings, ILogger<EmailAlertService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public Task SendAlertAsync(Exception exception, HttpContext httpContext)
    {
        throw new NotImplementedException();
    }

    public Task SendBatchAlertAsync(List<(int count, Exception exception, RequestInfo requestInfo)> errors)
    {
        throw new NotImplementedException();
    }
}
