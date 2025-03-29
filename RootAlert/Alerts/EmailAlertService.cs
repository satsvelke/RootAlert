using System.Net.Mail;
using System.Text;
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
            sb.AppendLine("<body style='font-family: -apple-system, BlinkMacSystemFont, \"Segoe UI\", Roboto, Oxygen, Ubuntu, Cantarell, \"Open Sans\", \"Helvetica Neue\", sans-serif; margin:0; padding:20px; background-color:#121212; color:#f5f5f5; line-height:1.6;'>");
            sb.AppendLine("    <div style='max-width:800px; margin:0 auto; padding:30px; background-color:#1e1e1e; border-radius:12px; box-shadow:0 4px 24px rgba(0,0,0,0.2);'>");
            sb.AppendLine("        <div style='display:flex; align-items:center; margin-bottom:30px; padding-bottom:15px; border-bottom:1px solid rgba(255,255,255,0.1);'>");
            sb.AppendLine("            <div style='font-size:28px; margin-right:15px; color:#ff4757;'>🚨</div>");
            sb.AppendLine("            <h1 style='margin:0; font-size:24px; font-weight:600; color:#f5f5f5;'>Root Alert - Batched Error Summary</h1>");
            sb.AppendLine("        </div>");
            // Navigation with total errors right-aligned
            sb.AppendLine("        <div style='background: linear-gradient(to right, rgba(255,71,87,0.1), rgba(33,150,243,0.1)); border-radius:12px; padding:15px 20px; margin-bottom:30px; display:flex; justify-content:flex-end; align-items:center;'>");
            sb.AppendFormat("            <div style='font-size:16px; font-weight:500;'>Total Errors: <strong style='color:#ff4757; font-size:18px;'>{0}</strong></div>", errors.Count);
            sb.AppendLine();
            sb.AppendLine("        </div>");

            // Generate each error section
            for (int i = 0; i < errors.Count; i++)
            {
                var error = errors[i];
                // If a Timestamp property exists, use it. Otherwise, display "N/A"
                var timestamp = error.GetType().GetProperty("Timestamp") != null
                    ? error.GetType().GetProperty("Timestamp")?.GetValue(error)?.ToString() ?? "N/A"
                    : "N/A";

                sb.AppendLine($"        <!-- Error #{i + 1} -->");
                sb.AppendLine($"        <div id='error{i + 1}' style='margin-bottom:30px; background-color:#292929; border-radius:12px; overflow:hidden; box-shadow:0 2px 10px rgba(0,0,0,0.1);'>");
                sb.AppendLine("            <div style='background: linear-gradient(to right, rgba(255,71,87,0.8), rgba(255,71,87,0.4)); padding:15px 20px; display:flex; justify-content: space-between; align-items:center;'>");
                sb.AppendFormat("                <h2 style='display:flex; align-items:center; gap:8px; margin:0; font-size:18px; font-weight:600; color:white;'><span>🔴</span> Error #{0}</h2>", i + 1);
                sb.AppendLine();
                sb.AppendFormat("                <div style='background-color: rgba(0,0,0,0.2); color:white; font-size:14px; padding:3px 10px; border-radius:20px; font-weight:500;'>Count: {0}</div>", error.Count);
                sb.AppendLine();
                sb.AppendLine("            </div>");
                sb.AppendLine("            <div style='padding:20px;'>");
                sb.AppendLine("                <div style='display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap:15px; margin-bottom:20px;'>");
                sb.AppendLine("                    <div style='background-color: rgba(255,255,255,0.05); padding:12px 15px; border-radius:8px;'>");
                sb.AppendLine("                        <div style='display:flex; align-items:center; gap:6px; font-size:13px; font-weight:500; color:#b0b0b0; margin-bottom:6px;'><span>📅</span> Timestamp</div>");
                sb.AppendFormat("                        <div style='font-size:14px; word-break:break-word;'>{0}</div>", timestamp);
                sb.AppendLine();
                sb.AppendLine("                    </div>");
                sb.AppendLine("                    <div style='background-color: rgba(255,255,255,0.05); padding:12px 15px; border-radius:8px;'>");
                sb.AppendLine("                        <div style='display:flex; align-items:center; gap:6px; font-size:13px; font-weight:500; color:#b0b0b0; margin-bottom:6px;'><span>🌐</span> Request URL</div>");
                sb.AppendFormat("                        <div style='font-size:14px; word-break:break-word;'>{0}</div>", error.Request?.Url ?? "N/A");
                sb.AppendLine();
                sb.AppendLine("                    </div>");
                sb.AppendLine("                    <div style='background-color: rgba(255,255,255,0.05); padding:12px 15px; border-radius:8px;'>");
                sb.AppendLine("                        <div style='display:flex; align-items:center; gap:6px; font-size:13px; font-weight:500; color:#b0b0b0; margin-bottom:6px;'><span>🏷️</span> HTTP Method</div>");
                sb.AppendFormat("                        <div style='font-size:14px; word-break:break-word;'>{0}</div>", error.Request?.Method ?? "N/A");
                sb.AppendLine();
                sb.AppendLine("                    </div>");
                sb.AppendLine("                </div>");

                if (!string.IsNullOrEmpty(error.Request?.Headers))
                {
                    sb.AppendLine("                <div style='background-color: rgba(255,255,255,0.03); border-radius:8px; padding:15px; margin-bottom:20px;'>");
                    sb.AppendLine("                    <div style='display:flex; align-items:center; gap:6px; font-size:15px; font-weight:500; color:#b0b0b0; margin-bottom:10px;'><span>📨</span> Request Headers</div>");
                    sb.AppendLine("                    <div style='display: grid; grid-template-columns: auto 1fr; gap:8px 15px; font-size:13px;'>");
                    sb.AppendFormat("                        <div style='font-weight:500; color:#2196f3;'>Headers:</div><div style='font-size:14px;'>{0}</div>", error.Request.Headers);
                    sb.AppendLine();
                    sb.AppendLine("                    </div>");
                    sb.AppendLine("                </div>");
                }

                if (error.Exception != null)
                {
                    sb.AppendLine("                <div style='background: linear-gradient(to right, rgba(255,165,2,0.1), rgba(255,165,2,0.05)); border-left:4px solid #ffa502; border-radius:8px; padding:15px;'>");
                    sb.AppendLine("                    <h3 style='display:flex; align-items:center; gap:8px; font-size:16px; font-weight:600; color:#ffa502; margin:0 0 12px 0;'><span>⚠️</span> Exception Details</h3>");
                    sb.AppendLine("                    <div style='margin-bottom:15px;'>");
                    sb.AppendLine("                        <div style='margin-bottom:8px;'><span style='font-weight:500; margin-right:8px; color:#b0b0b0;'>Type:</span> <span style='font-size:14px;'>" + error.Exception.Name + "</span></div>");
                    sb.AppendLine("                        <div style='margin-bottom:8px;'><span style='font-weight:500; margin-right:8px; color:#b0b0b0;'>Message:</span> <span style='font-size:14px;'>" + error.Exception.Message + "</span></div>");
                    sb.AppendLine("                    </div>");
                    sb.AppendFormat("                    <div style='background-color: rgba(0,0,0,0.2); border-radius:8px; padding:15px; font-family: \"SFMono-Regular\", Consolas, \"Liberation Mono\", Menlo, monospace; font-size:12px; line-height:1.5; overflow-x:auto; color:#e0e0e0; margin-top:12px; white-space:pre-wrap;'>{0}</div>", error.Exception.StackTrace);
                    sb.AppendLine();
                    sb.AppendLine("                </div>");
                }

                sb.AppendLine("            </div>");
                sb.AppendLine("        </div>");
            }

            sb.AppendLine("    </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }
    }
}
