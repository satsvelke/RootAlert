using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RootAlert.Config;
using RootAlert.Hashing;
using System.Collections.Concurrent;
using System.Text;

namespace RootAlert.Processing
{
    public class RootAlertProcessor
    {
        private readonly ILogger<RootAlertProcessor> _logger;
        private static readonly ConcurrentDictionary<string, (int Count, string FormattedLog)> _errorBatch = new();
        private static readonly object _lock = new();
        private static DateTime _lastBatchSent = DateTime.UtcNow;
        private static DateTime? _firstErrorTime = null;
        private readonly TimeSpan _batchInterval;

        public RootAlertProcessor(ILogger<RootAlertProcessor> logger, List<RootAlertOptions> optionsList)
        {
            _logger = logger;
            _batchInterval = optionsList[0].BatchInterval; // ✅ Use first option’s batch interval (or customize this)
        }

        public string FormatLog(Exception exception, HttpContext context)
        {
            var logBuilder = new StringBuilder();
            var errorKey = HashGenerator.GenerateErrorHash(exception);

            logBuilder.AppendLine($"🆔 Error ID: {errorKey}");
            logBuilder.AppendLine($"⏳ Timestamp: {DateTime.UtcNow:MM/dd/yyyy h:mm:ss tt}");
            logBuilder.AppendLine("----------------------------------------------------");

            logBuilder.AppendLine("🌐 REQUEST DETAILS");
            logBuilder.AppendLine($"🔗 URL: {context.Request.Path}");
            logBuilder.AppendLine($"📡 HTTP Method: {context.Request.Method}");
            logBuilder.AppendLine("----------------------------------------------------");

            logBuilder.AppendLine("📩 REQUEST HEADERS");
            foreach (var header in context.Request.Headers)
            {
                logBuilder.AppendLine($"📝 {header.Key}: {header.Value}");
            }
            logBuilder.AppendLine("----------------------------------------------------");

            logBuilder.AppendLine("⚠️ EXCEPTION DETAILS");
            logBuilder.AppendLine($"❗ Type: {exception.GetType().Name}");
            logBuilder.AppendLine($"💬 Message: {exception.Message}");
            logBuilder.AppendLine("----------------------------------------------------");

            logBuilder.AppendLine("🔍 STACK TRACE");
            logBuilder.AppendLine(exception.StackTrace);
            logBuilder.AppendLine("----------------------------------------------------");

            _logger.LogInformation("Exception processed for alerting.");
            return logBuilder.ToString();
        }

        public void AddToBatch(Exception exception, HttpContext context)
        {
            string errorKey = HashGenerator.GenerateErrorHash(exception);
            string formattedLog = FormatLog(exception, context);

            lock (_lock)
            {
                if (_errorBatch.ContainsKey(errorKey))
                {
                    _errorBatch[errorKey] = (_errorBatch[errorKey].Count + 1, formattedLog);
                }
                else
                {
                    _errorBatch[errorKey] = (1, formattedLog);
                    _firstErrorTime ??= DateTime.UtcNow;
                }
            }
        }

        public bool ShouldSendBatch()
        {
            var now = DateTime.UtcNow;
            return (now - _lastBatchSent) >= _batchInterval || (_firstErrorTime.HasValue && (now - _firstErrorTime.Value) >= _batchInterval * 2);
        }

        public string GetBatchSummary()
        {
            if (_errorBatch.Count == 0) return string.Empty;

            var batchSummary = new StringBuilder();

            lock (_lock)
            {
                foreach (var (errorKey, (count, formattedLog)) in _errorBatch)
                {
                    batchSummary.AppendLine($"🔴 Error Count: `{count}`");
                    batchSummary.AppendLine(formattedLog);
                    batchSummary.AppendLine("\n---\n");
                }

                _errorBatch.Clear();
                _lastBatchSent = DateTime.UtcNow;
                _firstErrorTime = null;
            }

            return batchSummary.ToString();
        }
    }
}
