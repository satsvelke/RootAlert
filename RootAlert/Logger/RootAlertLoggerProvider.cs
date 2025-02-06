using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RootAlert.Alerts;
using RootAlert.Logger;
using RootAlert.Processing;

namespace RootAlert.Logging
{
    public class RootAlertLoggerProvider : ILoggerProvider
    {
        private readonly IAlertService _alertService;
        private readonly RootAlertProcessor _processor;
        private readonly ILoggerFactory _loggerFactory;

        public RootAlertLoggerProvider(IAlertService alertService, RootAlertProcessor processor, ILoggerFactory loggerFactory)
        {
            _alertService = alertService;
            _processor = processor;
            _loggerFactory = loggerFactory; // ✅ Fix circular dependency
        }

        public ILogger CreateLogger(string categoryName)
        {
            // ✅ Create a logger inside instead of injecting ILogger<T>
            var logger = _loggerFactory.CreateLogger(categoryName);
            return new RootAlertLogger(_alertService, _processor, logger);
        }

        public void Dispose() { }
    }
}

