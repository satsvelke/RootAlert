using Microsoft.Extensions.Logging;
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

        public async Task SendAlertAsync(string message)
        {
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
                                new { type = "TextBlock", size = "Large", weight = "Bolder", text = "🚨 Echelon Error Alert", color = "Attention" },
                                new { type = "TextBlock", text = message, wrap = true },
                                new { type = "TextBlock", text = "🔗 Click below for details:", wrap = true },
                                new { type = "ActionSet", actions = new object[]
                                    {
                                        new { type = "Action.OpenUrl", title = "View Error Logs" }
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
    }
}
