namespace Michaelsoft.Mailer.Settings
{
    public class EmailSettings
    {

        public string HostAddress { get; set; }

        public int HostPort { get; set; }

        public string SenderEmail { get; set; }

        public string SenderPassword { get; set; }

        public string SenderName { get; set; }

        public string TemplatePath { get; set; }

    }
}