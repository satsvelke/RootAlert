using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RootAlert.Alerts;
using RootAlert.Config;
using RootAlert.Hashing;
using System.Collections.Concurrent;

namespace RootAlert.Processing
{
    public record RequestInfo(string Url, string Method, Dictionary<string, string> Headers);

    public class RootAlertProcessor : IDisposable
    {
        private readonly ILogger<RootAlertProcessor> _logger;
        private static readonly ConcurrentDictionary<string, (int Count, Exception exception, RequestInfo requestInfo)> _errorBatch = new();
        private static readonly object _lock = new();
        private readonly TimeSpan _batchInterval;
        private readonly IAlertService _alertService;
        private readonly System.Timers.Timer _batchTimer;

        public RootAlertProcessor(ILogger<RootAlertProcessor> logger, RootAlertSetting rootAlertSetting, IAlertService alertService)
        {
            _logger = logger;
            _batchInterval = rootAlertSetting.BatchInterval;
            _alertService = alertService;

            _batchTimer = new System.Timers.Timer(_batchInterval.TotalMilliseconds);
            _batchTimer.Elapsed += async (sender, e) => await ProcessBatch();
            _batchTimer.AutoReset = true;
            _batchTimer.Start();
        }

        public void AddToBatch(Exception exception, HttpContext context)
        {
            string errorKey = HashGenerator.GenerateErrorHash(exception);

            var requestInfo = new RequestInfo(
                context.Request.Path,
                context.Request.Method,
                context.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
            );

            lock (_lock)
            {
                if (_errorBatch.ContainsKey(errorKey))
                {
                    _errorBatch[errorKey] = (_errorBatch[errorKey].Count + 1, exception, requestInfo);
                }
                else
                {
                    _errorBatch[errorKey] = (1, exception, requestInfo);
                }
            }
        }

        private async Task ProcessBatch()
        {
            if (_errorBatch.Count == 0) return;

            List<(int count, Exception exception, RequestInfo requestInfo)> batchedErrors;

            lock (_lock)
            {
                batchedErrors = _errorBatch.Values.ToList();
                _errorBatch.Clear();
            }

            _logger.LogInformation($"🚀 Sending batched alert with {batchedErrors.Count} errors.");
            await _alertService.SendBatchAlertAsync(batchedErrors);
        }

        public void Dispose()
        {
            _batchTimer?.Stop();
            _batchTimer?.Dispose();
        }
    }
}
