namespace RootAlert.Config
{
    public enum AlertType
    {
        None = 0,    // No alerts
        Slack = 1,   // Slack webhook alert
        Teams = 2,   // Teams webhook alert
        SMTPEmail = 3    // Email alert with SMTP
    }
}