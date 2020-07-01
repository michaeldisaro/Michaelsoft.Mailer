using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Michaelsoft.Mailer.Extensions;
using Michaelsoft.Mailer.Interfaces;
using Michaelsoft.Mailer.Settings;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace Michaelsoft.Mailer.Services
{
    public class EmailSender : IEmailSender
    {

        private readonly EmailSettings _emailSettings;

        public EmailSender(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendEmailAsync(Dictionary<string, string> tos,
                                         string subject,
                                         string body,
                                         Dictionary<string, string> ccs = null,
                                         Dictionary<string, string> bccs = null)
        {
            try
            {
                ccs ??= new Dictionary<string, string>();

                bccs ??= new Dictionary<string, string>();

                var message = BuildMessage(tos, ccs, bccs, subject, body, body);

                await SendEmail(message);
            }
            catch (Exception ex)
            {
                // TODO: handle exception
                throw ex;
            }
        }

        public async Task SendEmailUsingTemplateAsync(Dictionary<string, string> tos,
                                                      string subject,
                                                      string template,
                                                      Dictionary<string, string> parameters,
                                                      Dictionary<string, string> ccs = null,
                                                      Dictionary<string, string> bccs = null)
        {
            try
            {
                ccs ??= new Dictionary<string, string>();

                bccs ??= new Dictionary<string, string>();

                var textBody = BuildTextBody(template, parameters);

                var htmlBody = BuildHtmlBody(template, parameters);

                var message = BuildMessage(tos, ccs, bccs, subject, textBody, htmlBody);

                await SendEmail(message);
            }
            catch (Exception ex)
            {
                // TODO: handle exception
                throw ex;
            }
        }

        private string BuildTextBody(string template,
                                     Dictionary<string, string> parameters)
        {
            var body = File.ReadAllText(Path.Combine(_emailSettings.TemplatePath, $"{template}.txt"));
            
            return parameters.Aggregate(body, (current,
                                               parameter) =>
                                            Regex.Replace(current, "\\{\\{" + parameter.Key + "\\}\\}",
                                                          parameter.Value));
        }

        private string BuildHtmlBody(string template,
                                     Dictionary<string, string> parameters)
        {
            var body = File.ReadAllText(Path.Combine(_emailSettings.TemplatePath, $"{template}.html"));
            
            return parameters.Aggregate(body, (current,
                                               parameter) =>
                                            Regex.Replace(current, "\\{\\{" + parameter.Key + "\\}\\}",
                                                          parameter.Value));
        }

        private MimeMessage BuildMessage(Dictionary<string, string> tos,
                                         Dictionary<string, string> ccs,
                                         Dictionary<string, string> bccs,
                                         string subject,
                                         string textBody,
                                         string htmlBody,
                                         AttachmentCollection attachments = null)
        {
            var message = new MimeMessage();

            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));

            foreach (var to in tos)
                message.To.Add(new MailboxAddress(to.Value, to.Key));

            foreach (var cc in ccs)
                message.Cc.Add(new MailboxAddress(cc.Value, cc.Key));

            foreach (var bcc in bccs)
                message.Bcc.Add(new MailboxAddress(bcc.Value, bcc.Key));

            message.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = htmlBody,
                TextBody = textBody
            };

            message.Body = bodyBuilder.ToMessageBody();

            message.MessageId =
                $"{string.Join(';', tos.Keys)};{new Random().Next(111111111, 999999999)}".Sha1();

            return message;
        }

        private async Task SendEmail(MimeMessage message)
        {
            using var client = new SmtpClient
            {
                // TODO: Verify the correct certificate for tls handshake
                ServerCertificateValidationCallback = (s,
                                                       c,
                                                       h,
                                                       e) => true
            };

            await client.ConnectAsync(_emailSettings.HostAddress, _emailSettings.HostPort,
                                      _emailSettings.HostPort != 25);

            if (!string.IsNullOrEmpty(_emailSettings.SenderPassword))
                await client.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.SenderPassword);

            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

    }
}