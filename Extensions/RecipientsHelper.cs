using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Michaelsoft.Mailer.Extensions
{
    public static class RecipientsHelper
    {

        public static bool IsValidEmail(this string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // Normalize the domain
                email = Regex.Replace(email, @"(@)(.+)$", DomainMapper,
                                      RegexOptions.None, TimeSpan.FromMilliseconds(200));

                // Examines the domain part of the email and normalizes it.
                static string DomainMapper(Match match)
                {
                    // Use IdnMapping class to convert Unicode domain names.
                    var idn = new IdnMapping();

                    // Pull out and process domain name (throws ArgumentException on invalid)
                    var domainName = idn.GetAscii(match.Groups[2].Value);

                    return match.Groups[1].Value + domainName;
                }
            }
            catch (RegexMatchTimeoutException e)
            {
                return false;
            }
            catch (ArgumentException e)
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(email,
                                     @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                                     RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public static Dictionary<string, string> ToDictionaryOfRecipients(this string recipients)
        {
            var recipientsDictionary = new Dictionary<string, string>();
            if (recipients == null)
                return recipientsDictionary;
            
            var firstLevelSubdivision = recipients.Split(";");
            foreach (var recipient in firstLevelSubdivision)
            {
                string emailAddress = null;
                string name = null;
                var withName = recipient.Contains("<");
                if (withName)
                {
                    var secondLevelSubdivision = recipient.Split("<");
                    if (secondLevelSubdivision.Length != 2)
                        throw new
                            InvalidOperationException("Send your recipients as emailaddress1<name1>;emailaddress2<name2>;...");
                    emailAddress = secondLevelSubdivision[0];
                    name = secondLevelSubdivision[1].Replace(">", "");
                }
                else
                {
                    emailAddress = recipient;
                    name = recipient;
                }

                if (!emailAddress.IsValidEmail())
                    throw new InvalidDataException("Invalid email address");

                recipientsDictionary.Add(emailAddress, name);
            }

            return recipientsDictionary;
        }

    }
}