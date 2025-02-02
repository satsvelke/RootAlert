using Microsoft.Extensions.Logging;
using Sherlog.Hashing;
using System.Collections.Concurrent;
using System.Text;

namespace SherLog.Processing
{
    public class SherLogProcessor
    {
        private readonly ILogger<SherLogProcessor> _logger;
        private static readonly ConcurrentDictionary<string, int> _errorBatch = new();
        private static readonly object _lock = new();
        private static DateTime _lastBatchSent = DateTime.UtcNow;

        public SherLogProcessor(ILogger<SherLogProcessor> logger)
        {
            _logger = logger;
        }

        public string FormatLog(Exception exception, string requestPath, string queryString, string requestMethod)
        {
            var logBuilder = new StringBuilder();
            logBuilder.AppendLine("🚨 **New Exception Logged in SherLog**");
            logBuilder.AppendLine($"📅 Timestamp: {DateTime.UtcNow}");
            logBuilder.AppendLine($"🔗 Request Path: {requestPath}");
            logBuilder.AppendLine($"🛠️ HTTP Method: {requestMethod}");
            logBuilder.AppendLine($"❓ Query String: {queryString}");
            logBuilder.AppendLine($"⚠️ Exception Type: {exception.GetType().Name}");
            logBuilder.AppendLine($"📢 Message: {exception.Message}");
            logBuilder.AppendLine("🔍 Stack Trace:");
            logBuilder.AppendLine($"```{exception.StackTrace}```");

            _logger.LogInformation("Exception processed for alerting.");
            return logBuilder.ToString();
        }

        public void AddToBatch(Exception exception)
        {
            string errorKey = HashGenerator.GenerateErrorHash(exception);

            lock (_lock)
            {
                if (_errorBatch.ContainsKey(errorKey))
                    _errorBatch[errorKey]++;
                else
                    _errorBatch[errorKey] = 1;
            }
        }

        public bool ShouldSendBatch()
        {
            return (DateTime.UtcNow - _lastBatchSent).TotalMinutes >= 1;
        }

        public string GetBatchSummary()
        {
            if (_errorBatch.Count == 0) return string.Empty;

            var batchSummary = new StringBuilder();
            batchSummary.AppendLine("🚨 **SherLog Error Summary** 🚨\n");

            foreach (var error in _errorBatch)
            {
                batchSummary.AppendLine($"🔴 Error Count: **{error.Value}**");
                batchSummary.AppendLine($"🔍 Error ID: `{error.Key}`\n");
            }

            lock (_lock)
            {
                _errorBatch.Clear();
                _lastBatchSent = DateTime.UtcNow;
            }

            return batchSummary.ToString();
        }

        public string GetBatchSummaryMarkdown()
        {
            if (_errorBatch.Count == 0) return string.Empty;

            var batchSummary = new StringBuilder();
            batchSummary.AppendLine("*🚨 SherLog Error Summary 🚨*\n");

            foreach (var error in _errorBatch)
            {
                batchSummary.AppendLine($"• *Error Count:* `{error.Value}`");
                batchSummary.AppendLine($"• *Error ID:* `{error.Key}`\n");
            }

            lock (_lock)
            {
                _errorBatch.Clear();
                _lastBatchSent = DateTime.UtcNow;
            }

            return batchSummary.ToString();
        }
    }
}
