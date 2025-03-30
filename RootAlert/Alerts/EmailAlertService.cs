using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RootAlert.Config;

namespace RootAlert.Alerts
{
    internal sealed class EmailAlertService : IAlertService
    {
        private readonly ILogger<EmailAlertService> _logger;
        private readonly RootAlertSetting _rootAlertSetting;

        public EmailAlertService(ILogger<EmailAlertService> logger, RootAlertSetting rootAlertSetting)
        {
            _logger = logger;
            _rootAlertSetting = rootAlertSetting;
        }

        public async Task SendBatchAlertAsync(IList<ErrorLogEntry> errors)
        {
            var emailOptions = _rootAlertSetting.RootAlertOptions?
                               .OfType<SMTPEmailAlertOption>()
                               .FirstOrDefault();

            if (emailOptions == null || emailOptions.SmtpClient == null || emailOptions.To == null || !emailOptions.To.Any())
            {
                _logger.LogWarning("SMTP email options are not properly configured.");
                return;
            }

            try
            {
                var message = new MailMessage()
                {
                    Subject = "Error Alert - Batch Notification",
                    Body = BuildEmailBody(errors),
                    IsBodyHtml = true
                };

                foreach (var recipient in emailOptions.To)
                {
                    message.To.Add(recipient);
                }

                if (emailOptions.CC != null)
                {
                    foreach (var cc in emailOptions.CC)
                    {
                        message.CC.Add(cc);
                    }
                }

                if (emailOptions.BCC != null)
                {
                    foreach (var bcc in emailOptions.BCC)
                    {
                        message.Bcc.Add(bcc);
                    }
                }

                if (!string.IsNullOrWhiteSpace(emailOptions.From))
                    message.From = new MailAddress(emailOptions.From!);

                await emailOptions.SmtpClient.SendMailAsync(message);
                _logger.LogInformation("Batch alert email sent successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send batch alert email.");
            }
        }

