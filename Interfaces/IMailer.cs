using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Michaelsoft.Mailer.Interfaces
{
    public interface IMailer
    {

        Task SendMailAsync(Dictionary<string, string> tos,
                            string subject,
                            string body,
                            Dictionary<string, string> ccs = null,
                            Dictionary<string, string> bccs = null,
                            Dictionary<string, Stream> attachments = null
        );

        Task SendMailUsingTemplateAsync(Dictionary<string, string> tos,
                                         string subject,
                                         string template,
                                         Dictionary<string, string> parameters,
                                         Dictionary<string, string> ccs = null,
                                         Dictionary<string, string> bccs = null,
                                         Dictionary<string, Stream> attachments = null
        );

    }
}