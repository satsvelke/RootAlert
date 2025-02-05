using Microsoft.Extensions.Logging;
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

        public async Task SendAlertAsync(string message)
        {
            var slackMessage = new
            {
                blocks = new object[]
                {
                    new { type = "section", text = new { type = "mrkdwn", text = "*🚨 Root Alert Error Alert*" } },
                    new { type = "divider" },
                    new { type = "section", text = new { type = "mrkdwn", text = message } },
                    new { type = "divider" },
                    new { type = "actions", elements = new object[]
                        {
                            new { type = "button", text = new { type = "plain_text", text = "View Error Logs" }}
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
    }
}
