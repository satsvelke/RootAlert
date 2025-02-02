using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace SherLog
{
    internal class SherLogProcessor
    {
        private readonly ILogger<SherLogProcessor> _logger;

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
    }
}