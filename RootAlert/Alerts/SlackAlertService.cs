using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RootAlert.Config;
using RootAlert.Processing;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace RootAlert.Alerts
{
    public class SlackAlertService : IAlertService
    {
        private readonly HttpClient _httpClient;
        private readonly string _webhookUrl;
        private readonly ILogger<SlackAlertService> _logger;

        public SlackAlertService(string webhookUrl, ILogger<SlackAlertService> logger)
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

            var slackMessage = new
            {
                blocks = new object[]
                {
            new { type = "section", text = new { type = "mrkdwn", text = "*🚨 Root Alert*" } },
            new { type = "divider" },

            new { type = "section", text = new { type = "mrkdwn", text = $"📅 *Timestamp:* `{DateTime.Now.ToString("d MMM yyyy, h:mm:ss tt", CultureInfo.InvariantCulture)}`" } },
            new { type = "section", text = new { type = "mrkdwn", text = $"🌐 *Request URL:* `{requestUrl}`" } },
            new { type = "section", text = new { type = "mrkdwn", text = $"📡 *HTTP Method:* `{requestMethod}`" } },

            new { type = "section", text = new { type = "mrkdwn", text = "*📩 Request Headers:*" } },
            new { type = "section", text = new { type = "mrkdwn", text = headersFormatted } },

            new { type = "section", text = new { type = "mrkdwn", text = "*⚠️ Exception Details*" } },
            new { type = "section", text = new { type = "mrkdwn", text = $"❗ *Type:* `{exception.GetType().Name}`" } },
            new { type = "section", text = new { type = "mrkdwn", text = $"💬 *Message:* `{exception.Message}`" } },

            new { type = "section", text = new { type = "mrkdwn", text = "*🔍 Stack Trace:*" } },
            new { type = "section", text = new { type = "mrkdwn", text = $"```{stackTrace}```" } },

            new { type = "divider" },
            new { type = "actions", elements = new object[]
                {
                    new { type = "button", text = new { type = "plain_text", text = "View Error Logs" }, url = "https://your-error-dashboard-url", style = "primary" }
                }
            }
                }
            };

            var jsonPayload = JsonSerializer.Serialize(slackMessage);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(_webhookUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Slack alert sent successfully.");
                }
                else
                {
                    _logger.LogError($"Failed to send Slack alert. Status: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending Slack alert.");
            }
        }

        public async Task SendBatchAlertAsync(List<(int count, Exception exception, RequestInfo requestInfo)> errors)
        {
            var errorBlocks = errors.Select((error, index) =>
            {
                var headersList = error.requestInfo.Headers
                    .Select(header => $"*{header.Key}:* `{header.Value}`")
                    .ToList();

                string headersFormatted = string.Join("\n", headersList);
                string stackTrace = error.exception.StackTrace ?? "No stack trace available.";

                return new object[]
                {
                    new { type = "header", text = new { type = "plain_text", text = $"🔴 Error #{index + 1}", emoji = true } },
                    new { type = "section", text = new { type = "mrkdwn", text = $"📅 *Timestamp:* `{DateTime.UtcNow:MM/dd/yyyy h:mm:ss tt}`" } },
                    new { type = "section", text = new { type = "mrkdwn", text = $"🌐 *URL:* `{error.requestInfo.Url}`" } },
                    new { type = "section", text = new { type = "mrkdwn", text = $"📡 *Method:* `{error.requestInfo.Method}`" } },
                    new { type = "section", text = new { type = "mrkdwn", text = $"📩 *Headers:*\n{headersFormatted}" } },
                    new { type = "section", text = new { type = "mrkdwn", text = $"⚠️ *Exception:* `{error.exception.Message}`" } },
                    new { type = "section", text = new { type = "mrkdwn", text = $"🔍 *Stack Trace:*\n```{stackTrace}```" } },
                    new { type = "divider" }
                };
            }).SelectMany(x => x).ToArray();

            var slackMessage = new
            {
                blocks = new object[]
                {
                    new { type = "section", text = new { type = "mrkdwn", text = "*🚨 Root Alert - Batched Error Summary*" } },
                    new { type = "divider" }
                }.Concat(errorBlocks).ToArray()
            };

            var jsonPayload = JsonSerializer.Serialize(slackMessage);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_webhookUrl, content);
        }
    }
}
