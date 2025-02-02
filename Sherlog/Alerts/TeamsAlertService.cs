using System;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Sherlog.Alerts;

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
        var payload = new { text = message };
        var jsonPayload = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync(_webhookUrl, content);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook alert sent successfully.");
            }
            else
            {
                _logger.LogError($"Failed to send alert. Status: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while sending webhook alert.");
        }
    }
}
