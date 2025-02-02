
using Microsoft.Extensions.Logging;
using Sherlog.Config;

namespace Sherlog.Alerts;

internal sealed class EmailAlertService : IAlertService
{

    private readonly EmailSettings _settings;
    private readonly ILogger<EmailAlertService> _logger;

    public EmailAlertService(EmailSettings settings, ILogger<EmailAlertService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public Task SendAlertAsync(string message)
    {
        throw new NotImplementedException();
    }
}
