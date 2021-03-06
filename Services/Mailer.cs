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

        public Task SendMailAsync(string tos,
                                  string subject,
                                  string body,
                                  string ccs = null,
                                  string bccs = null,
                                  List<Attachment> attachments = null)
        {
            var tosDict = tos.ToDictionaryOfRecipients();
            var ccsDict = ccs.ToDictionaryOfRecipients();
            var bccsDict = bccs.ToDictionaryOfRecipients();
            return SendMailAsync(tosDict,
                                 subject,
                                 body,
                                 ccsDict,
                                 bccsDict,
                                 attachments);
        }

        public Task SendMailUsingTemplateAsync(string tos,
                                               string subject,
                                               string template,
                                               Dictionary<string, string> parameters,
                                               string ccs = null,
                                               string bccs = null,
                                               List<Attachment> attachments = null,
                                               Dictionary<string, List<Dictionary<string, string>>> partials = null)
        {
            var tosDict = tos.ToDictionaryOfRecipients();
            var ccsDict = ccs.ToDictionaryOfRecipients();
            var bccsDict = bccs.ToDictionaryOfRecipients();
            return SendMailUsingTemplateAsync(tosDict,
                                       subject,
                                       template,
                                       parameters,
                                       ccsDict,
                                       bccsDict,
                                       attachments,
                                       partials);
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
                                                     List<Attachment> attachments = null,
                                                     Dictionary<string, List<Dictionary<string, string>>> partials =
                                                         null)
        {
            try
            {
                ccs ??= new Dictionary<string, string>();

                bccs ??= new Dictionary<string, string>();

                attachments ??= new List<Attachment>();

                partials ??= new Dictionary<string, List<Dictionary<string, string>>>();

                var textBody = BuildTextBody(template, parameters, partials);

                var htmlBody = BuildHtmlBody(template, parameters, partials);

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
                                     Dictionary<string, string> parameters,
                                     Dictionary<string, List<Dictionary<string, string>>> partials)
        {
            if (!File.Exists(Path.Combine(_emailSettings.TemplatePath, $"{template}.txt"))) return "";

            var body = File.ReadAllText(Path.Combine(_emailSettings.TemplatePath, $"{template}.txt"));

            body = IntegratePartials(body, partials, false);

            return parameters.Aggregate(body, (current,
                                               parameter) =>
                                            Regex.Replace(current, "\\{\\{" + parameter.Key + "\\}\\}",
                                                          parameter.Value ?? ""));
        }

        private string BuildHtmlBody(string template,
                                     Dictionary<string, string> parameters,
                                     Dictionary<string, List<Dictionary<string, string>>> partials)
        {
            if (!File.Exists(Path.Combine(_emailSettings.TemplatePath, $"{template}.html"))) return "";

            var body = File.ReadAllText(Path.Combine(_emailSettings.TemplatePath, $"{template}.html"));

            body = parameters.Aggregate(body, (current,
                                               parameter) =>
                                            Regex.Replace(current, "\\{\\{" + parameter.Key + "\\}\\}",
                                                          parameter.Value ?? ""));

            body = IntegratePartials(body, partials, true);

            return body;
        }

        private string IntegratePartials(string body,
                                         Dictionary<string, List<Dictionary<string, string>>> partials,
                                         bool isHtml = true)
        {
            var partialExtension = isHtml ? "html" : "txt";
            const string partialRegex = "\\{\\{(_\\w+)\\}\\}";

            var integratedPartials = false;
            var matches = Regex.Matches(body, partialRegex).Select(m => m.Groups).Select(g => g[1].Value).ToArray();
            while (matches.Any())
            {
                foreach (var match in matches)
                {
                    if (!partials.ContainsKey(match)) continue;

                    if (!partials[match].Any()) continue;

                    var partialFile = $"{match}.{partialExtension}";

                    if (!File.Exists(Path.Combine(_emailSettings.TemplatePath, "Partials", partialFile))) continue;

                    var partialTemplate =
                        File.ReadAllText(Path.Combine(_emailSettings.TemplatePath, "Partials", partialFile));

                    var partial = "";
                    foreach (var parameters in partials[match])
                    {
                        partial += parameters.Aggregate
                            (partialTemplate, (current,
                                               parameter) =>
                            {
                                var value = isHtml
                                                ? parameter.Value?.Replace("\n", "<br>")
                                                : parameter.Value;
                                return Regex.Replace(current,
                                                     "\\{\\{" + parameter.Key + "\\}\\}",
                                                     value ?? "");
                            });
                    }

                    body = Regex.Replace(body, "\\{\\{" + match + "\\}\\}", partial);

                    integratedPartials = true;
                }

                if (!integratedPartials) break;

                matches = Regex.Matches(body, partialRegex).Select(m => m.Groups).Select(g => g[1].Value).ToArray();
                integratedPartials = false;
            }

            return body;
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