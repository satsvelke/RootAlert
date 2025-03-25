namespace RootAlert.Config
{
    public class SlackAlertOption : RootAlertOption
    {
        public string? WebhookUrl { get; set; }
        public string? DashboardUrl { get; set; }
    }
}