        private string BuildEmailBody(IList<ErrorLogEntry> errors)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='en'>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <meta charset='UTF-8'>");
            sb.AppendLine("    <meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("    <title>Root Alert - Batched Error Summary</title>");
            sb.AppendLine("</head>");
            sb.AppendLine(@"
<body style='
    font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Oxygen, Ubuntu, Cantarell, ""Open Sans"", ""Helvetica Neue"", sans-serif; 
    margin: 0; 
    padding: 20px; 
    background-color: #121212; 
    color: #f5f5f5; 
    line-height: 1.6;'>

    <div style='
        max-width: 800px; 
        margin: 0 auto; 
        padding: 30px; 
        background-color: #1e1e1e; 
        border-radius: 12px; 
        box-shadow: 0 4px 24px rgba(0, 0, 0, 0.2);'>

        <!-- Header -->
        <div style='
            display: flex; 
            align-items: center; 
            margin-bottom: 30px; 
            padding-bottom: 15px; 
            border-bottom: 1px solid rgba(255, 255, 255, 0.1);'>
            <div style='font-size: 28px; margin-right: 15px; color: #ff4757;'>🚨</div>
            <h1 style='
                margin: 0; 
                font-size: 24px; 
                font-weight: 600; 
                color: #f5f5f5;'>
                Root Alert - Batched Error Summary
            </h1>
        </div>
");

            // Nav with total errors right-aligned
            sb.AppendLine(@"
        <div style='
            background: linear-gradient(to right, rgba(255,71,87,0.1), rgba(33,150,243,0.1)); 
            border-radius: 12px; 
            padding: 15px 20px; 
            margin-bottom: 30px; 
            text-align: right;'>
            <div style='
                display: inline-block; 
                font-size: 16px; 
                font-weight: 500;'>
                Total Errors: <strong style='color: #ff4757; font-size: 18px;'>");
            sb.Append(errors.Count);
            sb.AppendLine(@"</strong>
            </div>
        </div>
");

            // Generate each error section
            for (int i = 0; i < errors.Count; i++)
            {
                var error = errors[i];

                // If a Timestamp property exists, use it. Otherwise, display "N/A"
                var timestamp = error.GetType().GetProperty("Timestamp") != null
                    ? error.GetType().GetProperty("Timestamp")?.GetValue(error)?.ToString() ?? "N/A"
                    : "N/A";

                sb.AppendLine($@"        
        <!-- Error #{i + 1} -->
        <div id='error{i + 1}' style='
            margin-bottom: 30px; 
            background-color: #292929; 
            border-radius: 12px; 
            overflow: hidden; 
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);'>

            <!-- Header row with Error # on the left and Count on the right -->
            <div style='
                background: linear-gradient(to right, rgba(255,71,87,0.8), rgba(255,71,87,0.4)); 
                padding: 15px 20px;'>
                <table width='100%' border='0' cellspacing='0' cellpadding='0' style='border-collapse: collapse;'>
                    <tr>
                        <td style='vertical-align: middle;'>
                            <h2 style='
                                margin: 0; 
                                font-size: 18px; 
                                font-weight: 600; 
                                color: #fff;'>
                                <span>🔴</span> Error #{i + 1}
                            </h2>
                        </td>
                        <td style='
                            vertical-align: middle; 
                            text-align: right;'>
                            <div style='
                                display: inline-block; 
                                background-color: rgba(0,0,0,0.2); 
                                color: #fff; 
                                font-size: 14px; 
                                padding: 3px 10px; 
                                border-radius: 20px; 
                                font-weight: 500;'>
                                Count: {error.Count}
                            </div>
                        </td>
                    </tr>
                </table>
            </div>

            <!-- Error content -->
            <div style='padding: 20px;'>
                <!-- Info row: Timestamp, Request URL, HTTP Method -->
                <table width='100%' border='0' cellspacing='0' cellpadding='0' style='
                    border-collapse: separate; 
                    border-spacing: 15px; 
                    margin-bottom: 20px;'>
                    <tr>
                        <td width='33%' style='
                            background-color: rgba(255,255,255,0.05); 
                            padding: 12px 15px; 
                            border-radius: 8px; 
                            vertical-align: top;'>
                            <div style='
                                display: flex; 
                                align-items: center; 
                                gap: 6px; 
                                font-size: 13px; 
                                font-weight: 500; 
                                color: #b0b0b0; 
                                margin-bottom: 6px;'>
                                <span>📅</span> Timestamp
                            </div>
                            <div style='font-size: 14px; word-break: break-word;'>
                                {timestamp}
                            </div>
                        </td>
                        <td width='33%' style='
                            background-color: rgba(255,255,255,0.05); 
                            padding: 12px 15px; 
                            border-radius: 8px; 
                            vertical-align: top;'>
                            <div style='
                                display: flex; 
                                align-items: center; 
                                gap: 6px; 
                                font-size: 13px; 
                                font-weight: 500; 
                                color: #b0b0b0; 
                                margin-bottom: 6px;'>
                                <span>🌐</span> Request URL
                            </div>
                            <div style='font-size: 14px; word-break: break-word;'>
                                {(error.Request?.Url ?? "N/A")}
                            </div>
                        </td>
                        <td width='33%' style='
                            background-color: rgba(255,255,255,0.05); 
                            padding: 12px 15px; 
                            border-radius: 8px; 
                            vertical-align: top;'>
                            <div style='
                                display: flex; 
                                align-items: center; 
                                gap: 6px; 
                                font-size: 13px; 
                                font-weight: 500; 
                                color: #b0b0b0; 
                                margin-bottom: 6px;'>
                                <span>🏷️</span> HTTP Method
                            </div>
                            <div style='font-size: 14px; word-break: break-word;'>
                                {(error.Request?.Method ?? "N/A")}
                            </div>
                        </td>
                    </tr>
                </table>
");

                // Render Request Headers
                if (!string.IsNullOrEmpty(error.Request?.Headers))
                {
                    // Attempt to parse the JSON into a Dictionary<string, string>
                    Dictionary<string, object>? headersDict = null;
                    var headersList = new List<string>();
                    try
                    {
                        headersDict = JsonSerializer.Deserialize<Dictionary<string, object>>(error.Request.Headers);


                        headersList = headersDict!
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
                    }
                    catch
                    {
                        // If parsing fails, we'll show raw text as a fallback
                    }

                    if (headersList != null && headersList.Count > 0)
                    {
                        // Build a table of headers
                        sb.AppendLine(@"
                <div style='
                    background-color: rgba(255,255,255,0.03); 
                    border-radius: 8px; 
                    padding: 15px; 
                    margin-bottom: 20px;'>
                    <div style='
                        display: flex; 
                        align-items: center; 
                        gap: 6px; 
                        font-size: 15px; 
                        font-weight: 500; 
                        color: #b0b0b0; 
                        margin-bottom: 10px;'>
                        <span>📨</span> Request Headers
                    </div>
                    <table width='100%' border='0' cellspacing='0' cellpadding='0' style='border-collapse: collapse;'>
");
                        foreach (var kvp in headersList)
                        {
                            sb.AppendLine($@"
                        <tr>
                            <td width='25%' style='
                                font-weight: 500; 
                                color: #2196f3; 
                                padding: 6px; 
                                vertical-align: top;'>
                                {kvp}:
                            </td>
                          
                        </tr>");
                        }
                        sb.AppendLine(@"
                    </table>
                </div>");
                    }
                    else
                    {
                        // Fallback: show the raw headers string
                        sb.AppendLine($@"
                <div style='
                    background-color: rgba(255,255,255,0.03); 
                    border-radius: 8px; 
                    padding: 15px; 
                    margin-bottom: 20px;'>
                    <div style='
                        display: flex; 
                        align-items: center; 
                        gap: 6px; 
                        font-size: 15px; 
                        font-weight: 500; 
                        color: #b0b0b0; 
                        margin-bottom: 10px;'>
                        <span>📨</span> Request Headers
                    </div>
                    <div style='
                        font-weight: 500; 
                        color: #2196f3; 
                        margin-bottom: 6px;'>
                        Headers:
                    </div>
                    <div style='font-size:14px;'>
                        {error.Request.Headers}
                    </div>
                </div>");
                    }
                }

                // Exception
                if (error.Exception != null)
                {
                    sb.AppendLine($@"
                <div style='
                    background: linear-gradient(to right, rgba(255,165,2,0.1), rgba(255,165,2,0.05)); 
                    border-left: 4px solid #ffa502; 
                    border-radius: 8px; 
                    padding: 15px;'>
                    <h3 style='
                        display: flex; 
                        align-items: center; 
                        gap: 8px; 
                        font-size: 16px; 
                        font-weight: 600; 
                        color: #ffa502; 
                        margin: 0 0 12px 0;'>
                        <span>⚠️</span> Exception Details
                    </h3>
                    <div style='margin-bottom: 15px;'>
                        <div style='margin-bottom: 8px;'>
                            <span style='
                                font-weight: 500; 
                                margin-right: 8px; 
                                color: #b0b0b0;'>
                                Type:
                            </span> 
                            <span style='font-size:14px;'>
                                {error.Exception.Name}
                            </span>
                        </div>
                        <div style='margin-bottom: 8px;'>
                            <span style='
                                font-weight: 500; 
                                margin-right: 8px; 
                                color: #b0b0b0;'>
                                Message:
                            </span> 
                            <span style='font-size:14px;'>
                                {error.Exception.Message}
                            </span>
                        </div>
                    </div>
                    <div style='
                        background-color: rgba(0,0,0,0.2); 
                        border-radius: 8px; 
                        padding: 15px; 
                        font-family: ""SFMono-Regular"", Consolas, ""Liberation Mono"", Menlo, monospace; 
                        font-size: 12px; 
                        line-height: 1.5; 
                        overflow-x: auto; 
                        color: #e0e0e0; 
                        margin-top: 12px; 
                        white-space: pre-wrap;'>
                        {error.Exception.StackTrace}
                    </div>
                </div>");
                }

                sb.AppendLine("            </div>"); // end error-content
                sb.AppendLine("        </div>");     // end error-section
            }

            sb.AppendLine("    </div>"); // end container
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}
