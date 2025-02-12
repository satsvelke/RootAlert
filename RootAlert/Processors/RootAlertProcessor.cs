using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RootAlert.Alerts;
using RootAlert.Config;
using RootAlert.Storage;

namespace RootAlert.Processing
{
    public class RootAlertProcessor : IDisposable
    {
        private readonly ILogger<RootAlertProcessor> _logger;
        private readonly TimeSpan _batchInterval;
        private readonly IAlertService _alertService;
        private readonly IRootAlertStorage _rootAlertStorage;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private Task? _processingTask;

        public RootAlertProcessor(ILogger<RootAlertProcessor> logger, RootAlertSetting rootAlertSetting, IAlertService alertService, IRootAlertStorage rootAlertStorage)
        {
            _logger = logger;
            _batchInterval = rootAlertSetting.BatchInterval;
            _alertService = alertService;
            _rootAlertStorage = rootAlertStorage;

            _processingTask = Task.Factory.StartNew(
                                () => StartProcessingAsync(_cancellationTokenSource.Token),
                                TaskCreationOptions.LongRunning
                            );
        }

        public void AddToBatch(Exception exception, HttpContext context)
        {
            _rootAlertStorage.AddToBatchAsync(exception, context);
        }

        private async Task StartProcessingAsync(CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (stopwatch.Elapsed >= _batchInterval)
                {
                    await ProcessBatch();
                    stopwatch.Restart();
                }

                await Task.Delay(100, cancellationToken);
            }
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
            _cancellationTokenSource.Cancel();
            _processingTask?.Wait();
            _cancellationTokenSource.Dispose();
        }
    }
}
