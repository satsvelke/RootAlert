using System;
using Microsoft.Extensions.Logging;
using RootAlert.Alerts;
using RootAlert.Processing;

namespace RootAlert.Logger
{
    public class RootAlertLogger : ILogger
    {
        private readonly IAlertService _alertService;
        private readonly RootAlertProcessor _processor;
        private readonly ILogger _logger;

        public RootAlertLogger(IAlertService alertService, RootAlertProcessor processor, ILogger logger)
        {
            _alertService = alertService;
            _processor = processor;
            _logger = logger;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Error || logLevel == LogLevel.Critical;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (exception != null)
            {
                _logger.LogError(exception, "RootAlert captured an exception."); // ✅ Use injected logger
                _processor.AddToBatch(exception, null);

                if (_processor.ShouldSendBatch())
                {
                    string batchMessage = _processor.GetBatchSummary();
                    if (!string.IsNullOrEmpty(batchMessage))
                    {
                        _alertService.SendAlertAsync(batchMessage).Wait();
                    }
                }
            }
        }
    }
}
