using Microsoft.Extensions.Logging;
using RootAlert.Config;
using System.Text;
using System.Text.Json;

namespace RootAlert.Alerts
{
    public class TeamsAlertService : IAlertService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<TeamsAlertService> _logger;
        private readonly RootAlertSetting _rootAlertSetting;

        public TeamsAlertService(ILogger<TeamsAlertService> logger, RootAlertSetting rootAlertSetting)
        {
            _httpClient = new HttpClient();
            _logger = logger;
            _rootAlertSetting = rootAlertSetting;
        }

        public async Task SendBatchAlertAsync(IList<ErrorLogEntry> errors)
        {

            var teamsAlertOption = _rootAlertSetting.RootAlertOptions?.OfType<TeamsAlertOption>().Where(c => c.AlertMethod == AlertType.Teams).FirstOrDefault();

            var errorBlocks = errors.Select((error, index) =>
            {
                var headersDictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(error.Request!.Headers)
                        ?? new Dictionary<string, object>();

                var headersList = headersDictionary
                    .Where(header => !header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                    .Select(header =>
                    {
                        // Handle both single string and array values
                        var value = header.Value switch
                        {
                            string singleValue => singleValue, // Single value
                            JsonElement element when element.ValueKind == JsonValueKind.Array =>
                                string.Join(", ", element.EnumerateArray().Select(e => e.GetString())), // Array of values
                            _ => header.Value?.ToString() ?? string.Empty // Fallback for other types
                        };

                        return $"**{header.Key}:** `{value}`";
                    })
                    .ToList();


                string headersFormatted = string.Join("\n", headersList);

                string stackTrace = error.Exception!.StackTrace ?? "No stack trace available.";

                return new object[]
                {
                    new { type = "TextBlock", size = "Medium", weight = "Bolder", text = $"🔴 Error #{index + 1}", color = "Attention", spacing = "Medium" },
                    new { type = "TextBlock", size = "Medium",  text = $"Error Count {error.Count}", color = "Attention", spacing = "Medium" },
                    new { type = "TextBlock", text = $"📅 **Timestamp:** `{DateTime.Now:MM/dd/yyyy h:mm:ss tt}`", wrap = true },
                    new { type = "TextBlock", text = $"🌐 **Request URL:** `{error.Request.Url}`", wrap = true },
                    new { type = "TextBlock", text = $"📡 **HTTP Method:** `{error.Request.Method}`", wrap = true },

                    new { type = "TextBlock", text = "📩 **Request Headers:**", weight = "Bolder", spacing = "Small" },
                    new { type = "TextBlock", text = headersFormatted, wrap = true },

                    new { type = "TextBlock", text = "⚠️ **Exception Details**", weight = "Bolder", spacing = "Small" },
                    new { type = "TextBlock", text = $"**Type:** `{error.Exception.GetType().Name}`", wrap = true },
                    new { type = "TextBlock", text = $"💬 **Message:** `{error.Exception.Message}`", wrap = true },

                    new { type = "TextBlock", text = "🔍 **Stack Trace**", weight = "Bolder", spacing = "Small" },
                    new { type = "TextBlock", text = $"```{stackTrace}```", wrap = true },

                    new { type = "TextBlock", text = "------------------------------", spacing = "Medium" }
                };
            }).SelectMany(x => x).ToArray();


            object[]? actions = null;
            if (!string.IsNullOrWhiteSpace(teamsAlertOption?.DashboardUrl))
            {
                actions = new object[]
                {
                     new { type = "Action.OpenUrl", title = "View Error Logs", url = teamsAlertOption.DashboardUrl }
                };
            }

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
                    actions = actions
                }
            }
        }
            };

            var jsonPayload = JsonSerializer.Serialize(adaptiveCard);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(teamsAlertOption?.WebhookUrl, content);
        }
    }
}
