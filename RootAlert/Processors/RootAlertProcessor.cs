using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RootAlert.Alerts;
using RootAlert.Config;
using RootAlert.Storage;

namespace RootAlert.Processing
{

    public class RootAlertProcessor : IDisposable
    {
        private static readonly object _lock = new();
        private readonly ILogger<RootAlertProcessor> _logger;
        private readonly TimeSpan _batchInterval;
        private readonly IAlertService _alertService;
        private readonly System.Timers.Timer _batchTimer;
        private readonly IRootAlertStorage _rootAlertStorage;

        public RootAlertProcessor(ILogger<RootAlertProcessor> logger, RootAlertSetting rootAlertSetting, IAlertService alertService, IRootAlertStorage rootAlertStorage)
        {
            _logger = logger;
            _batchInterval = rootAlertSetting.BatchInterval;
            _alertService = alertService;
            _rootAlertStorage = rootAlertStorage;

            _batchTimer = new System.Timers.Timer(_batchInterval.TotalMilliseconds);
            _batchTimer.Elapsed += async (sender, e) => await ProcessBatch();
            _batchTimer.AutoReset = true;
            _batchTimer.Start();
        }

        public void AddToBatch(Exception exception, HttpContext context)
        {
            _rootAlertStorage.AddToBatchAsync(exception, context);
        }

        private async Task ProcessBatch()
        {
            var errorBatch = await _rootAlertStorage.GetBatchAsync();

            if (errorBatch.Count == 0) return;

            _logger.LogInformation($"🚀 Sending batched alert with {errorBatch.Count} errors.");

            await _alertService.SendBatchAlertAsync(errorBatch);
            await _rootAlertStorage.ClearBatchAsync();
        }

        public void Dispose()
        {
            _batchTimer?.Stop();
            _batchTimer?.Dispose();
        }
    }
}
