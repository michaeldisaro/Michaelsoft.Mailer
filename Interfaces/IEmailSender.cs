using System.Threading.Tasks;

namespace Michaelsoft.Mailer.Interfaces
{
    public interface IEmailSender
    {

        Task SendEmailAsync(string email,
                            string subject,
                            string htmlMessage);

    }
}