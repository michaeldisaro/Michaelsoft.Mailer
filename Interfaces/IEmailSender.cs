using System.Collections.Generic;
using System.Threading.Tasks;

namespace Michaelsoft.Mailer.Interfaces
{
    public interface IEmailSender
    {

        Task SendEmailAsync(Dictionary<string, string> tos,
                            string subject,
                            string body,
                            Dictionary<string, string> ccs = null,
                            Dictionary<string, string> bccs = null
        );

        Task SendEmailUsingTemplateAsync(Dictionary<string, string> tos,
                                         string subject,
                                         string template,
                                         Dictionary<string, string> parameters,
                                         Dictionary<string, string> ccs = null,
                                         Dictionary<string, string> bccs = null
        );

    }
}