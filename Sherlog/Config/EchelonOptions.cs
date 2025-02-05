
namespace RootAlert.Config
{
    public class RootAlertOptions
    {
        public AlertType AlertMethod { get; set; } = AlertType.None;
        public string? WebhookUrl { get; set; }
        public EmailSettings? EmailSettings { get; set; }
        public TimeSpan BatchInterval { get; set; } = TimeSpan.FromMinutes(30);
    }

    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string ToEmail { get; set; } = string.Empty;
    }
}
