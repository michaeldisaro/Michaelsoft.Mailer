using System.IO;

namespace Michaelsoft.Mailer.Models
{
    public class Attachment
    {

        public string Name { get; set; }

        public Stream Content { get; set; }

        public string Type { get; set; }

        public string SubType { get; set; }

    }
}