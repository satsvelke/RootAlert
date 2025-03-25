
using Microsoft.Extensions.Logging;
using RootAlert.Config;

namespace RootAlert.Alerts
{
    internal sealed class EmailAlertService : IAlertService
    {
        private readonly ILogger<EmailAlertService> _logger;

        public EmailAlertService(ILogger<EmailAlertService> logger, RootAlertSetting rootAlertSetting)
        {
            _logger = logger;
        }

        public Task SendBatchAlertAsync(IList<ErrorLogEntry> errors)
        {
            throw new NotImplementedException();
        }
    }
}
