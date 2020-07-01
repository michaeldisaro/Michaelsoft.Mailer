using System;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Michaelsoft.Mailer.Interfaces;
using Michaelsoft.Mailer.Settings;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Michaelsoft.Mailer.Services
{
    public class EmailSender : IEmailSender
    {

        private readonly EmailSettings _emailSettings;

        public EmailSender(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(string email,
                                         string subject,
                                         string message)
        {
            try
            {
                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.Sender));
                mimeMessage.To.Add(new MailboxAddress(email, email));
                mimeMessage.Subject = subject;
                mimeMessage.Body = new TextPart("html")
                {
                    Text = message
                };
                mimeMessage.MessageId = email + "." + new Random().Next(111111111, 999999999);

                using var client = new SmtpClient
                {
                    // TODO: Verify the correct certificate for tls handshake
                    ServerCertificateValidationCallback = (s,
                                                           c,
                                                           h,
                                                           e) => true
                };

                await client.ConnectAsync(_emailSettings.MailServer, _emailSettings.MailPort,
                                          _emailSettings.MailPort != 25);

                if (!string.IsNullOrEmpty(_emailSettings.Password))
                    await client.AuthenticateAsync(_emailSettings.Sender, _emailSettings.Password);

                await client.SendAsync(mimeMessage);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                // TODO: handle exception
                throw ex;
            }
        }

    }
}