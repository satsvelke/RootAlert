using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RootAlert.Processing;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace RootAlert.Alerts
{
    public class TeamsAlertService : IAlertService
    {
        private readonly HttpClient _httpClient;
        private readonly string _webhookUrl;
        private readonly ILogger<TeamsAlertService> _logger;

        public TeamsAlertService(string webhookUrl, ILogger<TeamsAlertService> logger)
        {
            _httpClient = new HttpClient();
            _webhookUrl = webhookUrl;
            _logger = logger;
        }

        public async Task SendAlertAsync(Exception exception, HttpContext context)
        {

            string requestUrl = context.Request.Path;
            string requestMethod = context.Request.Method;

            var headersList = context.Request.Headers
                .Where(header => !header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                .Select(header => $"*{header.Key}:* `{header.Value}`")
                .ToList();

            string headersFormatted = string.Join("\n", headersList);

            string stackTrace = exception.StackTrace ?? "No stack trace available.";

            var adaptiveCard = new
            {
                type = "message",
                attachments = new[]
                {
            new
            {
                contentType = "application/vnd.microsoft.card.adaptive",
                content = new
                {
                    type = "AdaptiveCard",
                    version = "1.4",
                    body = new object[]
                    {
                        new { type = "TextBlock", size = "Large", weight = "Bolder", text = "🚨 Root Alert", color = "Attention", spacing = "Medium" },

                        new { type = "TextBlock", text = $"📅 **Timestamp:** {DateTime.Now.ToString("d MMM yyyy, h:mm:ss tt", CultureInfo.InvariantCulture)}", wrap = true, spacing = "Small" },
                        new { type = "TextBlock", text = $"🌐 **Request URL:** `{requestUrl}`", wrap = true, spacing = "Small" },
                        new { type = "TextBlock", text = $"📡 **HTTP Method:** `{requestMethod}`", wrap = true, spacing = "Small" },

                        new { type = "TextBlock", text = "📩 **Request Headers:**", weight = "Bolder", spacing = "Medium" },
                        new { type = "TextBlock", text = headersFormatted, wrap = true, spacing = "None" },

                        new { type = "TextBlock", text = "⚠️ **Exception Details**", weight = "Bolder", spacing = "Medium" },
                        new { type = "TextBlock", text = $"**Type:** `{exception.GetType().Name}`", wrap = true, spacing = "Small" },
                        new { type = "TextBlock", text = $"💬 **Message:** `{exception.Message}`", wrap = true, spacing = "Small" },

                        new { type = "TextBlock", text = "🔍 **Stack Trace**", weight = "Bolder", spacing = "Medium" },
                        new { type = "TextBlock", text = $"```{stackTrace}```", wrap = true, spacing = "None" },

                        new { type = "ActionSet", spacing = "Medium", actions = new object[]
                            {
                                new { type = "Action.OpenUrl", title = "View Error Logs", url = "https://your-error-dashboard-url" }
                            }
                        }
                    }
                }
            }
        }
            };

            var jsonPayload = JsonSerializer.Serialize(adaptiveCard);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(_webhookUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Teams alert sent successfully.");
                }
                else
                {
                    _logger.LogError($"Failed to send Teams alert. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending Teams alert.");
            }
        }

        public async Task SendBatchAlertAsync(List<(int count, Exception exception, RequestInfo requestInfo)> errors)
        {
            var errorBlocks = errors.Select((error, index) =>
            {
                var headersList = error.requestInfo.Headers
                    .Select(header => $"**{header.Key}:** `{header.Value}`")
                    .ToList();
                string headersFormatted = string.Join("\n", headersList);

                string stackTrace = error.exception.StackTrace ?? "No stack trace available.";

                return new object[]
                {
                    new { type = "TextBlock", size = "Medium", weight = "Bolder", text = $"🔴 Error #{index + 1}", color = "Attention", spacing = "Medium" },
                    new { type = "TextBlock", text = $"📅 **Timestamp:** `{DateTime.Now:MM/dd/yyyy h:mm:ss tt}`", wrap = true },
                    new { type = "TextBlock", text = $"🌐 **Request URL:** `{error.requestInfo.Url}`", wrap = true },
                    new { type = "TextBlock", text = $"📡 **HTTP Method:** `{error.requestInfo.Method}`", wrap = true },

                    new { type = "TextBlock", text = "📩 **Request Headers:**", weight = "Bolder", spacing = "Small" },
                    new { type = "TextBlock", text = headersFormatted, wrap = true },

                    new { type = "TextBlock", text = "⚠️ **Exception Details**", weight = "Bolder", spacing = "Small" },
                    new { type = "TextBlock", text = $"**Type:** `{error.exception.GetType().Name}`", wrap = true },
                    new { type = "TextBlock", text = $"💬 **Message:** `{error.exception.Message}`", wrap = true },

                    new { type = "TextBlock", text = "🔍 **Stack Trace**", weight = "Bolder", spacing = "Small" },
                    new { type = "TextBlock", text = $"```{stackTrace}```", wrap = true },

                    new { type = "TextBlock", text = "------------------------------", spacing = "Medium" }
                };
            }).SelectMany(x => x).ToArray();

            var adaptiveCard = new
            {
                type = "message",
                attachments = new[]
                {
            new
            {
                contentType = "application/vnd.microsoft.card.adaptive",
                content = new
                {
                    type = "AdaptiveCard",
                    version = "1.4",
                    body = new object[]
                    {
                        new { type = "TextBlock", size = "Large", weight = "Bolder", text = "🚨 Root Alert - Batched Error Summary", color = "Attention", spacing = "Medium" }
                    }.Concat(errorBlocks).ToArray(),
                    actions = new object[]
                    {
                        new { type = "Action.OpenUrl", title = "View Error Logs", url = "https://your-error-dashboard-url" }
                    }
                }
            }
        }
            };

            var jsonPayload = JsonSerializer.Serialize(adaptiveCard);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_webhookUrl, content);
        }
    }
}
