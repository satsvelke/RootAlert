using Microsoft.Extensions.Logging;
using RootAlert.Config;
using System.Text;
using System.Text.Json;

namespace RootAlert.Alerts
{
    public class SlackAlertService : IAlertService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<SlackAlertService> _logger;
        private readonly RootAlertSetting _rootAlertSetting;
        public SlackAlertService(ILogger<SlackAlertService> logger, RootAlertSetting rootAlertSetting)
        {
            _httpClient = new HttpClient();
            _logger = logger;
            _rootAlertSetting = rootAlertSetting;
        }

        public async Task SendBatchAlertAsync(IList<ErrorLogEntry> errors)
        {
            var slackOption = _rootAlertSetting.RootAlertOptions?
                                .OfType<SlackAlertOption>()
                                .FirstOrDefault();

            var errorBlocks = new List<object>();

            foreach (var (error, index) in errors.Select((e, i) => (e, i)))
            {
                // Exclude Authorization
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


                string headersFormatted = headersList.Any() ? string.Join("\n", headersList) : "No headers available.";
                string stackTrace = error.Exception?.StackTrace ?? "No stack trace available.";
                string errorMessage = error.Exception?.Message ?? "No error message.";

                var errorDetails = new List<object>
                    {
                        new { type = "header", text = new { type = "plain_text", text = $"🔴 Error #{index + 1}", emoji = true } },
                        new { type = "section", text = new { type = "plain_text", text = $"Error Count: {error.Count}", emoji = true } },
                        new { type = "section", text = new { type = "mrkdwn", text = $"📅 *Timestamp:* `{DateTime.Now:MM/dd/yyyy h:mm:ss tt}`" } },
                        new { type = "section", text = new { type = "mrkdwn", text = $"🌐 *URL:* `{error.Request?.Url ?? "N/A"}`" } },
                        new { type = "section", text = new { type = "mrkdwn", text = $"📡 *Method:* `{error.Request?.Method ?? "N/A"}`" } },
                        new { type = "section", text = new { type = "mrkdwn", text = $"📩 *Headers:*\n{headersFormatted}" } },
                        new { type = "section", text = new { type = "mrkdwn", text = $"⚠️ *Exception:* `{errorMessage}`" } },
                        new { type = "section", text = new { type = "mrkdwn", text = $"🔍 *Stack Trace:*\n```{stackTrace}```" } },
                        new { type = "divider" }
                    };

                if (!string.IsNullOrWhiteSpace(slackOption?.DashboardUrl))
                {
                    errorDetails.Add(new
                    {
                        type = "actions",
                        elements = new object[]
                        {
                    new { type = "button", text = new { type = "plain_text", text = "View Error Logs" }, url = slackOption.DashboardUrl, style = "primary" }
                        }
                    });
                }

                errorBlocks.AddRange(errorDetails);
            }

            var slackMessage = new
            {
                blocks = new List<object>
                {
                    new { type = "section", text = new { type = "mrkdwn", text = "*🚨 Root Alert - Batched Error Summary*" } },
                    new { type = "divider" }
                }.Concat(errorBlocks).ToArray()
            };

            var jsonPayload = JsonSerializer.Serialize(slackMessage);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(slackOption?.WebhookUrl, content);
            response.EnsureSuccessStatusCode();
        }
    }
}
