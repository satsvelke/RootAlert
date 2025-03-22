using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RootAlert.Alerts;
using RootAlert.Config;
using RootAlert.Storage;

namespace RootAlert.Processing
{
    public class RootAlertProcessor : IDisposable
    {
        private readonly ILogger _logger;
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

            _processingTask = Task.Run(() => StartProcessingAsync(_cancellationTokenSource.Token));
        }

        public void AddToBatch(Exception exception, HttpContext context)
        {

            var requestInfo = new RequestInfo(
                context.Request.Path.ToString(),
                context.Request.Method,
                JsonSerializer.Serialize(context.Request.Headers)
            );

            _rootAlertStorage.AddToBatchAsync(exception, requestInfo);
        }

        private async Task StartProcessingAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(_batchInterval, cancellationToken);
                    await ProcessBatch();
                }
            }
            catch (TaskCanceledException)
            {
                _logger.LogInformation("Batch processing task was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in the batch processing loop.");
            }
        }

        private async Task ProcessBatch()
        {
            try
            {
                var errorBatch = await _rootAlertStorage.GetBatchAsync();

                if (errorBatch.Count == 0) return;

                _logger.LogInformation($"🚀 Sending batched alert with {errorBatch.Count} errors.");
                await _alertService.SendBatchAlertAsync(errorBatch);
                await _rootAlertStorage.ClearBatchAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process the error batch.");
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            try
            {
                _processingTask?.Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var inner in ex.InnerExceptions)
                {
                    _logger.LogError(inner, "Exception occurred while waiting for batch processing task to complete.");
                }
            }
            finally
            {
                _cancellationTokenSource.Dispose();
            }
        }
    }
}