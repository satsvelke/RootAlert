
using System.Net.Mail;

namespace RootAlert.Config
{
    public class SMTPEmailAlertOption : RootAlertOption
    {
        public SmtpClient? SmtpClient { get; set; }

        public string? From { get; set; }
        public IList<string>? To { get; set; }
        public IList<string>? CC { get; set; }
        public IList<string>? BCC { get; set; }
    }
}