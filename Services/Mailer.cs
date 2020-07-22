﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Michaelsoft.Mailer.Extensions;
using Michaelsoft.Mailer.Interfaces;
using Michaelsoft.Mailer.Models;
using Michaelsoft.Mailer.Settings;
using Microsoft.Extensions.Options;
using MimeKit;
using AttachmentCollection = MimeKit.AttachmentCollection;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;

namespace Michaelsoft.Mailer.Services
{
    public class Mailer : IMailer
    {

        private readonly EmailSettings _emailSettings;

        public Mailer(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendMailAsync(Dictionary<string, string> tos,
                                        string subject,
                                        string body,
                                        Dictionary<string, string> ccs = null,
                                        Dictionary<string, string> bccs = null,
                                        List<Attachment> attachments = null)
        {
            try
            {
                ccs ??= new Dictionary<string, string>();

                bccs ??= new Dictionary<string, string>();

                attachments ??= new List<Attachment>();

                var attachmentCollection = BuildAttachmentCollection(attachments);

                var message = BuildMessage(tos, ccs, bccs, subject, body, body, attachmentCollection);

                await SendEmail(message);
            }
            catch (Exception ex)
            {
                // TODO: handle exception
                throw ex;
            }
        }

        public async Task SendMailUsingTemplateAsync(Dictionary<string, string> tos,
                                                     string subject,
                                                     string template,
                                                     Dictionary<string, string> parameters,
                                                     Dictionary<string, string> ccs = null,
                                                     Dictionary<string, string> bccs = null,
                                                     List<Attachment> attachments = null)
        {
            try
            {
                ccs ??= new Dictionary<string, string>();

                bccs ??= new Dictionary<string, string>();

                attachments ??= new List<Attachment>();

                var textBody = BuildTextBody(template, parameters);

                var htmlBody = BuildHtmlBody(template, parameters);

                var attachmentCollection = BuildAttachmentCollection(attachments);

                var message = BuildMessage(tos, ccs, bccs, subject, textBody, htmlBody, attachmentCollection);

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

        private AttachmentCollection BuildAttachmentCollection(List<Attachment> attachments)
        {
            var attachmentCollection = new AttachmentCollection();

            foreach (var attachment in attachments)
            {
                var mimePart = new MimePart(attachment.Type, attachment.SubType)
                {
                    Content = new MimeContent(attachment.Content, ContentEncoding.Default),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    ContentTransferEncoding = ContentEncoding.Base64,
                    FileName = attachment.Name
                };
                attachmentCollection.Add(mimePart);
            }

            return attachmentCollection;
        }

        private MimeMessage BuildMessage(Dictionary<string, string> tos,
                                         Dictionary<string, string> ccs,
                                         Dictionary<string, string> bccs,
                                         string subject,
                                         string textBody,
                                         string htmlBody,
                                         AttachmentCollection attachments)
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

            foreach (var attachment in attachments)
                bodyBuilder.Attachments.Add(attachment);

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